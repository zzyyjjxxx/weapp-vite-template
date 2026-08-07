# 企业认证与用地需求 Web API 设计

## 目标

为 Forguncy 8.0.4 服务端 Web API 增加明确的企业认证、企业信息和用地需求填报接口。认证接口使用现有 `c_userinfo` 表校验企业信用代码和密码；企业信息来自 `m_preliminary_list`；填报记录来自 `landusedemand_info`。业务接口只能访问当前 access token 对应的企业。

本次开发阶段直接使用本机 `mujunbigdata` 数据库进行元数据和连接验证，但不在仓库中保存数据库账号、密码或连接字符串，不执行真实填报新增/修改数据，也不修改数据库结构。

## 已确认的接口契约

### 企业认证与企业信息

`EnterpriseApi` 提供：

```text
POST /customapi/enterpriseapi/login
POST /customapi/enterpriseapi/refresh
GET  /customapi/enterpriseapi/getinfo
```

旧的 `/customapi/authapi/login` 和 `/customapi/authapi/refresh` 路由直接移除，不保留兼容别名。

登录请求继续接受 JSON 和 `application/x-www-form-urlencoded`，其中 `username` 是企业信用代码，`password` 是企业密码。成功响应继续使用现有五字段 token 契约：

```json
{
  "access_token": "...",
  "refresh_token": "...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_expires_in": 604800
}
```

登录不返回企业对象；客户端登录成功后调用 `getinfo`。`getinfo` 必须携带 access token，服务端从 JWT `name` claim 获取信用代码，不读取 query 或 body 中的信用代码，返回：

```json
{
  "businessname": "企业名称",
  "creditcode": "统一社会信用代码",
  "county": "区县名称",
  "region": "乡镇名称"
}
```

`m_preliminary_list.creditCode` 关联 `yj_regioninfo.id`，`m_preliminary_list.county` 只作为区县 ID 使用，返回 `yj_regioninfo.name`。企业名称和信用代码分别来自 `businessName`、`creditCode`。

刷新请求继续接受 JSON 或表单中的 `refresh_token`，只允许 `token_use=refresh` 的有效 JWT；refresh token 不得访问企业信息或填报接口。

### 用地需求填报

`LandDemandApi` 提供：

```text
GET  /customapi/landdemandapi/getlanddemand
POST /customapi/landdemandapi/addlanddemand
POST /customapi/landdemandapi/updatelanddemand
```

三个接口都从 access token 的 `name` claim 派生信用代码。请求体不得覆盖 `creditcode`、`businessname`、`county` 或 `region`；新增时这些字段由服务端从企业主数据填充，修改时保持数据库原值。`updatetime` 和 `updateuser` 始终由服务端写入，`id`、审核意见、推荐状态等内部字段不可由企业接口写入。

查询、新增成功和修改成功的业务字段范围为：

```text
businessname
creditcode
county
region
area
building_area
expect_park
expect_time
is_deploy
deploy_park
is_specialuse
deploy_landtype
deploy_height
deploy_weight
investment
project_hydm
keyindustry
futureindustry
pred_ys
pred_tax
pred_rdex
pred_unitenergy
projectdata
is_financing
financing_money
financing_time
contact
office
phone
landusedemand
updatetime
```

`deploy_park` 按 `landusedemand_info` 的逗号分隔字符串传输；数值和状态值按数据库列定义传输。`landusedemand=1` 为正式提交，`landusedemand=2` 为暂存。`projectdata` 不增加 UI 字符数限制，实际数据库列长度仍由现有表结构决定。

新增和修改请求使用 JSON，字段为上述业务字段中可写的表单字段和 `landusedemand`，不接受身份字段、`updatetime`、`updateuser` 或内部字段。正式提交沿用现有产品规则校验必填项、条件字段和数值格式；暂存允许不完整但仍拒绝类型、数值和日期格式错误。

业务状态码约定如下：

| 场景 | HTTP | body |
| --- | ---: | --- |
| 缺失/无效 access token | 401 | `{"error":"invalid_token"}` |
| 缺失或格式错误的请求 | 400 | `{"error":"invalid_request"}` |
| 企业主数据不存在 | 404 | `{"error":"enterprise_not_found"}` |
| 填报记录不存在 | 404 | `{"error":"land_demand_not_found"}` |
| 新增时已有同信用代码记录 | 409 | `{"error":"land_demand_exists"}` |
| 未预期服务端异常 | 500 | `{"error":"server_error"}` |

所有错误响应固定为非敏感结构；不会返回 SQL、连接字符串、JWT 密钥、异常消息或其他企业数据。

## 架构

将当前只负责认证的 `AuthApi` 改为 `EnterpriseApi`，将 `Login`、`Refresh` 和 `GetInfo` 放在企业边界内；新增独立的 `LandDemandApi`。两类 API 通过共享的组合根和可重试缓存获得同一个运行时：

```text
EnterpriseApi / LandDemandApi
        |
共享 EnterpriseCompositionRoot
        |
AccessTokenReader + JwtTokenService
        |
EnterpriseService / AuthService / LandDemandService
        |
EnterpriseRepository / UserRepository / LandDemandRepository
        |
SqlSugar -> Forguncy config.item='ssl' -> MySQL
```

组合根继续从 Forguncy `config.item='ssl'` 读取连接字符串，从 `config` 表读取 JWT 配置。不会新增环境变量、数据库初始化、schema migration 或硬编码本机凭据。登录和业务运行时共享同一个 JWT 配置，进程重启仍是配置变更后的生效边界。

`JwtTokenService` 增加明确的 access token 校验：除签名、发行者、有效期和算法校验外，还必须要求 `token_use=access`。现有 refresh 校验保持 `token_use=refresh`，业务接口不能调用通用签名校验来绕过 token 类型检查。

## 数据层设计

新增带 `SugarTable`/`SugarColumn` 映射的类型和仓储接口：

- 企业查询映射 `m_preliminary_list.businessName`、`creditCode`、`county`、`region`，通过 `yj_regioninfo.id` 取得区县名称；企业仓储只返回企业 DTO，不暴露原始主数据行。
- 填报行映射 `landusedemand_info`，查询条件始终是服务端解析出的信用代码；现有 `creditcode` 唯一键用于阻止重复新增。
- 新增只写企业表单白名单和服务器控制字段；修改使用同一白名单，未包含的内部/审核列不参与更新，因此不会被空值覆盖。
- SQL 使用 SqlSugar 参数化表达式；仓储把数据库异常交给应用层统一转换为非敏感 500 响应。

## 测试与验收

测试在公开边界验证行为，不依赖真实填报写入：

1. JWT 单元测试验证 access token 可用、refresh token 被业务接口拒绝、缺少/错误 claim 被拒绝。
2. 请求读取测试验证 JSON/表单认证请求和 JSON 填报请求的成功、缺失字段、错误类型及不支持 content type。
3. 企业服务/仓储测试验证区县 ID 关联、信用代码隔离、企业字段白名单和无企业记录分支。
4. 填报服务测试验证按信用代码查询、重复新增、缺失修改、身份字段不可覆盖、内部字段保留、服务器生成更新时间，以及 `1/2` 状态和提交条件校验。
5. Forguncy API surface 测试验证只导出正式的 `EnterpriseApi` 与 `LandDemandApi` 路由，不再导出旧 `AuthApi` 登录/刷新路由；响应 JSON 不包含内部审核/审计字段。
6. Release `net472` 测试、构建、反射表面检查和 `git diff --check` 必须记录在 `reports/verification.md`。本地 MySQL 只做连接/schema 读取证据；若未执行真实 HTTP 或 Forguncy Designer 交互，必须明确标为未验证。

## 非目标

- 不改 `m_preliminary_list`、`landusedemand_info` 或 `yj_regioninfo` 表结构。
- 不实现短信、发布、生产凭据、管理员审核接口、内部意见查询或 refresh token 持久化撤销。
- 不把本机 root 密码、连接字符串或真实企业数据写入源码、文档、测试快照或验证报告。

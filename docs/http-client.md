# 用地需求 HTTP Client 与 Repository

应用运行时在 `src/app.vue` 配置真实 HTTP Repository，默认请求基地址为
`http://localhost:17163/`。验证码暂时仍由本地 Mock Repository 处理，以便本地开发服务不可达时继续测试提交流程；步骤草稿也继续使用微信小程序 Storage。本地步骤草稿是明确的例外，由 LandDemand Store 直接调用 Repository，不经过 Service 或 Query。单元测试仍可注入 Mock Repository。

## 接口边界

- `AuthRepository.login(input)`：调用登录接口，再用 access token 调用 `getinfo` 组装企业会话。
- `AuthRepository.refresh(session)`：使用 refresh token 调用刷新接口，再重新读取企业信息。
- `AuthRepository.getInfo(token)`：读取当前 access token 对应的企业基础信息。
- `LandDemandRepository.get/save/update`：调用用地需求查询、新增和修改接口；查询接口返回 `404 land_demand_not_found` 时映射为空记录。
- `getDraft/setDraft/removeDraft`：管理只在本机使用的步骤草稿。
- `sendCode/verifyCode`：暂时模拟六位验证码、5 分钟有效期、60 秒重发间隔与最多 5 次错误尝试，不调用短信接口。

页面和步骤组件不得直接实例化 Repository。已持久化记录遵循 `页面 → Query/Mutation → Service → Repository`；仅本地编辑草稿遵循 `页面 → Store → Repository`，以便步骤切换时同步保存且不污染 Query 的持久化记录所有权。测试可注入内存 Storage 和确定性时钟/验证码。

## HTTP 路由

| Method | Path | Adapter |
| --- | --- | --- |
| POST | `/customapi/enterpriseapi/login` | Auth login |
| POST | `/customapi/enterpriseapi/refresh` | Auth refresh |
| GET | `/customapi/enterpriseapi/getinfo` | Auth getInfo |
| POST | `/customapi/enterpriseapi/sendcode` | Local Mock for now |
| POST | `/customapi/enterpriseapi/verifycode` | Local Mock for now |
| GET | `/customapi/landdemandapi/getlanddemand` | Land-demand get |
| POST | `/customapi/landdemandapi/addlanddemand` | Land-demand save |
| POST | `/customapi/landdemandapi/updatelanddemand` | Land-demand update |

The HTTP client uses `wpi.request`, sends JSON bodies, and adds
`Authorization: Bearer <access_token>` to protected routes. It maps the server's
`error`/`status` codes to sanitized domain errors and does not log credentials,
phone numbers, tokens, or complete form payloads.

## Storage 键

- `land-demand.auth`：版本化认证会话。
- `mock:land-demand:{creditcode}`：Mock 持久化用地需求记录。
- `draft:land-demand:{creditcode}`：本地步骤、表单和保存时间。
- `mock:verification:{phone}`：短期验证码挑战与尝试次数。

这些键仅是当前 Mock 协议，不应作为生产接口契约。退出登录会清除认证会话与私有 Query 缓存；企业记录仍按信用代码保留，以便重新登录回显。

## Payload 与业务状态

`landusedemand=2` 表示暂存，`landusedemand=1` 表示正式提交。新增 Payload 包含企业基础信息；修改 Payload 使用信用代码并保留页面不展示的旧字段。`deploy_park` 在表单内为数组，接口层按逗号序列化；`deploy_landtype` 是单个名称。

信用代码及企业名称、区县、乡镇来自当前认证企业，本地草稿不能替换。`decimal(20,6)` 字段最多 14 位整数，`decimal(10,2)` 字段最多 8 位整数；当前 UI 最多允许 2 位小数。可选数值空字符串表示未填写，不能在适配层擅自转换为 `0`。

## 当前真实后端适配

真实适配器位于 `src/features/auth/http-repository.ts` 和
`src/features/land-demand/http-repository.ts`，并在应用初始化处配置，不改页面、Store、校验或 Query/Mutation 规则。适配器负责：

1. 将 access/refresh token 和 `getinfo` 响应映射到认证领域模型；后端当前 `getinfo` 只返回企业名称、信用代码、区县和乡镇，刷新时保留已有的可编辑联系人字段。
2. 将表单中的字符串数值转换为 JSON number，将接口响应数值转换回表单字符串；空数值不会被转换为 `0`。
3. 只向用地需求写接口发送后端允许的 26 个业务字段，移除 `businessname`、`creditcode`、`county`、`region`、`updateuser`、`newproject` 等客户端/服务端字段。
4. 由后端生成 `updatetime/updateuser`；服务端验证码接入前，验证码挑战和校验留在本地 Mock。
5. 保持 Query 键、私有缓存清理和新增后切换修改语义。

生产请求域名、真实短信和后端部署不属于当前本地开发配置；部署前应替换 `src/platform/api-config.ts` 中的基地址。
## Storage 失败语义

Storage 键不存在时读取为空；真正的读取异常以及写入、删除失败都必须向调用方抛出。
因此记录保存失败不会被 Mutation 报告为成功，页面也不会执行
`markPersisted` 或删除仍可恢复的本地草稿。

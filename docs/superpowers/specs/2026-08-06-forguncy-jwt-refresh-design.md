# 活字格 8.0.4 JWT 刷新令牌设计

## 1. 目标与范围

在现有 `forguncy-server-api` 登录 API 上增加无状态 JWT 刷新机制：

- 登录成功同时返回访问令牌和刷新令牌。
- 新增 `Refresh` Web API，根据刷新令牌重新颁发一对令牌。
- 用户信息仍只放在 JWT 的 `sub` 和 `name` 声明中，不在 HTTP 响应中返回用户对象。
- 保持活字格 8.0.4、.NET Framework 4.7.2、SqlSugar 5.1.4.111 和 MySql.Data 8.0.30 不变。
- 不新增数据库表，不提供独立的 JWT 颁发、校验或注销 API。

本阶段不实现服务端刷新令牌撤销、令牌黑名单、刷新令牌持久化、用户注销或账号状态实时吊销。

## 2. 对外 API

### 2.1 登录

```text
POST /customapi/authapi/login
```

登录请求保持现有 JSON 和 `application/x-www-form-urlencoded` 两种格式。

成功响应为：

`expires_in` 和 `refresh_expires_in` 的单位均为 seconds。配置表中的有效期
仍以 minutes 保存；默认 `FGC_JWT_REFRESH_EXPIRES_MINUTES=10080` minutes，
因此默认刷新响应示例为 `refresh_expires_in=604800` seconds。

```json
{
  "access_token": "<access-jwt>",
  "refresh_token": "<refresh-jwt>",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_expires_in": 604800
}
```

### 2.2 刷新

```text
POST /customapi/authapi/refresh
```

JSON 请求体为：

```json
{
  "refresh_token": "<refresh-jwt>"
}
```

刷新接口同时支持 `application/x-www-form-urlencoded` 的
`refresh_token=<refresh-jwt>` 请求。校验成功后返回与登录成功相同的令牌响应结构。

错误响应约定：

- 缺少字段、空字段或格式错误：`400 {"error":"invalid_request"}`。
- 签名错误、发行者错误、过期、缺少身份声明，或传入访问令牌：
  `401 {"error":"invalid_refresh_token"}`。
- 未预期异常：`500 {"error":"server_error"}`，不暴露异常、配置、连接字符串或密钥。

## 3. JWT 设计

访问令牌和刷新令牌均使用当前配置中的 HS256、签名密钥和发行者，并包含：

- `sub`：`c_userinfo.id`。
- `name`：`c_userinfo.creditCode`。
- `jti`：随机令牌 ID。
- `iat`、`nbf`、`exp`：签发、生效和过期时间。
- `token_use`：访问令牌为 `access`，刷新令牌为 `refresh`。

`JwtTokenService` 保留现有访问令牌创建能力，新增刷新令牌创建和专用刷新令牌校验能力。
刷新校验必须验证 `token_use=refresh`，因此访问令牌不能被当作刷新令牌使用。
刷新令牌中的 `sub` 和 `name` 经过严格解析后构造内部 `AuthUser`，不重新查询数据库；
这是无状态方案的核心行为。

每次刷新都会签发新的访问令牌和刷新令牌，但由于不保存刷新令牌状态，旧刷新令牌在自身过期前仍然有效，
不能实现真正的撤销或单次使用轮换。

## 4. 配置

现有配置继续从 Forguncy `config` 表读取：

| `item` | 用途 | 缺失或空值时写入 |
| --- | --- | --- |
| `FGC_JWT_SIGNING_KEY` | HS256 签名密钥 | 随机密钥 |
| `FGC_JWT_ISSUER` | JWT 发行者 | 随机发行者 |
| `FGC_JWT_EXPIRES_MINUTES` | 访问令牌有效期 | `60` |
| `FGC_JWT_REFRESH_EXPIRES_MINUTES` | 刷新令牌有效期 | `10080` |

新增 `AuthOptions.JwtRefreshLifetime`，沿用现有正整数分钟校验。配置读取仍按“先查询，再决定读取、更新或插入”的逻辑执行；
非空非法值继续导致初始化失败，不被静默覆盖。

## 5. 代码边界与数据流

```text
POST /login                 POST /refresh
       |                           |
       v                           v
     AuthApi  -- request/response adaptation --+
       |                                        |
       v                                        v
   AuthService -- login/query user       AuthService -- validate refresh claims
       |                                        |
       +------------ IJwtTokenService ----------+
                             |
                      access + refresh JWT
```

- `AuthApi` 负责读取请求、调用应用服务、设置状态码和序列化响应。
- `AuthService.LoginAsync` 查询 `c_userinfo`、校验标准小写 16 位 MD5 密码并创建令牌对。
- `AuthService.RefreshAsync` 校验刷新令牌声明并从声明创建令牌对，不访问数据库。
- `IJwtTokenService` 负责令牌生成和刷新令牌校验；它不暴露为 Forguncy API。
- `AuthOptions` 同时提供访问令牌和刷新令牌的有效期。
- `AuthCompositionRoot` 将同一组配置和 JWT 服务注入 `AuthService`。

应用层使用内部令牌对结果，HTTP 层只序列化令牌字段，禁止加入 `user`、数据库记录或其他用户对象。

## 6. 测试与验收

测试先行覆盖以下行为：

1. 配置读取能够读取或创建刷新令牌有效期，并保留现有配置行为。
2. JWT 服务创建刷新令牌，刷新令牌包含 `token_use=refresh`，访问令牌不能通过刷新校验。
3. 刷新令牌签名错误、发行者错误、过期、身份声明缺失和类型错误均被拒绝。
4. 登录结果包含访问令牌、刷新令牌以及两种有效期。
5. 刷新服务根据刷新令牌声明生成新的令牌对，不读取数据库。
6. `AuthApi` 暴露两个无参数 `[Post]` 方法：`Login` 和 `Refresh`，且响应不含用户对象。
7. 缺失刷新令牌返回 `400`，无效刷新令牌返回 `401`，未预期异常返回固定 `500`。
8. Release 测试、构建、Forguncy 8.0.4 反射 API 扫描和 `git diff --check` 全部通过。

验证命令继续使用 Forguncy 8.0.4 SDK 路径：

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

## 7. 非目标与安全边界

- 不修改 `c_userinfo` 表结构，不新增用户信息返回字段。
- 不把刷新令牌写入 MySQL、Forguncy `config` 表或日志。
- 不新增外部 JWT 校验接口。
- 生产环境仍须通过 HTTPS 暴露登录和刷新接口。
- 无状态刷新方案不能即时撤销令牌；如后续需要注销、强制下线或账号禁用即时生效，需另行设计有状态刷新令牌存储方案。

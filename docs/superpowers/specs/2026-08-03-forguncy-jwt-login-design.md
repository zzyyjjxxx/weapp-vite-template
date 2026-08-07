# 活字格 8.0.4 JWT 登录 Web API 设计

## 1. 目标与范围

在当前工作区新增独立的 `forguncy-server-api/` .NET 类库，为活字格 8.0.4 提供一个最小登录服务：

- 对外只暴露登录接口。
- 登录成功后由内部 JWT 服务签发 Bearer Token。
- JWT 签发和校验作为内部类库能力，不暴露 `/issue` 或 `/validate` API。
- 使用 EF Core + Pomelo MySQL 查询用户和创建用户表。
- 不修改当前微信小程序的既有业务代码。

本阶段不包含注册、刷新令牌、登出、令牌黑名单、角色权限、短信登录、生产发布或真实业务数据接入。

## 2. 版本与宿主约束

本机活字格安装目录为：

```text
D:\Program Files\Forguncy 8.0.4\Website\bin
```

已确认的本机程序集能力：

- `GrapeCity.Forguncy.ServerApi.dll` 版本为 `8.0.4.0`，目标框架为 `netstandard2.0`。
- `Forguncy.Server2` 目标框架为 `net6.0`。
- `ForguncyApi` 暴露 `Context`、`DataAccess` 等属性。
- `GetAttribute` 和 `PostAttribute` 可标注服务端 API 方法。
- 本机 EF Core 为 `6.0.21`，Pomelo.EntityFrameworkCore.MySql 为 `6.0.0`，MySqlConnector 为 `2.1.2`。
- 本机 JWT 依赖为 `System.IdentityModel.Tokens.Jwt 6.8.0` 及其对应的 Microsoft.IdentityModel 程序集。

新项目目标框架为 `net6.0`，添加 `Microsoft.AspNetCore.App` FrameworkReference，并以指定安装目录中的程序集作为编译时版本依据。活字格官方文档的扩展方式是继承 `ForguncyApi`、用 `[Post]`/`[Get]` 标注方法并通过 `Context` 读写 HTTP 请求响应；8.0.4 实现以本机程序集实际签名为准。

## 3. 架构

```text
POST /customapi/authapi/login
          │
          ▼
      AuthApi : ForguncyApi
          │ 读取 JSON 或表单请求、组织 HTTP 响应
          ▼
      AuthService
          ├── UserRepository
          │     └── AuthDbContext (EF Core + Pomelo MySQL)
          └── JwtTokenService (内部签发与校验)
```

职责边界：

- `AuthApi` 只处理活字格 Web API 适配、请求解析和状态码，不包含密码哈希或 JWT 细节。
- `AuthService` 负责登录用例：校验输入、查询启用用户、验证密码并生成登录结果。
- `UserRepository` 只负责用户表查询、创建和初始账号写入。
- `AuthDbContext` 映射 `jwt_users`，使用 EF Core 建表和查询。
- `PasswordHasher` 使用 .NET 内置 PBKDF2-SHA256 生成和验证密码哈希。
- `JwtTokenService` 只负责 JWT 的签发和校验，不能被活字格路由直接发现为 Web API。

## 4. 对外接口

### 4.1 登录

```text
POST /customapi/authapi/login
```

支持 `application/json` 和活字格常用的 `application/x-www-form-urlencoded` 请求。

JSON 请求体：

```json
{
  "username": "demo",
  "password": "demo123"
}
```

成功响应 `200 OK`：

```json
{
  "access_token": "<jwt>",
  "token_type": "Bearer",
  "expires_in": 3600,
  "user": {
    "id": 1,
    "username": "demo"
  }
}
```

错误约定：

- 缺少用户名或密码：`400 Bad Request`，返回 `invalid_request`。
- 用户不存在、密码错误或用户被禁用：统一返回 `401 Unauthorized` 和 `invalid_credentials`，不泄露账号是否存在。
- JWT 或数据库配置缺失：`500 Internal Server Error`，只记录服务端诊断信息，不把密钥、连接字符串或堆栈返回给客户端。

本阶段不增加登录以外的公开路由。`JwtTokenService.CreateToken` 和 `JwtTokenService.ValidateToken` 只能由类库内部代码调用；未来新增受保护 API 时，在对应 API 内部读取 Bearer Header 并调用校验服务。

## 5. 用户数据模型

表名为 `jwt_users`，由 EF Core `EnsureCreatedAsync` 在指定数据库中创建：

| 字段 | 类型 | 约束 | 说明 |
| --- | --- | --- | --- |
| `id` | `BIGINT` | 主键、自增 | 用户标识 |
| `username` | `VARCHAR(100)` | 非空、唯一 | 登录名 |
| `password_hash` | `VARCHAR(512)` | 非空 | PBKDF2-SHA256 自包含哈希 |
| `is_enabled` | `TINYINT(1)` | 非空，默认 `1` | 是否允许登录 |
| `created_at` | `DATETIME(6)` | 非空 | 创建时间，UTC |
| `updated_at` | `DATETIME(6)` | 非空 | 更新时间，UTC |

密码哈希格式包含算法、迭代次数、随机盐和派生结果，例如：

```text
PBKDF2-SHA256$100000$<base64-salt>$<base64-hash>
```

验证使用固定时间比较，不保存或记录明文密码。数据库不提供注册 API。

### 5.1 初始账号

通过以下两个环境变量可选地创建首个账号：

```text
FGC_AUTH_BOOTSTRAP_USERNAME
FGC_AUTH_BOOTSTRAP_PASSWORD
```

只有两个变量同时存在且用户名尚不存在时才创建账号；已存在账号不会被覆盖。未配置初始账号时，部署人员可以通过数据库管理工具按同一 PBKDF2 格式预置用户。

## 6. 配置与安全边界

使用环境变量，不把本机数据库密码或 JWT 密钥写入源代码、配置提交或日志：

```text
FGC_AUTH_MYSQL_CONNECTION=<MySQL connection string>
FGC_JWT_SIGNING_KEY=<at least 32 characters>
FGC_JWT_ISSUER=forguncy-server-api
FGC_JWT_EXPIRES_MINUTES=60
FGC_AUTH_BOOTSTRAP_USERNAME=<optional>
FGC_AUTH_BOOTSTRAP_PASSWORD=<optional>
```

连接字符串默认按本机 MySQL 约定使用 `127.0.0.1:3306` 和独立数据库 `forguncy_auth`，但实际值必须由部署环境提供。项目只负责在已连接的数据库中创建表；数据库本身由部署前置脚本或管理员创建。

JWT 规则：

- 算法固定为 HS256，不接受客户端指定算法。
- `sub` 为用户 ID，`name` 为用户名，附带随机 `jti`、`iat` 和 `exp`。
- 签发有效期默认 60 分钟，可通过 `FGC_JWT_EXPIRES_MINUTES` 调整。
- `FGC_JWT_SIGNING_KEY` 缺失或长度少于 32 个字符时拒绝启动对应操作。
- 校验必须验证签名和有效期；不配置额外 audience 校验，避免引入未要求的客户端注册流程。
- 生产环境必须由活字格站点的 HTTPS/网络边界保护登录接口；本阶段不实现限流和账号锁定。

## 7. 项目文件边界

新增项目预计包含：

```text
forguncy-server-api/
├── ForguncyServerApi.csproj
├── Api/AuthApi.cs
├── Application/AuthService.cs
├── Application/LoginModels.cs
├── Domain/AuthUser.cs
├── Infrastructure/AuthDbContext.cs
├── Infrastructure/UserRepository.cs
├── Security/PasswordHasher.cs
├── Security/JwtTokenService.cs
├── Configuration/AuthOptions.cs
├── sql/001-create-database.sql
├── README.md
└── tests/ForguncyServerApi.Tests/
```

项目不修改现有 `src/`、`e2e/` 或微信小程序配置。`ForguncyServerApi.csproj` 通过可覆盖的 `ForguncyBin` MSBuild 属性引用本机 8.0.4 程序集，默认值指向用户提供的安装目录；输出 DLL 不携带数据库凭据。

## 8. 验证与部署

实现采用测试驱动：先为密码哈希、JWT 服务和登录用例写失败测试，再实现最小代码并逐项转绿。

验证范围：

1. PBKDF2 正确密码通过，错误密码失败，禁用用户失败。
2. JWT 能签发并验证，过期令牌、错误签名和不支持算法失败。
3. 登录请求的 JSON/表单解析、成功响应、`400` 和统一 `401` 行为符合契约。
4. 反射检查 `AuthApi` 继承 `ForguncyApi`，仅有登录 Web API 方法，不存在外部签发/校验路由。
5. 使用本机 MySQL 建立数据库和 `jwt_users` 表，配置环境变量后执行一次真实登录 smoke test。
6. 使用 .NET 6 和活字格 8.0.4 本机程序集完成 Release 构建。
7. 按活字格设计器“文件 → 设置 → 自定义 Web Api → Upload Web Api Assembly”上传生成 DLL，并记录实际运行结果。

所有命令、退出码、测试数量、构建结果和真实运行阻塞项记录在 `reports/verification.md`；没有实际观察到的 DevTools 或活字格运行结果不作成功声明。

## 9. 非目标

- 不提供独立的 JWT 颁发 API。
- 不提供独立的 JWT 校验 API。
- 不实现用户注册、修改密码、找回密码、刷新令牌、撤销令牌或权限管理。
- 不把用户表复制到微信小程序 Storage，不修改当前小程序 Mock 登录流程。
- 不提交用户提供的 MySQL root 密码、JWT 密钥或任何真实账号密码。

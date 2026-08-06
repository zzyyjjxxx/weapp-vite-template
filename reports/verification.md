# 用地需求填报验证报告

日期：2026-07-29

## TDD 文档一致性

| 命令 | 退出码 | 实际结果 |
|---|---:|---|
| `pnpm test tests/smoke/product-shape.test.ts`（RED） | 1 | 1 个文件中 1/3 测试失败；README 仍是模板文案且不含“用地需求”，证明测试能捕获旧文档。 |
| `pnpm test tests/smoke/product-shape.test.ts`（文档 GREEN） | 0 | 1 个文件、3 个测试全部通过。 |
| `pnpm test tests/smoke/product-shape.test.ts`（环境配置 RED） | 1 | 新增旧 HTTP 环境配置断言后 1/4 测试失败，定位到未使用的 `src/shared/env.ts`。 |
| `pnpm test tests/smoke/product-shape.test.ts`（最终 GREEN） | 0 | 删除未使用的 HTTP 环境配置后，1 个文件、4 个测试全部通过。 |

## 审查修复 TDD

| 命令 | 退出码 | 实际结果 |
|---|---:|---|
| `pnpm test tests/unit/components/land-demand-wizard.test.ts tests/smoke/product-shape.test.ts`（RED） | 1 | 2 个文件各有 1 个失败：投资/单位能耗标签仍是旧文案，Mock 文档也没有说明本地草稿的 Store→Repository 例外。 |
| 同一聚焦命令（GREEN） | 0 | 2 个文件、14 个测试全部通过；精确标签和草稿架构说明均受测试保护。 |

## 静态、单元和构建门禁

以下命令均在 `D:\WorkProject\weapp-vite-template\.worktrees\land-demand-mini-program` 中分别执行。

| 命令 | 退出码 | 实际结果 |
|---|---:|---|
| `pnpm install --frozen-lockfile` | 0 | 锁文件无需更新，pnpm 11.17.0。 |
| `pnpm prepare` | 0 | Weapp-TailwindCSS 识别 Tailwind CSS 4.3.3，生成 `.weapp-vite` 支持文件。 |
| `pnpm typecheck:app` | 0 | `vue-tsc` 应用类型检查通过。 |
| `pnpm typecheck:e2e` | 0 | `tsc -p e2e/tsconfig.json` 通过。 |
| `pnpm typecheck`（审查修复复核） | 0 | 应用与 E2E 类型检查通过。 |
| `pnpm lint`（审查修复复核） | 0 | 零警告产品门禁通过：覆盖所有维护中的 `src` TS/Vue、测试/E2E TS、行业生成器与根配置。仅排除 SQL 生成产物 `industries.generated.ts`；其生成器仍受 lint，产物受精确字典测试保护。 |
| Node UTF-8 文档检查 | 0 | README、AGENTS、7 个来源文档和本报告共 10 个文件均可按 UTF-8 解码，包含“用地需求”且不含替换字符。 |
| `pnpm stylelint` | 0 | `src/**/*.{css,scss,vue,wxss}` 样式检查通过。 |
| `pnpm test`（审查修复复核） | 0 | 30 个测试文件、96 个测试全部通过。 |
| `pnpm test:coverage`（审查修复复核） | 0 | 30 个测试文件、96 个测试全部通过；语句 83.27%、分支 76.16%、函数 79.82%、行 84.15%。 |
| `pnpm build` | 0 | 微信小程序构建完成；主包 706 KB。 |
| `pnpm analyze:budget` | 0 | 包体预算检查通过。 |
| `pnpm verify`（审查修复聚合复核） | 0 | 32.2 秒内依次通过 prepare、组合类型检查、零警告产品 lint、stylelint、30 文件/96 测试、706 KB 构建和包体预算。 |

## 微信开发者工具运行时 E2E

| 命令 | 退出码 | 实际结果 |
|---|---:|---|
| `pnpm exec wv ide info`（Task 9 前置检查） | 1 | 非交互模式检测到登录失效，微信开发者工具返回 `message: re-login`。 |
| `pnpm test:e2e` | 1 | 运行 7 个串行用例时，第一个用例在 `miniProgram` fixture 初始化 60 秒后超时；其余 6 个未运行。CLI 报告“无法连接到当前项目的微信开发者工具自动化 websocket”，并要求确认目标项目窗口、关闭多余 DevTools 窗口或结束残留 auto 进程后重试。 |
| `pnpm exec wv screenshot --project ./dist --page pages/login/index --output .tmp/login.png --json`（Task 9） | 超时 | 34 秒边界后终止，没有产生 `.tmp/login.png`；只结束了该命令创建的两个孤立 Node 进程，没有关闭 DevTools 或修改用户配置。 |

因此运行时产品交互仍是**受环境前置条件阻塞、未通过**。Task 10 没有重复执行截图；由于 Task 9 没有得到可观察截图，`wv compare` 未执行，也没有生成、更新或声称任何登录页/确认页截图基线。重新验收前应只打开目标项目，确认微信开发者工具已登录且服务端口可用，再运行 `pnpm test:e2e`。

## 当前结论

- 文档一致性、类型检查、零警告产品 lint、样式、全部 Vitest、覆盖率、构建和包体预算通过。
- DevTools E2E 因自动化 websocket 不可连接而未完成，不能用静态构建结果替代。

## 审查修复第二轮

- TDD RED：`pnpm test tests/smoke/product-shape.test.ts` 退出 1，5 个测试中 1 个失败，明确指出 `lint:product` 未包含 `vitest.config.ts`。
- TDD GREEN：将 `vitest.config.ts` 和 `stylelint.config.js` 同时加入 `lint:product` 与 `lint:fix` 后，同一命令退出 0，5/5 通过。
- `pnpm lint`：退出 0；新增两个根配置后仍满足零警告门禁。
- `pnpm typecheck`、`pnpm stylelint`：均退出 0。
- `pnpm test`：退出 0，30 个文件、96 个测试通过。
- `pnpm build`：退出 0，主包 706 KB；`pnpm analyze:budget` 退出 0。
- `git diff --check`：退出 0，仅有 Windows LF→CRLF 转换警告。检查时 `git status --short` 只有 `package.json` 和 `tests/smoke/product-shape.test.ts` 两个预期修改；写入本段后本报告成为第三个预期修改。
- 本轮开始时 HEAD 为 `e350f2c fix: address land demand review findings`。运行时 E2E 未重复执行，继续沿用本报告记录的 `re-login`/websocket 环境阻塞，未声称截图。

## Git 检查与提交基线

- `git diff --check`：退出码 0；仅输出 Windows 工作区未来 LF→CRLF 转换警告，没有空白错误。
- 检查时 `git status --short` 精确列出 14 个预期修改文件：`AGENTS.md`、3 个文档、`eslint.config.js`、`package.json`、3 个源文件和 5 个测试文件。更新本验证报告后，`reports/verification.md` 也进入预期修改集合；没有无关或用户拥有文件。
- 审查修复开始时的 HEAD 为 `5177332 docs: align repository with land demand product`。
- 运行时 E2E 证据来自 `1b70044 test: add land demand runtime e2e`，其草稿所有权修复为 `7d4222a fix: keep persisted drafts query-owned`。
- 本报告不能在提交自身之前记录审查修复提交的最终 SHA；提交后由根任务独立运行 `git status --short` 和 `git log -1` 复核。
## 运行时阻塞修复（2026-07-29）

- RED：8 个聚焦测试文件按预期失败，分别捕获事件二次解包、首页误标
  tab、缺少固定入口、Storage 写删异常被吞、会话过期判断缓存和原生直接
  启动未守卫。
- GREEN：事件 detail、Storage、导航、直接页面守卫、认证及 Repository
  聚焦测试共 9 个文件/41 个测试通过。
- `pnpm prepare`、`pnpm typecheck:app`、`pnpm build` 均退出 0；构建主包
  706 KB。
- `pnpm verify` 退出 0：类型检查、零警告 lint、stylelint、34 个测试文件/
  110 个测试、构建、生成产物契约和包体预算全部通过。
- `pnpm test:coverage` 退出 0：34 个测试文件/110 个测试通过；语句
  85.33%、分支 77.41%、函数 80.85%、行 86.26%。
- 构建后 `dist/app.json` 的 `entryPagePath` 为 `pages/login/index` 且没有
  `tabBar`；生成 dispatcher 包含单次 `return e.detail`，业务页面和表单组件
  生成脚本不再读取 `.detail`。
- 本轮没有重试微信开发者工具 E2E；此前记录的 `re-login`/websocket
  环境阻塞仍有效，不将静态或构建结果冒充运行时验收。

## 运行时审查加固（2026-07-29）

- Storage 键不存在仍返回空值，但真正的读取异常会向 Repository/Mutation
  传播；保存无法确认旧记录是否存在时不会执行任何写入。
- 暂存、发送验证码、校验验证码和最终持久化均通过前台会话操作守卫；过期
  会话不会调用动作回调，并跳转携带 `returnTo` 的登录页。
- 托管 CI 在 `pnpm build` 后执行 `pnpm verify:generated-runtime`，再执行
  `pnpm analyze:budget`；对应顺序受 smoke 测试保护。
- 聚焦 RED 捕获 4 个失败，最终 GREEN 为 4 个文件/22 个测试通过；`pnpm verify`
  退出 0，依次通过 prepare、应用与 E2E 类型检查、零警告 lint、stylelint、
  34 个文件/113 个测试、707 KB 构建、生成产物契约及包体预算。
- 本轮未重试 DevTools E2E，继续记录为外部 `re-login`/websocket 前置条件阻塞。

## 业务完整性最终审计修复（2026-07-29）

- 严格 TDD RED：7 个聚焦文件/33 个测试中出现 11 个预期失败，分别捕获篡改草稿覆盖认证企业、身份补丁可写、数值整数位溢出、宁波市无法反选、Query 取消未传播、缺少只读详情/真实成功页，以及 E2E 缺少冷启动、修改再暂存和截图定义。
- 聚焦 GREEN：8 个文件/36 个测试通过，并通过应用与 E2E 组合类型检查。
- `pnpm prepare`、`pnpm typecheck`、`pnpm lint`、`pnpm stylelint` 均退出 0；产品 lint 保持零警告。
- `pnpm test` 退出 0：34 个测试文件/124 个测试全部通过。
- `pnpm test:coverage` 退出 0：34 个测试文件/124 个测试通过；语句 85.12%、分支 77.18%、函数 81.17%、行 86.01%。
- `pnpm build` 退出 0，主包 714 KB；`pnpm verify:generated-runtime` 与 `pnpm analyze:budget` 均退出 0。
- 已提交记录现在通过独立 `mode=view` 只读确认页查看；成功页仅在 Query 返回当前企业的已提交记录后展示企业名称、提交时间与成功状态。企业四个归属字段只读且在草稿恢复/补丁后由认证会话重新断言。
- E2E 静态合约增加冷启动会话恢复、已提交记录修改再暂存，以及 `.tmp/e2e-login.png`/`.tmp/e2e-review.png` Driver 截图调用。本轮遵照最终审计要求没有重试 DevTools E2E；既有 `re-login`/websocket 外部阻塞仍有效，未生成基线、未声称运行时通过。

## 冷启动 E2E 语义修复（2026-07-29）

- 已核对安装的 `weapp-ide-cli`/`@weapp-vite/miniprogram-automator` 类型：`MiniProgramLike` 暴露 `callWxMethod`；本地微信 API 类型包含 `wx.restartMiniProgram({ path })`。
- TDD RED：Driver 与静态合约 2 个文件/7 个测试中有 2 个预期失败，证明原用例仅使用同一运行时的 `reLaunch`。
- GREEN：`MiniProgramDriver.restart` 调用 `callWxMethod('restartMiniProgram', { path: normalizedPath })` 并轮询等待目标页面；仅容忍重启过渡期间已知的协议超时或扩展上下文失效，其他调用错误继续失败。冷启动场景改用 `restart`。
- 聚焦 Driver/静态合约 2 个文件/9 个测试通过，包括重启调用响应失效后等待目标页面，以及不吞掉不支持的 API 错误；`pnpm typecheck:e2e` 与零警告 `pnpm lint` 通过。
- 最终 `pnpm test` 退出 0：34 个测试文件/127 个测试通过；`pnpm build` 退出 0，主包 714 KB；`pnpm verify:generated-runtime` 与 `pnpm analyze:budget` 均退出 0。
- 本轮没有连接或关闭微信开发者工具、没有修改安全设置，也没有重试现场 E2E；既有 `re-login`/websocket 阻塞仍为运行时未验收状态。

## DevTools 登录后运行时复核（2026-07-29）

- 用户完成登录后，微信开发者工具 RC `2.02.2607171` 能打开仓库根项目并在模拟器显示登录页；修复了生成 WXML 中可选链泄漏，构建后 `rg "\\?\\." dist -g "*.wxml"` 无匹配。
- TDD RED/GREEN：新增生成产物可选链断言、App Service Storage 清理、TDesign `loading/disabled` 可操作等待和嵌套组件树定位覆盖；最终聚焦 5 个文件/23 个测试通过。
- `pnpm verify` 退出 0：依次通过 prepare、应用与 E2E 类型检查、零警告 lint、stylelint、34 个测试文件/130 个测试、715 KB 构建、生成产物契约和包体预算。
- `pnpm test:coverage` 退出 0：34 个测试文件/130 个测试通过；语句 85.85%、分支 77.97%、函数 81.89%、行 86.57%。
- `git diff --check` 退出 0；仅报告工作区未来 LF→CRLF 转换提示，没有空白错误。
- 真实 Playwright/Automator 曾进入首个串行场景并生成 `.tmp/e2e-login.png`（4,141 字节），已人工检查为正常登录页；`.tmp/e2e-review.png` 未生成。
- `pnpm test:e2e` 仍未通过。当前 RC 的 `cli auto --auto-port 10535` 在前台可报告成功，但后台/工具进程中会以 0 退出且不持续监听 10535，或连接后首个 App 协议命令超时；因此 9 个串行场景没有完成全量验收。
- `wv screenshot`（通过 workspace 工具调用，目标 `pages/login/index`、输出 `.tmp/wv-login.png`）退出 `-1`/超时，明确报告“无法连接到当前项目的微信开发者工具自动化 websocket”；没有生成该截图。由于没有可用的 `wv screenshot` 当前图，`wv compare` 未执行，也未创建或更新视觉基线。
- 结论：静态、单元、覆盖率、构建和产物检查均通过；真实 DevTools E2E 与 `wv screenshot/compare` 仍是当前 RC 自动化服务的外部阻塞，不能标记为通过。
## Forguncy 8.0.4 JWT login verification — 2026-08-03

All commands below were run in `D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login`. Secret values, connection strings, signing keys, and bootstrap credentials were neither printed nor recorded.

### Release unit tests

Command (run from `forguncy-server-api`):

```powershell
dotnet test .\ForguncyServerApi.sln --configuration Release --no-restore --logger "console;verbosity=normal"
```

- Exit code: `1`.
- Counts reported by the test runner: total `48`; passed `41`; failed `7`; skipped `0` (the runner did not print a skipped count, and `41 + 7 = 48`).
- The test host and both project assemblies built for Release, but all seven failures were in `JwtTokenServiceTests` because the test process could not load `System.IdentityModel.Tokens.Jwt, Version=6.8.0.0`.
- Failed tests: `CreateToken_contains_user_claims_and_validate_returns_them`; `ValidateToken_rejects_an_expired_token`; `ValidateToken_rejects_a_token_signed_with_another_key`; `ValidateToken_rejects_a_token_that_is_not_yet_valid`; `ValidateToken_rejects_a_malformed_token`; `ValidateToken_rejects_a_token_using_a_non_hs256_algorithm`; and `ValidateToken_rejects_a_token_with_another_issuer`.
- Read-only diagnostic: neither `tests\ForguncyServerApi.Tests\bin\Release\net6.0` nor the two Release `.deps.json` manifests contained a JWT/IdentityModel assembly entry. `ForguncyServerApi.csproj` references `System.IdentityModel.Tokens.Jwt.dll`, `Microsoft.IdentityModel.Tokens.dll`, and `Microsoft.IdentityModel.JsonWebTokens.dll` through `$(ForguncyBin)`. This is consistent with a Release test-output dependency problem; no source, project, dependency, or restore change was made.

### Release upload artifact

Command (run from `forguncy-server-api`):

```powershell
dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

- Exit code: `0`.
- Output: `ForguncyServerApi -> ...\bin\Release\net6.0\ForguncyServerApi.dll`; `0` warnings and `0` errors.

Artifact listing command:

```powershell
Get-ChildItem .\bin\Release\net6.0 -File | Select-Object Name,Length
```

| Name | Length (bytes) |
|---|---:|
| `ForguncyServerApi.deps.json` | 443 |
| `ForguncyServerApi.dll` | 46592 |
| `ForguncyServerApi.pdb` | 27864 |

`ForguncyServerApi.dll` is present with the required custom-assembly name. The complete file listing contains no generated settings, credential, connection-string, key, or secret-bearing file.

### MySQL preflight and schema/login smoke

Read-only preflight command:

```powershell
Test-NetConnection -ComputerName 127.0.0.1 -Port 3306
```

- Exit code: `0`; `TcpTestSucceeded : True` for `127.0.0.1:3306`.
- `mysqld` processes were present, but no `mysql` command was available on `PATH`.
- The names (not values) of environment variables matching the permitted task-scoped/MySQL prefixes were checked; none were present.
- Blocker: no usable local MySQL client and no task-scoped credentials were available. `sql\001-create-database.sql` was not executed, `forguncy_auth.jwt_users` was not queried, and no real login request was sent. The schema script itself exists (`98` bytes).

### Active Forguncy designer/runtime check

- Read-only process checks initially found a Forguncy 8.0.4 designer executable, but it did not expose an independently targetable designer window.
- Windows-app inspection found two visible designer windows, both identified in their titles as Forguncy `10.0.103`, not 8.0.4. The subsequently refreshed 8.0.4 process query no longer returned a usable window.
- Blocker: no usable, active Forguncy 8.0.4 designer/app was available for a version-correct upload. The Release DLL was not uploaded to the visible 10.x projects.
- Consequently, no `/customapi/authapi/login` HTTP response, database-backed bootstrap login, or absence check for `/customapi/authapi/issue` and `/customapi/authapi/validate` was observed. These runtime checks are not passed.

### Unresolved blockers

1. MySQL listens locally, but the scoped schema/login smoke is blocked by the absence of both a usable local client and task-scoped credentials.
2. A visible, independently targetable Forguncy 8.0.4 app is unavailable; only 10.0.103 designer windows were observed, so no upload or HTTP route verification was performed.

## Forguncy 8.0.4 Release JWT test-runtime fix — 2026-08-03

All commands below were run in `D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login`. No source behavior, routes, packages, credentials, production references, or upload artifacts were changed.

### Baseline and test-only fix

The original Task 7 Release test run failed with exit code `1`: total `48`, passed `41`, failed `7`. Every failure was in `JwtTokenServiceTests` because the test host could not load `System.IdentityModel.Tokens.Jwt, Version=6.8.0.0`.

`dotnet clean .\ForguncyServerApi.sln --configuration Release` exited `0` and removed only generated Release `bin`/`obj` output. The installed .NET 6 SDK rejects `--no-restore` for `dotnet clean`, so no unsupported switch was used for that cleanup command.

Only `forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj` changed. It now has an overridable `ForguncyBin` property defaulting to `D:\Program Files\Forguncy 8.0.4\Website\bin` and test-only `Private=true` references to these existing host files:

- `System.IdentityModel.Tokens.Jwt.dll`
- `Microsoft.IdentityModel.Tokens.dll`
- `Microsoft.IdentityModel.JsonWebTokens.dll`
- `Microsoft.IdentityModel.Logging.dll`

The production project's host references remain `Private=false`.

### Release unit-test rerun

```powershell
dotnet test .\ForguncyServerApi.sln --configuration Release --no-restore --logger "console;verbosity=normal"
```

- Exit code: `0`.
- Counts: total `48`; passed `48`; failed `0`; skipped `0` (the runner did not print a skipped count, and all 48 discovered tests passed).
- The seven `JwtTokenServiceTests` now execute and pass in a clean Release test output.

### Release production artifact

```powershell
dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
Get-ChildItem .\bin\Release\net6.0 -File | Select-Object Name,Length
```

- Build exit code: `0` with `0` warnings and `0` errors.

| Name | Length (bytes) |
|---|---:|
| `ForguncyServerApi.deps.json` | 443 |
| `ForguncyServerApi.dll` | 46592 |
| `ForguncyServerApi.pdb` | 27864 |

The production artifact contains zero copied host IdentityModel DLLs and zero secret-named files. The added references are therefore confined to the test output.

### Remaining MySQL and designer blockers

The JWT test-output blocker is resolved. The prior runtime blockers remain unchanged: no usable local MySQL client or task-scoped credentials are available for schema/login smoke, and no independently targetable Forguncy 8.0.4 designer/app is available (only 10.0.103 designer windows were previously observed). No database action, upload, or HTTP route verification was attempted by this fix.

## Final verification after the API-boundary fix — 2026-08-03

The verified code baseline was commit `01a36d859a843f6a415fc088f9e980ef098f6d0f`
(`01a36d8 fix: cover login request read failures`). This was the code HEAD before
the documentation-only commit that records this final evidence.

### Current Release unit tests

Command (run from `forguncy-server-api`):

```powershell
dotnet test .\ForguncyServerApi.sln --configuration Release --no-restore --logger "console;verbosity=normal" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

- Exit code: `0`.
- Counts: total `54`; passed `54`; failed `0`; skipped `0`.

### Current Release production build and artifact

Command (run from `forguncy-server-api`):

```powershell
dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

- Exit code: `0`.
- Result: Release production build succeeded with `0` warnings and `0` errors
  using the Forguncy 8.0.4 `ForguncyBin` path shown above.

Artifact listing command:

```powershell
Get-ChildItem .\bin\Release\net6.0 -File | Select-Object Name,Length
```

| Name | Length (bytes) |
|---|---:|
| `ForguncyServerApi.deps.json` | 443 |
| `ForguncyServerApi.dll` | 48640 |
| `ForguncyServerApi.pdb` | 28200 |

The clean current-code build contained only these three production artifact
files. The earlier `46592`-byte DLL and `27864`-byte PDB listing is retained in
the historical evidence above; it does not describe the clean `01a36d8` build.

### Repository and documentation checks

- `git diff --check` passed with exit code `0`.
- A sanitized scan of the current tree and reachable feature history found no
  user-provided credential literal. No credential value was printed or
  reproduced during the scan or in this report.
- Focused README/report phrase scans passed for the PowerShell-compatible
  database command, HTTPS/trusted-boundary warning, code baseline, test/build
  counts, artifact names, and explicit runtime blockers.

### Remaining runtime blockers

1. MySQL runtime verification at `127.0.0.1:3306` remains blocked: no usable MySQL client or task-scoped credentials are available. No schema, database-backed login, or live MySQL check was performed.
2. Upload and HTTP verification remain blocked because no usable, active Forguncy 8.0.4 designer/runtime is available. No DLL upload, live login request, direct HTTP request, or route-absence smoke check was performed.

These blockers are not passes, and the unit/build evidence above is not a claim of live MySQL, HTTP, upload, or Forguncy runtime verification.

## Forguncy config connection documentation and final verification - 2026-08-04

All commands below were run in
`D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login`. No
connection string, credential, signing key, or bootstrap value was printed or
recorded.

### Documentation assertions (RED then GREEN)

Before editing `forguncy-server-api/README.md`, focused `rg` scans found the
old `FGC_AUTH_MYSQL_CONNECTION` environment-setting command, while the exact
`config.item='ssl'` and `config table` source phrases were absent. This was the
expected RED state. The generic word `value` already appeared elsewhere in the
README, so it was not treated as proof of the required config-source guidance.

After the edit, focused scans verified that the README contains the Forguncy
config table source, `item='ssl'`, `value`, and the existing HTTPS/trusted
network-boundary warning. Separate absence scans confirmed that neither the old
`FGC_AUTH_MYSQL_CONNECTION` setting command nor its former required-variable
wording remains. The documentation states that `enable` is intentionally not a
lookup condition.

### Release unit tests

Command (run from `forguncy-server-api`):

```powershell
dotnet test .\ForguncyServerApi.sln --configuration Release --no-restore --logger "console;verbosity=normal" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

- Exit code: `0`.
- Test runner result: total `63`; passed `63`; failed `0`.

### Release production build and artifact

Command (run from `forguncy-server-api`):

```powershell
dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
Get-ChildItem .\bin\Release\net6.0 -File | Select-Object Name,Length
```

- Build exit code: `0`.
- Build result: `0` warnings and `0` errors.

| Name | Length (bytes) |
|---|---:|
| `ForguncyServerApi.deps.json` | 443 |
| `ForguncyServerApi.dll` | 49664 |
| `ForguncyServerApi.pdb` | 28424 |

### Reachable-history credential scan

A read-only scan over revisions reachable from `HEAD` searched for MySQL-style
connection strings containing a host/source and a user or password field. It
reported `22` candidate file revisions. No candidate filename content or value
was printed or recorded, and this pattern-only scan does not classify those
historical candidates as live credentials. The Task 3 README and verification
changes contain no connection-string or credential literal.

### Runtime boundaries

This evidence is limited to documentation assertions, unit tests, and the
Release artifact. No live MySQL schema, database-backed login, active Forguncy
8.0.4 designer upload, or HTTP login request was performed. The existing
blockers remain: no usable local MySQL client or task-scoped credentials, and
no usable active Forguncy 8.0.4 designer/runtime. These are not passes.

## Forguncy auth final-review cache fix - 2026-08-04

All commands below were run in
`D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login`. No
connection string, credential, signing key, or bootstrap value was printed or
recorded.

### Focused RED/GREEN coverage

The initial focused Release run exited `1` with `13` tests discovered: `9`
passed and `4` failed for the expected reasons. The public `AuthOptions`
constructor assertion found the obsolete second overload, and the three cache
tests could not load the not-yet-implemented cache type.

After the implementation, the focused Release run exited `0`: `27` tests
passed, with `0` failed and `0` skipped. The selection covered `AuthOptions`,
the retryable async cache, the public auth API surface, and database context
initialization behavior.

### Full Release unit tests

```powershell
dotnet test .\ForguncyServerApi.sln --configuration Release --no-restore --logger "console;verbosity=normal" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

- Exit code: `0`.
- Test runner result: total `67`; passed `67`; failed `0`; skipped `0`.

### Release production build

```powershell
dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

- Exit code: `0`.
- Result: Release production build succeeded with `0` warnings and `0` errors.

This verification did not use a live MySQL database or Forguncy runtime and
does not claim an uploaded or HTTP-observed login flow.

## Shared initialization cancellation fix - 2026-08-04

The new regression test first failed against the cached implementation: the
focused run had `1` failed and `3` passed because the cancellation-aware cache
overload did not exist. After the fix, the focused Release run passed `4/4`.
The test verifies that one canceled waiter does not cancel the shared
initialization observed by another waiter.

The full Release suite then passed `68/68` with `0` failures and `0` skipped.
The Release production build completed with `0` warnings and `0` errors, and
`git diff --check` completed successfully. No live MySQL, Forguncy designer,
upload, or HTTP verification was performed.

## c_userinfo bootstrap removal and deployment evidence - 2026-08-04

All commands below were run in
`D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login`. No live
database operation, Forguncy upload, or HTTP request was performed, and no
credential or connection-string value was introduced or recorded.

### TDD RED/GREEN evidence

The unchanged focused baseline passed `16/16`. After updating the required
configuration and API/source-surface assertions, the exact focused Release
filter exited `1`: total `17`, passed `13`, failed `4`. The failures identified
the obsolete five-argument `AuthOptions` constructor, bootstrap environment
reads, the initializer type/startup schema write, and legacy database guidance.

The first GREEN rerun found one stale test fixture that still assigned the
removed `AuthUser.IsEnabled` compatibility field. The fixture was updated to
the real `c_userinfo.isopen` model. The exact focused filter then exited `0`:
total `17`, passed `17`, failed `0`, skipped `0`.

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~AuthOptionsTests|FullyQualifiedName~AuthApiSurfaceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

### Final Release unit tests and build

```powershell
dotnet test .\ForguncyServerApi.sln --configuration Release --no-restore --logger "console;verbosity=minimal" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

- Final test exit code: `0`; total `62`, passed `62`, failed `0`, skipped `0`.
- Final build exit code: `0`; `0` warnings and `0` errors.

The final `bin\Release\net6.0` production artifact listing was:

| Name | Length (bytes) |
|---|---:|
| `ForguncyServerApi.deps.json` | 443 |
| `ForguncyServerApi.dll` | 43008 |
| `ForguncyServerApi.pdb` | 27484 |

### Source and repository checks

A case-insensitive scan of maintained production `*.cs`, `*.md`, and `*.sql`
under `forguncy-server-api`, excluding tests and generated `bin`/`obj` output,
found no stale legacy table, PBKDF2, initializer, schema-creation, bootstrap
environment, or deleted SQL-script guidance. `git diff --check` exited `0`;
Git printed only expected LF-to-CRLF working-copy notices and reported no
whitespace errors.

The verified deployment contract is read-only: Forguncy supplies the existing
database and `c_userinfo` table selected through `config.item='ssl'`.
`username` maps to `creditCode`, passwords use lowercase middle-16 MD5, and
`isopen` must equal integer `1`. The API does not create, alter, seed, or
initialize database content.

## c_userinfo MD5 login refactor - 2026-08-04

The Task 3 RED run was intentionally executed before removing the legacy
initializer. The focused configuration/API-surface run discovered `17` tests:
`13` passed and `4` failed on the expected obsolete constructor, bootstrap
environment reads, initializer type, and legacy deployment guidance.

After the refactor, the focused Release run was:

```powershell
dotnet test .\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~AuthOptionsTests|FullyQualifiedName~AuthApiSurfaceTests|FullyQualifiedName~AuthDbContextTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin' --logger "console;verbosity=minimal"
```

- Exit code: `0`.
- Result: `21` passed, `0` failed, `0` skipped.

The full Release suite then passed `62/62` with `0` failures and `0` skipped:

```powershell
dotnet test .\ForguncyServerApi.sln --configuration Release --no-restore --logger "console;verbosity=minimal" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

The production Release build passed with `0` warnings and `0` errors:

```powershell
dotnet build .\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

The resulting `bin\Release\net6.0` artifact listing was:

| Name | Length (bytes) |
|---|---:|
| `ForguncyServerApi.deps.json` | 443 |
| `ForguncyServerApi.dll` | 43008 |
| `ForguncyServerApi.pdb` | 27484 |

`git diff --check` completed with exit code `0`. A source scan over production
files (excluding tests and generated `bin`/`obj`) found no
`AuthDbInitializer`, `EnsureCreated`, `jwt_users`, bootstrap environment,
`forguncy_auth`, or PBKDF2 references. No live MySQL, Forguncy upload, or HTTP
verification was performed.

## Forguncy 8.0.4 .NET Framework 4.7.2 migration - 2026-08-04

Commands run from `D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login`:

- `dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release` exited `0`; 62 tests passed, 0 failed, 0 skipped.
- `dotnet build .\forguncy-server-api\ForguncyServerApi.csproj --configuration Release --no-restore` exited `0`; 0 warnings and 0 errors.
- A stale API scan found no `net6.0`, `FrameworkReference`, `System.Text.Json`, `ArgumentNullException.ThrowIfNull`, `MD5.HashData`, `Convert.ToHexString`, `Task.WaitAsync`, `HasJsonContentType`, or `ServerVersion.AutoDetect` references in the production project outside generated `bin`/`obj`.
- Built metadata confirmed `ForguncyServerApi.dll` targets `.NETFramework,Version=v4.7.2`; EF Core and Pomelo target `.NETStandard,Version=v2.0`; MySqlConnector targets `.NETFramework,Version=v4.7.1`; Newtonsoft.Json targets `.NETFramework,Version=v4.5`; and the Forguncy SDK reference is version `8.0.4.0`.
- `git diff --check` exited `0`; only expected line-ending notices were emitted.
- Runtime DLL upload, live Forguncy hosting, and live MySQL/HTTP login verification were not performed.

## SqlSugar 5.1.4.111 database migration - 2026-08-05

- The focused red test first failed because `AuthSqlSugarClientFactory` did not exist.
- `dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --filter FullyQualifiedName~SqlSugarPersistenceTests` exited `0`; 3 tests passed.
- `dotnet clean .\forguncy-server-api\ForguncyServerApi.csproj --configuration Release` exited `0` to remove the previous Release output.
- `dotnet build .\forguncy-server-api\ForguncyServerApi.csproj --configuration Release --no-restore` exited `0`; 0 warnings and 0 errors.
- `dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release` exited `0`; 61 tests passed, 0 failed, 0 skipped.
- Resolved production database packages are exactly `SqlSugar 5.1.4.111` and `MySql.Data 8.0.29`; the Release output contains no EF Core, Pomelo, or MySqlConnector DLLs.
- Release assembly metadata confirmed `SqlSugar.dll` assembly/file version `5.1.4.111` and `MySql.Data.dll` assembly/file version `8.0.29.0`.
- Live Forguncy upload and live MySQL login verification were not performed.

## MySql.Data driver update - 2026-08-05

- Updated the production and test projects from `MySql.Data 8.0.29` to the requested `MySql.Data 8.0.30`; `SqlSugar` remains `5.1.4.111`.
- The Release output was cleaned and rebuilt; the resulting `MySql.Data.dll` metadata was verified as version `8.0.30.0`.
- Final serial verification: production build exited `0` with 0 warnings and 0 errors; full test run exited `0` with 61/61 tests passed; the output contained no EF Core, Pomelo, or MySqlConnector DLLs.

## Forguncy DLL version audit - 2026-08-05

- Compared all 23 managed DLLs in `forguncy-server-api\bin\Release\net472` against same-name DLLs in `D:\Program Files\Forguncy 8.0.4\Website\bin` and, where the SDK dependency is kept in the companion directory, `Website\designerBin`.
- 14 same-name DLLs matched on both AssemblyVersion and FileVersion; 0 same-name DLLs differed.
- 9 DLLs were new dependencies absent from the Forguncy directories: the custom API, `SqlSugar 5.1.4.111`, `MySql.Data 8.0.30`, and their MySQL compression/protocol dependencies. These must be uploaded with the API DLL.

## Forguncy custom API discovery compatibility fix - 2026-08-05

- Added a red reflection-surface test. Before the fix, `ForguncyServerApi.dll`
  exposed `AuthApi` plus 17 implementation types; the test failed as expected.
- Made database, JWT, configuration, and application implementation types
  internal, leaving only `ForguncyServerApi.Api.AuthApi` exported. The test then
  passed.
- Moved the logging implementation out of `AuthApi`. The API type's reflected
  fields and method signatures no longer reference
  `Microsoft.Extensions.Logging.Abstractions`, which was absent from the
  Forguncy 8.0.4 designer directory.
- Full Release test suite exited `0`; 63 tests passed, 0 failed, 0 skipped.
- Release build exited `0`; 0 warnings and 0 errors.
- An isolated reflection probe using only the rebuilt API DLL and
  `Website\designerBin\GrapeCity.Forguncy.ServerApi.dll` reported one exported
  type, successful `AuthApi` activation, a parameterless `Task Login` method,
  and the `[Post]` attribute.
- The DLL version audit still reports 23 output DLLs, 14 exact host matches,
  and 0 same-name mismatches.
- Live upload and live HTTP login verification remain pending; the designer
  process was not modified by this diagnostic run.

## Public implementation surface requested - 2026-08-05

- Restored `public` visibility for the application, configuration, domain,
  infrastructure, security, request-reader, cache, and diagnostics types.
- The compiler compatibility shim `IsExternalInit` is also public, so the
  maintained production source contains no internal type declarations.
- The focused public-surface test was red before the visibility change and
  passed afterward; the rebuilt assembly exposes 21 public types.
- Full Release test suite exited `0`; 63 tests passed, 0 failed, 0 skipped.
- Release build exited `0`; 0 warnings and 0 errors.
- A bare-DLL designer probe now correctly fails on the expected public
  dependency requirement (`Microsoft.Extensions.Logging.Abstractions 3.1.7.0`).
  A probe with the complete Release DLL bundle reports 21 exported types,
  successful `AuthApi` activation, and one `[Post]` parameterless `Task Login`
  route.

## Forguncy JWT refresh documentation and release verification - 2026-08-06

Worktree: `D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login`

Scope for this cycle:

- Updated `forguncy-server-api/README.md` to document both `login` and `refresh`.
- Updated `forguncy-server-api/tests/ForguncyServerApi.Tests/Api/AuthApiSurfaceTests.cs` with README contract assertions before editing the README.
- No live Forguncy designer upload, host interaction, or HTTP round-trip was claimed or observed in this cycle.
- Secrets, connection strings, user passwords, and signing keys were not printed or recorded.

### Focused RED

Command:

```powershell
dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~AuthApiSurfaceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Exit code: `1`

Output:

```text
  ForguncyServerApi -> D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\bin\Release\net472\ForguncyServerApi.dll
  ForguncyServerApi.Tests -> D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\tests\ForguncyServerApi.Tests\bin\Release\net472\ForguncyServerApi.Tests.dll
D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\tests\ForguncyServerApi.Tests\bin\Release\net472\ForguncyServerApi.Tests.dll (.NETFramework,Version=v4.7.2)的测试运行
VSTest 版本 17.14.0 (x64)

正在启动测试执行，请稍候...
总共 1 个测试文件与指定模式相匹配。
  失败 ForguncyServerApi.Tests.Api.AuthApiSurfaceTests.Readme_documents_refresh_route_request_response_and_stateless_limitations [3 ms]
  错误消息:
   Assert.Contains() Failure
Not found: POST /customapi/authapi/refresh
...
失败!  - 失败:     1，通过:    16，已跳过:     0，总计:    17，持续时间: 1 s - ForguncyServerApi.Tests.dll (net472)

有可用的工作负载更新。有关详细信息，请运行 `dotnet workload list`。
[xUnit.net 00:00:05.40]     ForguncyServerApi.Tests.Api.AuthApiSurfaceTests.Readme_documents_refresh_route_request_response_and_stateless_limitations [FAIL]
```

Observed RED reason: the README still documented only `POST /customapi/authapi/login` and explicitly said the API did not expose refresh.

### Focused GREEN

Command:

```powershell
dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~AuthApiSurfaceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Exit code: `0`

Output:

```text
  ForguncyServerApi -> D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\bin\Release\net472\ForguncyServerApi.dll
  ForguncyServerApi.Tests -> D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\tests\ForguncyServerApi.Tests\bin\Release\net472\ForguncyServerApi.Tests.dll
D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\tests\ForguncyServerApi.Tests\bin\Release\net472\ForguncyServerApi.Tests.dll (.NETFramework,Version=v4.7.2)的测试运行
VSTest 版本 17.14.0 (x64)

正在启动测试执行，请稍候...
总共 1 个测试文件与指定模式相匹配。

已通过! - 失败:     0，通过:    17，已跳过:     0，总计:    17，持续时间: 1 s - ForguncyServerApi.Tests.dll (net472)

有可用的工作负载更新。有关详细信息，请运行 `dotnet workload list`。
```

### Full Release test

Command:

```powershell
dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Exit code: `0`

Output:

```text
  ForguncyServerApi -> D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\bin\Release\net472\ForguncyServerApi.dll
  ForguncyServerApi.Tests -> D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\tests\ForguncyServerApi.Tests\bin\Release\net472\ForguncyServerApi.Tests.dll
D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\tests\ForguncyServerApi.Tests\bin\Release\net472\ForguncyServerApi.Tests.dll (.NETFramework,Version=v4.7.2)的测试运行
VSTest 版本 17.14.0 (x64)

正在启动测试执行，请稍候...
总共 1 个测试文件与指定模式相匹配。

已通过! - 失败:     0，通过:   107，已跳过:     0，总计:   107，持续时间: 1 s - ForguncyServerApi.Tests.dll (net472)

有可用的工作负载更新。有关详细信息，请运行 `dotnet workload list`。
```

### Release build

Command:

```powershell
dotnet build .\forguncy-server-api\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Exit code: `0`

Output:

```text
  ForguncyServerApi -> D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\bin\Release\net472\ForguncyServerApi.dll

已成功生成。
    0 个警告
    0 个错误

已用时间 00:00:00.80

有可用的工作负载更新。有关详细信息，请运行 `dotnet workload list`。
```

Final DLL path:

```text
D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\bin\Release\net472\ForguncyServerApi.dll
```

### Reflection check against the Forguncy 8.0.4 SDK assembly

Command:

```powershell
$dllDir = 'D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\bin\Release\net472'
$forguncyBin = 'D:\Program Files\Forguncy 8.0.4\Website\bin'
$forguncyAssembly = Join-Path $forguncyBin 'GrapeCity.Forguncy.ServerApi.dll'
[void][System.Reflection.Assembly]::LoadFrom($forguncyAssembly)
Get-ChildItem -LiteralPath $dllDir -Filter '*.dll' | ForEach-Object {
    [void][System.Reflection.Assembly]::LoadFrom($_.FullName)
}
$assembly = [System.Reflection.Assembly]::LoadFrom((Join-Path $dllDir 'ForguncyServerApi.dll'))
$type = $assembly.GetType('ForguncyServerApi.Api.AuthApi', $true)
Write-Output ("DLL=" + (Join-Path $dllDir 'ForguncyServerApi.dll'))
Write-Output ("TYPE=" + $type.FullName)
Write-Output ("BASE=" + $type.BaseType.FullName)
foreach ($method in ($type.GetMethods([System.Reflection.BindingFlags]'Public, Instance, DeclaredOnly') | Where-Object { $_.Name -in @('Login', 'Refresh') } | Sort-Object Name)) {
    $hasPost = [bool]($method.GetCustomAttributes($false) | Where-Object { $_.GetType().FullName -eq 'GrapeCity.Forguncy.ServerApi.PostAttribute' })
    $paramCount = $method.GetParameters().Count
    Write-Output (("METHOD={0};RET={1};PARAMS={2};POST={3}" -f $method.Name, $method.ReturnType.FullName, $paramCount, $hasPost))
}
```

Exit code: `0`

Output:

```text
DLL=D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\bin\Release\net472\ForguncyServerApi.dll
TYPE=ForguncyServerApi.Api.AuthApi
BASE=GrapeCity.Forguncy.ServerApi.ForguncyApi
METHOD=Login;RET=System.Threading.Tasks.Task;PARAMS=0;POST=True
METHOD=Refresh;RET=System.Threading.Tasks.Task;PARAMS=0;POST=True
```

### Diff check

Command:

```powershell
git diff --check
```

Exit code: `0`

Output:

```text
warning: in the working copy of 'forguncy-server-api/README.md', LF will be replaced by CRLF the next time Git touches it
warning: in the working copy of 'forguncy-server-api/tests/ForguncyServerApi.Tests/Api/AuthApiSurfaceTests.cs', LF will be replaced by CRLF the next time Git touches it
```

Observed result: no whitespace or patch-format errors; only Git line-ending warnings for the edited tracked files.

## 全面视觉重构验收（2026-07-31）

- 视觉范围：登录页、企业工作台、五步填报、确认提交、提交成功、错误、
  空状态、加载状态、验证码弹窗与公共页面骨架均已统一为蓝白渐变、规划插画、
  悬浮卡片和清晰步骤层级；业务仍为项目真实五步，没有加入参考图中的注册、
  忘记密码、图形验证码或附件上传。
- TDD RED：`pnpm exec vitest run tests/smoke/visual-system.test.ts` 退出 1，
  3/3 测试按预期失败，分别捕获缺少规划插画/登录文案、缺少共享视觉令牌、
  以及旧步骤名称和旧进度结构。
- TDD GREEN：同一聚焦命令退出 0，1 个文件/3 个测试通过。
- `pnpm verify` 退出 0：依次通过 `prepare`、应用与 E2E 类型检查、零警告
  `lint`、`stylelint`、35 个测试文件/133 个测试、微信小程序构建、生成产物
  契约和包体预算；主包为 753 KB。
- `pnpm test:coverage` 退出 0：35 个测试文件/133 个测试通过；语句
  85.85%、分支 77.97%、函数 81.89%、行 86.57%。
- 插画源文件经压缩后为 `src/assets/land-planning-hero.webp`（20,054 字节）；
  构建实际输出 `dist/land-planning-hero-eqexouqf.webp`，不是悬空模板路径。
- 实现阶段提交为 `55ef3da feat: redesign land demand mini program`；
  用户已有的 `skills-lock.json` 修改没有暂存或提交。

### 本轮微信开发者工具运行时结果

- `pnpm test:e2e` 退出 1：9 个串行用例中首个用例连接
  `ws://127.0.0.1:9643` 失败，1 个失败、其余 8 个未运行；错误明确要求目标
  项目窗口以自动化模式打开。
- `pnpm open` 与
  `pnpm exec wv open . --non-interactive --no-mcp --debug` 均在等待
  DevTools 时超时，现场未出现可连接的 9643 监听端口。
- `pnpm exec wv screenshot --project ./dist --page pages/login/index
  --output .tmp/login-redesign.png --json` 退出 1，报告“无法连接到当前项目的
  微信开发者工具自动化 websocket”，没有生成截图。
- 因没有新的真实运行时截图，`wv compare` 未执行，也没有创建或更新视觉基线。
  当前结论为静态、单元、类型、构建与包体门禁通过；DevTools E2E 和运行时
  截图受外部自动化连接前置条件阻塞，不能标记为通过。

## DevTools 控制台与运行时稳定性复核（2026-07-31）

- 修复 Summer Compiler 虚拟 slot wrapper、页面属性类型、空 Query 记录、
  TDesign 弹窗属性、冷启动认证恢复及 App Service 自动化连接问题；生成产物
  不再包含可执行的 `wx.getSystemInfoSync` 调用。
- 登录、工作台、五步填报、提交成功和错误页完成加载后逐页预检；产品控制台
  警告/错误关键字扫描为 0。MCP 连接预检自身触发的系统 API 弃用提示不来自
  产品产物，未计作产品通过证据。
- 独立 Automator 端口 `9651` 上运行 `pnpm test:e2e`，退出 0：9 个串行
  场景全部通过，耗时 46.8 秒；覆盖登录、草稿恢复、冷启动会话、字段显隐、
  园区/行业选择、融资校验、提交查看及修改再保存。
- E2E 完成后扫描 6 份本轮 DevTools 运行时日志，Summer Compiler、WXML
  缺失、组件属性不兼容、Query `undefined` 和产品系统 API 弃用等关键字命中
  数均为 0。
- 最终 `pnpm verify` 退出 0：`prepare`、应用与 E2E 类型检查、零警告
  `lint`、`stylelint`、35 个测试文件/149 个测试、微信小程序构建、生成产物
  契约和包体预算全部通过；主包为 749 KB。
- 登录页截图仅在页面就绪和控制台预检完成后采集；测试夹具只断开自己的
  Automator 连接，不关闭用户已打开的微信开发者工具。

## UI 修正与控制台清零复核（2026-07-31，本轮实际结果）

- 关闭并重新打开当前项目后，确认微信开发者工具服务端口为 `40637`、
  Automator 端口为 `9651`；避免使用旧的内存编译结果。
- 修正首页插画为完整铺满 hero 的 `aspectFit`，填报页使用紧凑骨架；步骤条
  改为五等分且不横向溢出，底部操作栏改为固定吸底；详情页和成功页改用
  `replace`，避免复用旧页面实例造成 `routeReady` 未初始化、详情页卡住。
- TDesign 输入、单选、行业级联和弹窗属性均提供首帧可用的字符串/数组；逐页
  Automator console/exception 扫描结果：登录页、工作台、填报页、成功页及
  填报各步骤/验证码弹窗均为 `warnings=[]`、`exceptions=[]`。成功页因当前
  测试记录为草稿而按业务守卫返回工作台，仍无警告或异常。
- `pnpm test:e2e` 退出码 0：9/9 串行场景通过，耗时 49.3 秒，包含最终验证
  提交、成功页、详情页查看及修改再保存。
- `pnpm verify` 退出码 0（71 秒）：35 个测试文件、152 个测试通过；应用与
  E2E 类型检查、零警告 lint、stylelint、生成运行时契约和包体预算均通过；
  微信小程序主包 739 KB。
- 实际检查截图：`.tmp/e2e-login.png`、`.tmp/e2e-home-latest.png`、
  `.tmp/e2e-basic-latest.png`、`.tmp/e2e-review.png`；均在页面就绪后采集。

## 首页与填报页视觉复核（2026-07-31，本轮最终结果）

- 首页 hero 使用 `aspectFill` 配合右侧定位（`left: 5%`、`width: 108%`），
  插画定位点与参考图保持右侧构图，避免原先 `aspectFit` 留白导致的错位。
- 五步进度条将圆点、连接线、文字拆分为独立布局：五等分节点、连接线不再占用
  圆点宽度，文字 `text-align: center` 且不横向溢出。
- 填报步骤组件启用 `styleIsolation: 'apply-shared'`，使共享的白色卡片、
  `32rpx` 圆角、标题蓝色竖标、卡片阴影在真实运行时生效；底部操作区继续固定吸底。
- `pnpm vitest run tests/unit/components/land-demand-wizard.test.ts tests/smoke/visual-system.test.ts`：
  2 个测试文件、18 个测试通过。
- `pnpm verify`：退出码 0，35 个测试文件、153 个测试通过；应用/E2E 类型检查、
  零警告 lint、stylelint、739 KB 主包构建、生成运行时契约和包体预算均通过，耗时
  66.5 秒。
- 构建后真实运行时逐页扫描（登录、首页、填报第 1–5 步、成功页，监听 Automator
  `console` 与 `exception`）：`warnings=[]`、`exceptions=[]`。
- 微信开发者工具服务端口 `40637`、Automator `9651`；完整 `pnpm test:e2e`
  首次因会话协议超时失败，关闭并重新打开同一端口后重试退出码 0，9/9 场景通过，
  用时 49.3 秒。
- 最终截图：`.tmp/home-final.png`、`.tmp/fill-final.png`、`.tmp/e2e-login.png`、
  `.tmp/e2e-review.png`，均在页面完成渲染后采集并人工核对。

## 登录、工作台与提交链路最终复核（2026-07-31，本轮实际结果）

- 登录页插画右移量调整为稳定的参考构图，并为用户名、密码输入补齐用户和锁图标；
  工作台移除重复规划插画，退出操作改为紧凑原生按钮，进度条使用 1–5 等分节点，
  按 Store 中的本地草稿当前步骤高亮并显示“当前第 N 步 / 共 5 步”。
- 填报页根容器为固定底栏预留 `220rpx` 底部空间；确认页“修改”改为紧凑胶囊操作，
  与标题垂直居中且不再挤出右侧空白；验证码弹窗链路由真实 E2E 覆盖。
- `pnpm stylelint`、`pnpm lint` 均退出 0；聚焦视觉/向导/确认测试为 3 个文件、
  23 个测试通过。
- 最终 `pnpm verify` 退出 0（59.3 秒）：35 个测试文件、153 个测试通过；
  应用与 E2E 类型检查、零警告 lint、stylelint、微信构建、生成运行时契约和包体预算
  全部通过，主包 743 KB。
- 构建后关闭并重开微信开发者工具（服务端口 `40637`、Automator `9651`），
  逐页扫描登录、工作台和填报第 1–5 步，运行时结果为
  `RUNTIME_BLOCKING=[]`；成功页直接访问因业务守卫返回工作台，未产生异常。
- 最终 `pnpm test:e2e` 退出 0：9/9 场景通过（48.0 秒），包含验证码弹窗出现、
  输入 `123456`、验证提交、成功页、详情查看及修改再保存。
- 页面完成渲染后采集并人工复核：`.tmp/login-visual-current.png`、
  `.tmp/home-visual-current.png`、`.tmp/fill-visual-current.png`；其中填报截图已滚动
  至底部，内容卡片与固定底栏之间可见留白。用户已有的 `skills-lock.json` 未暂存。

## UI 重构与运行时复核（2026-08-03，本轮最终结果）

- `pnpm verify` 退出 0（57.4 秒）：35 个测试文件、153 个单元测试通过；类型检查、
  零警告 lint、stylelint、微信构建、生成运行时契约和包体预算全部通过，主包 766 KB。
- 微信开发者工具服务端口为 `40637`，Automator 端口为 `9643`。构建后重新打开项目，
  扫描登录、工作台、填报第 1–5 步和成功页，控制台结果为 `RUNTIME_BLOCKING=[]`。
- 清理运行时日志后执行 `pnpm test:e2e`，退出 0：9/9 场景通过（48.1 秒），覆盖当前步骤
  必填拦截、融资条件、验证码弹窗、输入 `123456`、验证提交、成功页、详情查看和修改再保存。
- 实机截图已写入正确目录 `C:\Users\hp\.codex\worktrees\f809\weapp-vite-template\.tmp`：
  `login-visual-current.png`、`home-visual-current.png`、`fill-visual-current.png`、
  `verification-dialog-current.png`；截图确认插画、输入图标、日期区间、步骤进度、底栏留白和
  验证码弹窗正文均已渲染。
- `skills-lock.json` 为用户已有修改，未暂存。

## 登录与验证码对齐复核（2026-08-03，本轮结果）

- 登录页已移除“演示环境”角标和底部本地提示；用户名/密码图标改为固定高度的
  TDesign `prefix-icon` 插槽容器，输入文字与图标垂直居中。
- 验证码弹窗将“验”图标移入标题插槽与标题同排，验证码输入、Mock 验证码和底部操作
  使用 TDesign 内容/按钮插槽并保持等宽布局。
- `pnpm test:e2e` 退出 0：9/9 场景通过（约 1.1 分钟）。
- `pnpm verify` 退出 0（60.7 秒）：35 个测试文件、153 个测试通过，主包 766 KB。
- 构建后运行时扫描结果为 `RUNTIME_PATH=pages/home/index`、`RUNTIME_BLOCKING=[]`；
  最新截图为 `.tmp/login-visual-current.png` 与 `.tmp/verification-dialog-current.png`。

## 验证码弹窗 footer 溢出修复（2026-08-03，本轮最终结果）

- footer 的取消/确认插槽改为显式 flex 容器，内部 TDesign 按钮使用 `block` 填充各自半区，
  修复确认按钮超出弹窗右边界的问题。
- 修复后实机截图确认按钮完整位于弹窗内，验证码提交按钮仍可被自动化定位。
- `pnpm test:e2e` 退出 0：9/9 场景通过（约 1.4 分钟）。
- `pnpm verify` 退出 0（57.8 秒）：35 个测试文件、153 个测试通过，主包 766 KB。
- 构建后运行时扫描结果：`RUNTIME_PATH=pages/home/index`、`RUNTIME_BLOCKING=[]`。

## 验证码输入类 Dialog 对齐官方实现（2026-08-03，本轮最终结果）

- 验证码弹窗 footer 改为 TDesign `t-dialog` 原生 `confirm-btn` / `cancel-btn` 对象配置，
  显式使用 `button-layout="horizontal"`；输入内容继续通过 `content` slot 承载业务字段。
- 移除 footer 内嵌自定义 `t-button` 和手写 flex 宽度，避免小程序 slot 包裹层导致按钮溢出、重叠。
- 使用 TDesign `tId` 保留验证码提交的稳定运行时定位，提交事件改由 `t-dialog` 的 `confirm` 事件触发。
- `pnpm test:e2e` 退出 0：9/9 场景通过（50.1 秒）。
- `pnpm verify` 退出 0（47.8 秒）：35 个测试文件、153 个测试通过，主包 766 KB。
- 构建后运行时扫描结果：`RUNTIME_PATH=pages/home/index`、`RUNTIME_BLOCKING=[]`。
- 最新验证码弹窗截图：`.tmp/verification-dialog-native.png`。

## 首页步骤轨道与已填步骤跳转（2026-08-03，本轮最终结果）

- 首页五步进度轨道改为五列等宽布局，连接线从节点中心延伸，已填步骤增加可选中态；
  `.tmp/home-step-selection.png` 为实机截图。
- 已填进度以内的节点可点击选择；点击首页主操作会携带 `step` 查询参数，填报页解析后
  定位到对应步骤。实机验证点击第 1 步后进入 `pages/land-demand/index?step=1`，
  运行时 `currentStep=1`。
- `pnpm verify` 退出 0（58.3 秒）：35 个测试文件、154 个测试通过，类型检查、
  零警告 lint、stylelint、生成运行时契约和包体预算全部通过，主包 767 KB。
- 微信开发者工具服务端口 `40637`、Automator `9651`；最新运行时扫描结果为
  `RUNTIME_PATH=pages/home/index`、`RUNTIME_BLOCKING=[]`。
- `pnpm test:e2e` 退出 0：9/9 场景通过（42.3 秒），包含已有填报、步骤切换、融资条件、
  验证码提交、详情查看及修改再保存回归。用户已有的 `skills-lock.json` 未暂存。

## 首页已填进度与当前选择分离（2026-08-03，本轮最终结果）

- Store 新增可持久化的 `progressStep`，`currentStep` 回退到已填写步骤时不再降低最高进度；
  首页增加 `completed / selected / pending` 三种节点状态。
- 实机选择第 2 步后，运行时仍显示“已填写至第 5 步 / 共 5 步”，节点 3–5 保持完成态，
  节点 2 显示选中态；截图为 `.tmp/home-step-selection-middle.png`。
- `pnpm verify` 退出 0（76.9 秒）：35 个测试文件、155 个测试通过，生成运行时契约和包体
  预算均通过，主包 768 KB。
- `pnpm test:e2e` 退出 0：9/9 场景通过（46.6 秒）；最终运行时扫描为
  `RUNTIME_PATH=pages/home/index`、`RUNTIME_BLOCKING=[]`。用户已有的 `skills-lock.json` 未暂存。

## Task 4 final review repair - 2026-08-06

This repair round addressed the two Important review findings in the isolated
worktree `D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login`.
No runtime implementation or dependency was changed. No secret, connection
string, password, or signing key was printed or recorded. No live Forguncy
designer upload, host interaction, or HTTP round-trip was observed.

Repair implementation commit:

```text
524bdfe63cd81d1857f3bb08b08baca74f3da483 docs: address JWT refresh review findings
```

The README now requires both `login` and `refresh` routes to be exposed only
through the Forguncy HTTPS endpoint or an equivalent trusted network boundary.
The surface test now scopes contract assertions to the `## Refresh contract`
section through the next same-level heading, checks JSON and form request
examples, checks the five token fields and exact-response/no-user wording,
parses the refresh success JSON to require exactly those five properties, and
checks `FGC_JWT_REFRESH_EXPIRES_MINUTES`/`10080`, `stateless`, and
`cannot be revoked before expiry`.

### TDD repair evidence

The first focused attempt after adding `JObject` parsing exited `1` with the
test-only compile error `CS0103: The name 'JObject' does not exist in the
current context`. Adding `using Newtonsoft.Json.Linq` corrected that test
setup before the intended RED run.

Focused RED command:

```powershell
dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~AuthApiSurfaceTests" -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Exit code `1`; actual failure was the expected missing HTTPS wording:

```text
Assert.Contains() Failure
Not found: Expose both the login and refresh routes only through the Forguncy site's HTTPS endpoint or an equivalent trusted network boundary.
失败: 1，通过: 16，已跳过: 0，总计: 17
```

After the README update and whitespace-tolerant HTTPS assertion, the same
focused command exited `0`:

```text
已通过! - 失败:     0，通过:    17，已跳过:     0，总计:    17
```

### Release verification

Full Release test command:

```powershell
dotnet test .\forguncy-server-api\tests\ForguncyServerApi.Tests\ForguncyServerApi.Tests.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Exit code `0`; `107` passed, `0` failed, `0` skipped.

Release build command:

```powershell
dotnet build .\forguncy-server-api\ForguncyServerApi.csproj --configuration Release --no-restore -p:ForguncyBin='D:\Program Files\Forguncy 8.0.4\Website\bin'
```

Exit code `0`; `0` warnings and `0` errors. The final DLL was:

```text
D:\WorkProject\weapp-vite-template\.worktrees\forguncy-jwt-login\forguncy-server-api\bin\Release\net472\ForguncyServerApi.dll
```

The Release DLL reflection probe exited `0` and reported:

```text
TYPE=ForguncyServerApi.Api.AuthApi
BASE=GrapeCity.Forguncy.ServerApi.ForguncyApi
METHOD=Login;RET=System.Threading.Tasks.Task;PARAMS=0;POST=True
METHOD=Refresh;RET=System.Threading.Tasks.Task;PARAMS=0;POST=True
```

`git diff --check` exited `0`. Its only output was the expected LF-to-CRLF
working-copy warnings for edited tracked files; no whitespace errors were
reported.

Post-repair commit status showed only the pre-existing user-owned item:

```text
?? forguncy-server-api/.vs/
```

The `.vs` directory was not staged, removed, or modified.

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

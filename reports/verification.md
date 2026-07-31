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

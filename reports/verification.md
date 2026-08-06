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

## 项目级 MCP stdio 握手修复（2026-08-03）

- 项目级 `.codex/config.toml` 与 `.mcp.json` 改用 `scripts/weapp-vite-mcp.mjs`，绕过
  `wv mcp` 启动时写入 stdout 的引导文本，确保 stdout 仅包含 JSON-RPC。
- `pnpm exec eslint scripts/weapp-vite-mcp.mjs` 退出 0；`git diff --check` 退出 0。
- 按项目配置启动服务后，首行即为合法 `initialize` JSON-RPC 响应，stderr 为空。
- 使用 `@modelcontextprotocol/sdk` 的 `StdioClientTransport` 实测连接成功：服务端
  `@weapp-vite/mcp@2.0.0`，可列出 35 个工具。
- 主工作树首次检查发现未安装 `node_modules`；执行 `pnpm install --frozen-lockfile` 退出 0，
  `postinstall/prepare` 均成功。安装后从主工作树再次完成同样的 MCP 握手验证。

## TDesign 弹框、选择器与截图文案调整（2026-08-03，本轮实际结果）

- 清空确认改为 TDesign `t-dialog` 标准标题/描述/取消/确认属性；验证码改为 TDesign
  标题+描述+输入内容；所有单选字段统一通过共享 `SinglePicker` 使用 TDesign
  `t-cell`、`t-picker`、`t-picker-item`。多选园区的后续 Picker 改动见本报告下一节。
- 移除登录页副说明、首页 hero/`LAND DEMAND`/填报时间与“五步完成信息填报”文案，
  以及填报详情页标题下的企业提交状态副标题；生成的 WXML 中未再出现这些文案。
- `pnpm verify` 退出码 0（55.4 秒）：35 个测试文件、155 个测试通过；应用与 E2E
  类型检查、零警告 lint、stylelint、WeChat 构建、生成运行时契约和包体预算均通过，
  主包 767 KB。
- `pnpm test:e2e` 已实际尝试但退出码 1：微信开发者工具自动化端口
  `ws://127.0.0.1:11228` 未连接，首个场景连接失败，后续 8 个场景未执行；本轮没有
  截图或真实 DevTools 交互通过证据，不能将运行时视觉验收标记为通过。
- 修正 `vite.config.ts` 中既有 TDesign 兼容补丁的产物路径定位，使最终构建产物不再
  包含 `getSystemInfoSync`；用户已有的 `skills-lock.json` 修改保持未动。

## DevTools 首次启动页面缺失排查（2026-08-03，本轮实际结果）

- 复现原因：启动 `pnpm dev:open` 后，`wv dev --open` 会先打开 DevTools，再异步生成 `dist`；生成尚未完成时，`dist/pages/login/index.wxml` 和 `dist/app.json` 暂时不存在，因此模拟器显示 `pages/login/index.wxml not found`。
- 等待当前工程首次编译完成后，确认 `dist/app.json`、`dist/pages/login/index.wxml`、`dist/pages/login/index.js`、`dist/pages/login/index.json` 均已生成，时间戳为 2026-08-03 15:25:14。
- 使用微信开发者工具 CLI 重新打开 `C:\Users\hp\.codex\worktrees\1498\weapp-vite-template`，命令返回 `√ open`；Automator 真实运行时连接返回 `RUNTIME_CONNECTED`，当前页面为 `pages/login/index`。
- 结论：本次不是页面源码缺文件，而是 DevTools 在首轮构建完成前提前加载。当前工程已完成生成并重新打开；本轮未将 Automator 截图 API 超时误报为视觉验收通过。

## 可调剂园区改为 TDesign Picker（2026-08-03，本轮实际结果）

- `land-info-step.vue` 已移除 `t-checkbox-group`，改用 `MultiPicker`；组件内部使用
  TDesign `t-cell`、`t-picker` 和 `t-picker-item`。园区仍按原有数组格式保存，支持多选及
  “宁波市”与具体区域互斥规则。
- `pnpm lint` 退出码 0；`pnpm stylelint` 退出码 0；定向 Vitest 退出码 0：4 个测试文件、
  42 个测试通过；`pnpm build` 退出码 0；`pnpm verify:generated-runtime` 退出码 0。
- 构建产物 `dist/features/land-demand/components/land-info-step.wxml` 已确认包含
  `MultiPicker`，未包含 `t-checkbox-group`。本轮未宣称真实 DevTools 视觉截图验收通过。

## 页面布局回归修正（2026-08-03，本轮实际结果）

- 恢复首页顶部服务卡片及“用地需求填报”标题行原有结构；移除“当前登录企业”标签，企业名称
  与统一社会信用代码两行直接对应显示。
- 所有 TDesign `t-cell` 选择项统一设置标题区最小宽度和内容区断行规则，避免“细分方向”等长
  标题逐字竖排；真实性承诺复选框改为无底部分隔线。
- 移除验证码弹窗外部的“验证码已发送，请在弹窗中完成验证”提示；成功页两个底部按钮使用
  相同的 flex 宽度。
- `pnpm lint`、`pnpm stylelint` 均退出码 0；定向 Vitest 4 个测试文件、42 个测试通过；
  `pnpm build` 和 `pnpm verify:generated-runtime` 均退出码 0，主包 773 KB。

## 首页卡片旧运行时排查（2026-08-03，本轮实际结果）

- 当前 `src/pages/home/index.vue` 与 `dist/pages/home/index.wxml` 均包含顶部 hero 卡片、企业卡片、
  `u-card` 填报卡和标题行；`dist` 生成时间为 16:32:00。用户截图中的这些节点缺失，属于旧运行时
  页面，不是当前源码结构。
- 使用微信开发者工具 CLI 清理当前项目编译缓存退出码 0，并重新打开
  `C:\Users\hp\.codex\worktrees\1498\weapp-vite-template` 退出码 0；文件观察进程已确认监听当前
  1498 工程路径。
- `wv screenshot` 本轮因 DevTools Automator WebSocket 未连接退出码 124，未将运行时截图验收标记为通过。

## 首页红框内容与卡片保留修正（2026-08-03，本轮实际结果）

- 删除首页顶部红框区域、`LAND DEMAND`、填报卡片副标题和填报时间文字；保留企业信息卡与填报卡
  的 `u-card` 白色背景。
- “用地需求填报”继续使用 `u-section-heading`，生成的 `dist/app.wxss` 仍包含左侧蓝色竖线
  `u-section-heading::before`。
- 生成的首页 WXML 已确认包含两个 `u-card`，且不再包含 `home__hero`、`LAND DEMAND`、
  “五步完成信息填报”或“填报时间”。
- 本轮 `pnpm lint`、`pnpm stylelint`、定向 42 个单测、`pnpm build` 和
  `pnpm verify:generated-runtime` 均通过；主包 771 KB。

## DevTools 刷新流程（2026-08-03，本轮实际结果）

- `pnpm build` 退出码 0，主包 771 KB。
- 微信开发者工具 CLI 对当前工程依次执行 `close`、`cache --clean compile`，均退出码 0；随后清理了本轮超时遗留的、仅绑定当前工程的 `wv open/cli open` 进程，并启动了新的 CLI 实例。
- 清理后再次执行 `cli.bat open --project ... --trust-project`，退出码 0，实际输出 `√ open`。
- 运行时截图自动化仍因 DevTools Automator WebSocket 未连接而无法提供视觉验收证据；未将其标记为通过。当前 `dist/pages/home/index.wxml` 已确认保留企业卡片、填报卡片及 `u-section-heading`，且不含已删除的首页红框文字。
- 已启动一次 `pnpm dev` 监听进程；首次监听构建完成后，`dist/app.json` 与首页 WXML 均已生成，后续改动走热编译，不再重启 DevTools。

## 可调剂园区取消 Picker 多选（2026-08-04，本轮实际结果）

- `可调剂园区` 恢复为 TDesign `t-checkbox-group`，支持直接勾选和取消；其他单选字段继续使用 `SinglePicker`。
- 删除不再使用的 `src/components/ui/multi-picker/index.vue`，同步更新组件契约测试。
- `pnpm lint`、`pnpm stylelint` 通过；定向 2 个测试文件共 20 个测试通过。
- 开发监听生成的 `dist` 已确认包含 `t-checkbox-group` 且不含 `MultiPicker`。

## 必填星号统一到字段名称后（2026-08-04，本轮实际结果）

- 基本信息、用地需求、投资项目、融资及联系人中的普通必填标签统一为“字段名称 + 红色星号”；TDesign `required` 选择器保持同样的后置显示。
- `.field__required` 明确设置为行内显示并改用左侧间距，避免星号单独换行。
- `pnpm lint`、`pnpm stylelint` 和定向 15 个单测通过；完整 `pnpm build` 通过，主包 767 KB，生成的 `dist/app.wxss` 已包含 `.field__required { display: inline; }`。

## WXSS 缓存路径恢复（2026-08-04，本轮实际结果）

- DevTools 报错为缓存引用已不存在的 `dist/styles/tailwind.wxss`，并伴随旧 `common.js` 缓存导致的 `require_common.x is not a function`。
- 仅重启开发监听后，`dist/app.json`、`dist/app.wxss`、登录页和首页产物均重新生成，当前产物不包含 `tailwind.wxss` 引用。
- 在产物稳定后执行 `cli.bat cache --clean compile` 和 `cli.bat open --project ... --trust-project`，均退出码 0，分别输出 `√ cleancache`、`√ open`；未重启开发者工具。

## WXSS 兼容路径稳定生成（2026-08-04，本轮实际结果）

- 在 `vite.config.ts` 增加构建后兼容产物：将完整 `dist/app.wxss` 同步到 `dist/styles/tailwind.wxss`，用于兼容 DevTools 热编译残留路径，不改变实际页面引用和样式。
- `pnpm lint`、`pnpm stylelint`、定向 15 个单测和 `pnpm build` 均通过；完整构建确认兼容文件存在且与 `app.wxss` 大小一致。
- 恢复 `pnpm dev` 后首次监听构建确认 `dist/app.json` 与 `dist/styles/tailwind.wxss` 均生成；随后 `cli.bat cache --clean compile`、`cli.bat open --project ... --trust-project` 均返回成功。

## Wevu vendor 模块运行时重载（2026-08-04，本轮实际结果）

- 运行时错误指向 `weapp-vendors/wevu-template.js`，检查确认当前 `dist/weapp-vendors/wevu-template.js` 存在，且页面 JS 的相对引用正确；问题属于已运行页面未重新注册 vendor 模块。
- 对当前工程执行 `cli.bat close`、`cache --clean compile`、`open --project ... --trust-project`，均退出码 0；重新加载后 vendor 文件仍存在，开发监听和项目文件观察进程均正常。

## 星号位置与多选已选内容布局（2026-08-04，本轮实际结果）

- 普通字段星号不再使用会撑满剩余空间的 flex-grow，改为紧跟字段文字；标题使用次级文字色和较小字号，输入内容保持主文字色和更清晰的字号层级。
- `可调剂园区` 已选园区内容移动到标题下方、复选项上方；无选择时不显示空的已选内容块。
- `pnpm lint`、`pnpm stylelint`、定向 15 个单测和完整 `pnpm build` 均通过；生成 WXML/CSS 已确认顺序与样式规则正确，`cli.bat cache --clean compile` 和 `open` 均成功。

## 输入项与选择项样式对齐（2026-08-04，本轮实际结果）

- 普通输入字段的标签改为与 TDesign `t-cell` 类似的行内 flex 结构，必填星号固定为同行元素，避免星号单独换行。
- 输入字段标签统一为 32rpx/48rpx 常规字重，输入控件左右内边距统一为 32rpx；选择字段容器取消额外外层间距，统一与选择项对齐。
- `pnpm lint`、`pnpm stylelint`、定向 15 个单测、完整 `pnpm build` 和 `pnpm verify:generated-runtime` 均通过；主包 768 KB。构建后的 `dist/app.wxss` 已包含新的 `field__label`、`field--control` 与 `field__required` 规则。
- 完整构建后恢复 `pnpm dev` 监听；首次监听构建完成后再次确认 `dist/app.json`、登录页 WXML 及新的 WXSS 规则均存在。随后 `cli.bat cache --clean compile` 与 `cli.bat open --project ... --trust-project` 均返回成功。

## Wevu vendor ENOENT 缓存残留清理（2026-08-04，本轮实际结果）

- 附件日志中的 6 个缺失 vendor 文件来自微信开发者工具旧编译依赖图；当前稳定 `dist` 中的 JS 已不再引用它们，`dist/weapp-vendors` 当前仅生成 `wevu-watch.js` 与 `request-globals-wevu-web-apis-shared.js`，两者均存在。
- 官方微信开发者工具 CLI 执行 `cache --clean compile --project ...` 成功，实际输出包含 `√ IDE server has started` 与 `√ cleancache`。
- 通过当前服务端口执行 `/v2/resetfileutils` 返回 HTTP 200，随后执行 `/open?projectpath=...` 返回 HTTP 200；未重启微信开发者工具主进程。
- 最终静态检查确认 `dist` 中不存在对 `request-globals-runtime.js`、`wevu-base.js`、`wevu-computed.js`、`wevu-router.js`、`wevu-store.js`、`wevu-template.js` 的残留引用。未将本轮结果冒充为真实视觉截图验收。

## 项目启动（2026-08-04，本轮实际结果）

- 当前稳定 `dist` 已确认包含 `app.json`、登录页 `index.wxml` 和 `wevu-watch.js`。
- 执行微信开发者工具 CLI `open --project ... --trust-project --port 40637 --non-interactive` 成功，实际输出 `√ open`。
- 当前工程文件监听进程已启动并指向 `C:\Users\hp\.codex\worktrees\1498\weapp-vite-template`；本轮未重启开发者工具主进程。

## 输入与选择字段标题统一（2026-08-04，本轮实际结果）

- 原因确认：选择字段使用 TDesign `t-cell`，标题默认规格为 `32rpx / 48rpx / 主文字色`；输入字段外层使用自定义 `.field__label`，原规格为 `28rpx / 44rpx / 次文字色`。
- `.field__label` 与 `.field__required` 已统一为 TDesign 标题的 `32rpx / 48rpx`，标题改为主文字色、常规字重；输入、Picker、Cell 和多选标题现在使用同一套视觉规格。
- 先新增失败测试（15 个通过、1 个失败），再完成样式修改；修改后定向测试 16 个全部通过，`pnpm lint`、`pnpm stylelint`、`pnpm build` 和 `pnpm verify:generated-runtime` 均通过，主包 768 KB。
- 已执行微信开发者工具 `cache --clean compile` 和 `open`，均成功；本轮 Automator 连接等待 30 秒超时，未将运行时截图验收标记为通过。

## Success page action buttons (2026-08-04, actual results)

- Fixed the success-page action row in `src/pages/land-demand/success.vue`: both TDesign buttons now use the same width class; action wrappers use `width: 0`, `flex: 1 1 0`, and the second wrapper has an explicit `margin-left: 16rpx` instead of relying on flex `gap`.
- Generated `dist/pages/land-demand/success.wxss` contains the equal-width and inset rules, and generated WXML contains the shared button class twice.
- `pnpm vitest run tests/unit/components/land-demand-wizard.test.ts` passed: 17 tests; `pnpm stylelint`, `pnpm lint`, `pnpm build`, and `pnpm verify:generated-runtime` all exited 0. Build main package: 768 KB.
- WeChat DevTools CLI `close` and `open --project ... --trust-project --port 40637 --non-interactive` exited 0. Automator connection was not claimed: port 40637 is occupied by the IDE CLI server, so no runtime screenshot evidence was recorded in this round.

## Success page button overflow follow-up (2026-08-04, actual results)

- The previous `class` attribute was replaced with TDesign's `t-class` so the width rule reaches the internal button node; each flex cell also clips component overflow with `overflow: hidden`.
- The generated success WXML contains `t-class="land-demand-success__button"` twice, and generated WXSS contains the explicit equal-width, 16rpx separation, 100% width, and clipping rules.
- The regression test was updated first and failed before the source fix; after the fix the focused suite passed 17 tests. `pnpm stylelint`, `pnpm lint`, `pnpm build`, and `pnpm verify:generated-runtime` passed.
- After build, `cli.bat cache --clean compile --project ... --non-interactive` returned `√ cleancache`, and `cli.bat open --project ... --trust-project --port 40637 --non-interactive` returned `√ open`.

## Success page button edge clipping follow-up (2026-08-04, actual results)

- Removed the action-cell `overflow: hidden` rule because it clipped the TDesign button's right rounded edge; kept `t-class` on the internal TDesign button and added `width: 100%`, `min-width: 0`, and `max-width: 100%`.
- The focused regression suite passed 17 tests; `pnpm stylelint`, `pnpm lint`, `pnpm build`, and `pnpm verify:generated-runtime` passed. Generated WXSS/WXML were checked for the final width rules and two `t-class` bindings.
- Ran `cli.bat cache --clean compile --project ... --non-interactive` and `cli.bat open --project ... --trust-project --port 40637 --non-interactive`; both returned success. No runtime screenshot claim was made because Automator remains unavailable.

## DevTools lifecycle/404 session reset (2026-08-04, actual results)

- The current generated package still contains `dist/assets/land-planning-hero.webp`, the login WXML uses the literal `/assets/land-planning-hero.webp` path, and `node scripts/verify-generated-runtime.mjs` passed.
- Closed the project with the official CLI, ran `cache --clean compile`, and reopened the exact worktree project. All three commands returned success; the final IDE process was responsive and port `40637` was listening for the project.
- The MCP Automator connection timed out and captured zero console entries, so physical-device behavior and the full 404 URL remain unobserved. The generic `cachedata`/`LifeCycle.load` text alone does not identify which resource is returning 404.

## Real-device login hero asset packaging (2026-08-04, actual results)

- Root cause confirmed from the project configuration and generated output: `project.config.json` had `setting.ignoreUploadUnusedFiles: true`, while the hero image is bound through a runtime JS value (`src="{{landPlanningHero}}"`). The simulator can read the local `dist` file, but real-device package filtering can omit this dynamically referenced asset.
- Changed `ignoreUploadUnusedFiles` to `false` and added a regression assertion. The focused suite passed 19 tests; `pnpm stylelint`, `pnpm lint`, `pnpm build`, and `pnpm verify:generated-runtime` passed.
- Verified `dist/land-planning-hero-eqexouqf.webp` exists and the generated runtime path is `/land-planning-hero-eqexouqf.webp`. Ran `cli.bat cache --clean compile --project ... --non-interactive` and `cli.bat open --project ... --trust-project --port 40637 --non-interactive`; both returned success. Physical-device display was not directly observed in this environment.

## Real-device hero 404 static path fix (2026-08-04, actual results)

- Moved the hero asset into `public/assets/land-planning-hero.webp` and changed the login WXML to use the literal `/assets/land-planning-hero.webp` path; removed the hashed JS runtime asset import.
- The build now produces `dist/assets/land-planning-hero.webp` (20,054 bytes), generated login WXML contains the static path once, and the old root hashed asset is no longer generated.
- Added a static-path and asset-existence regression assertion; the focused suite passed 20 tests. `pnpm stylelint`, `pnpm lint`, `pnpm build`, and `pnpm verify:generated-runtime` passed.
- Ran `cli.bat cache --clean compile --project ... --non-interactive` and `cli.bat open --project ... --trust-project --port 40637 --non-interactive`; both returned success. Physical-device display was not directly observed in this environment.

## Login label aligned with prefix icon (2026-08-04, actual results)

- Adjusted `.login__field-label` from `padding-left: 0` to `padding-left: 32rpx`, aligning the `用户名` and `密码` labels with the left edge of their prefix icons without restoring the previous excessive 68rpx offset.
- Generated login WXSS contains `padding-left: 32rpx`; the focused suite passed 18 tests. `pnpm stylelint`, `pnpm lint`, `pnpm build`, and `pnpm verify:generated-runtime` passed.
- Ran `cli.bat cache --clean compile --project ... --non-interactive` and `cli.bat open --project ... --trust-project --port 40637 --non-interactive`; both returned success. No runtime screenshot claim was made because Automator remains unavailable.

## Login field label left alignment (2026-08-04, actual results)

- Removed the login label's extra `padding-left: 68rpx`; generated login WXSS now has `padding-left: 0`, so `用户名` and `密码` align with the form content area's left edge.
- Added the alignment assertion to the login typography contract; the focused suite passed 18 tests. `pnpm stylelint`, `pnpm lint`, `pnpm build`, and `pnpm verify:generated-runtime` passed.
- Ran `cli.bat cache --clean compile --project ... --non-interactive` and `cli.bat open --project ... --trust-project --port 40637 --non-interactive`; both returned success. No runtime screenshot claim was made because Automator remains unavailable.

## Login field typography aligned with form (2026-08-04, actual results)

- Updated `src/pages/login/index.vue` so username/password labels use the form title metrics: `32rpx`, `48rpx` line height, regular weight, and primary text color.
- Login TDesign inputs now use the same `16rpx 32rpx` vertical/horizontal padding and primary input text color as form control fields.
- Added a regression contract test; the focused suite passed 18 tests. `pnpm stylelint`, `pnpm lint`, `pnpm build`, and `pnpm verify:generated-runtime` all passed; generated login WXSS was checked for the final rules.
- Ran `cli.bat cache --clean compile --project ... --non-interactive` and `cli.bat open --project ... --trust-project --port 40637 --non-interactive`; both returned success. No runtime screenshot claim was made because Automator remains unavailable.

## Real-device hero explicit package inclusion (2026-08-04, superseded)

- The physical-device request was confirmed as `/wx491b28d7d178edbe/0/assets/land-planning-hero.webp` returning 404, while the same path worked in the simulator. This proves the path reached the runtime but the file was absent from that device package.
- Added `assets/land-planning-hero.webp` to `project.config.json` under `packOptions.include` as an explicit file entry; retained the static WXML path and `ignoreUploadUnusedFiles: false`.
- Focused Vitest passed 24 tests, `pnpm lint`, `pnpm build`, and `pnpm verify:generated-runtime` passed. The rebuilt `dist/assets/land-planning-hero.webp` is 20,054 bytes and its SHA-256 matches the public source asset.
- Closed the project, cleaned the DevTools compile cache, and reopened the exact worktree; all CLI commands succeeded. A fresh preview package was generated successfully (1,868,766 bytes) with QR output at `.tmp/hero-fix-preview.png`. Physical-device display still requires scanning this new preview and was not claimed as directly observed here.
- Follow-up physical-device evidence still returned 404 for the WebP URL. The explicit-include-only fix is superseded by the PNG conversion below because WebP is not in the mini-program upload suffix whitelist.

## Real-device hero PNG packaging fix (2026-08-04, actual results)

- The physical-device-only 404 was traced to the upload suffix whitelist: WebP can be read by the local DevTools filesystem but is not an uploadable mini-program package suffix, so `packOptions.include` cannot force it into the device package.
- Converted the 1000 x 562 hero to `public/assets/land-planning-hero.png`, updated the login WXML path and explicit package include entry, and removed the public WebP duplicate. The original source WebP remains available under `src/assets` as a recoverable source asset.
- Optimized the PNG to 112,005 bytes without changing dimensions. Focused Vitest passed 24 tests, `pnpm lint`, `pnpm build`, and `pnpm verify:generated-runtime` passed; generated WXML contains the PNG path once and no WebP path, while `dist/assets/land-planning-hero.png` exists and the WebP output does not.
- Closed the project, cleaned the DevTools compile cache, reopened the exact worktree, and generated a fresh preview successfully. Final preview size is 1,980,770 bytes and its QR is `.tmp/hero-png-final-preview.png`. Physical-device display requires scanning this new preview and was not claimed as directly observed here.

## Wizard and route scroll reset (2026-08-04, actual results)

- Added `src/platform/page-scroll.ts`, using the Wevu `wpi.pageScrollTo` adapter with `scrollTop: 0` and `duration: 0`; host-side scroll errors are contained so they do not break navigation.
- Land-demand step changes now wait for the Wevu render tick before returning the page to the top. The typed `navigate`, `replace`, and `replaceUrl` helpers also reset the next route's page scroll after navigation completes.
- `pnpm vitest run tests/unit/platform/page-scroll.test.ts tests/unit/components/land-demand-wizard.test.ts tests/unit/router/navigation.test.ts` passed: 24 tests. `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` all exited 0. Generated `dist/weapp-vendors/wevu-watch.js` contains the scroll adapter call with `scrollTop: 0` and `duration: 0`.
- After the build, the WeChat DevTools CLI `cache --clean compile --project ...` and `open --project ... --trust-project --port 40637 --non-interactive` both returned success for this worktree.

## Save success top message (2026-08-04, actual results)

- Replaced the bottom inline `已暂存` feedback with a TDesign `t-message` using the success theme. It is fixed at the top of the screen, displays for 2 seconds, and clears on timeout or close; validation and error feedback remain unchanged.
- The generated page registers `t-message` as `tdesign-miniprogram/message/message` and contains the `save-success-message` binding. The focused suite passed 25 tests; `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, and `pnpm build` passed.
- After the build, the WeChat DevTools CLI `cache --clean compile --project ...` and `open --project ... --trust-project --port 40637 --non-interactive` both returned success.

## Verification code dialog resend UX (2026-08-04, actual results)

- Removed the visible `六位验证码` label and the Mock test-code block. The TDesign input is now left-aligned and shows `请输入验证码` when empty.
- Added a right-side TDesign text button showing the 60-second resend countdown, then `重新发送验证码` when the cooldown ends. It emits a resend action that requests a fresh challenge and restarts from the repository-provided `retryAt` timestamp; the timer is cleaned up when the dialog is hidden or unmounted.
- Updated the runtime/E2E contracts to use `verification-resend` and the fixed mock input value without exposing `mockCode` in the UI. Focused tests passed 53 tests; full Vitest passed 164 tests. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` passed. DevTools cache clean/open both returned success.

## Verification dialog width and Message runtime fix (2026-08-04, actual results)

- Shortened the resend label to `秒后重新发送` / `重新发送`, switched the TDesign resend button to `extra-small`, removed its horizontal padding, and set the text size to `22rpx`; the verification input now flexes into the recovered width.
- The attached DevTools error was traced to TDesign Message's generated `message.interface.js` import not being registered by the WeChat module loader. The build compatibility plugin now inlines the four message themes in the generated Message entry points, so the page continues using TDesign without that runtime dependency failure.
- Focused tests passed 26 tests; full Vitest passed 164 tests. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` all exited 0. Generated Message entries contain no `message.interface` require.
- Closed the project, cleaned the DevTools compile cache, and reopened the exact worktree with `cli.bat open --project ... --trust-project --port 40637 --non-interactive`; all commands returned success.

## Save notification shadow and overflow fix (2026-08-04, actual results)

- The TDesign save notification was being rendered flush against the `PageShell`, whose `overflow: hidden` clipped its edge shadow. Added page-scoped spacing, width constraints, rounded corners, and an explicit shadow so the notification no longer touches the viewport edges.
- Focused wizard tests passed 21 tests; `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` exited 0. Generated `dist/pages/land-demand/index.wxss` contains the four-sided notification inset and shadow rule.
- Closed the project, cleaned the DevTools compile cache, and reopened the exact worktree on port 40637; all CLI commands returned success.

## Verification dialog cancel, reopen, and resend cooldown (2026-08-04, actual results)

- Cancel now only hides the TDesign verification dialog and keeps an unexpired challenge. Clicking `验证并提交` again reopens the existing dialog without calling `sendCode`, so the repository cooldown cannot produce `请稍后再试` or a failed resend mutation.
- The resend action reuses the challenge while `retryAt` is still in the future and only requests a new code after the cooldown ends; expired challenges or a changed phone number request a new challenge.
- The verification input keeps the full `请输入验证码` placeholder with a wider TDesign input area, and the dialog shows `N秒后可重新发送` while resend is unavailable.
- `pnpm vitest run tests/unit/features/land-demand-submit.test.ts` passed 8 tests; the focused review/wizard suite passed 26 tests; full `pnpm test` passed 166 tests. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` passed.
- Closed the project, cleaned the WeChat DevTools compile cache, and reopened the exact worktree on port 40637; all CLI commands returned success. Physical-device behavior was not directly observed here.

## Return to workbench with unsaved-draft protection (2026-08-04, actual results)

- Added a TDesign `t-button` in the filling page header to return to the enterprise service workbench. The button is hidden on the read-only detail page and disabled while a save is pending.
- Reused `LandDemandStore.isDirty` to detect edits that have not been explicitly saved. The return flow offers TDesign dialog actions for `继续编辑`, `不暂存，直接返回`, and `暂存并返回`; the last option runs the existing save mutation and navigates only after the Store is no longer dirty.
- Added a source contract for the return button and all dialog actions. Focused wizard/store tests passed 29 tests; full `pnpm test` passed 171 tests across 37 files. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` all passed. Generated WXML contains the new header button and leave-draft dialog.
- Closed the project, cleaned the WeChat DevTools compile cache, reopened the exact worktree on port 40637, and confirmed `islogin` returned `{"login":true}`. No live screenshot claim was made because the DevTools runtime bridge is not available in this session.

## Page-level loading states for async pages (2026-08-05, actual results)

- Added the shared `AppLoading` and `AppError` states to the workbench query branch. The workbench now shows a loading card while its private land-demand query is pending and a retryable error state if that query fails. The content is wrapped in a native default-slot view so the generated WXML retains the full workbench body.
- Rechecked the other async paths: the filling page already covers query/initialization loading and errors; the success page covers the submitted-record query; login, verification, save, submit, and return-to-workbench actions expose their TDesign loading states.
- `pnpm exec vitest run tests/unit/components/land-demand-wizard.test.ts tests/unit/features/product-navigation.test.ts` passed 26 tests; `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` passed. The final build produced a 769 KB main package.
- Reopened the exact worktree in WeChat DevTools on IDE port 40637 and automator port 11228. Captured `.tmp/runtime-home-final.png` and `.tmp/runtime-land-demand-final.png`; both pages rendered after the rebuild with no fatal runtime errors. The console only reported the existing `wx.getSystemInfoSync` deprecation warning.

## Current-step return dialog and incomplete progress status (2026-08-05, actual results)

- Return validation now uses only `validateStep(form.value, currentStep.value)`. An incomplete earlier step does not block returning from a later step; the current step still opens the TDesign dialog when a required field is empty.
- The dialog content is exactly `当前还有必填项未填写，是否确认返回？`. The return handler is now parameterless, so the Wevu tap event cannot be interpreted as an internal flag that bypasses validation. The confirmation button calls the save-and-return operation separately.
- The progress rail exposes `incompleteSteps`; in the real DevTools runtime at step 5 with the step-3 investment field empty, runtime state reported `incompleteSteps: [3]` and the step-3 indicator/label rendered red.
- Replayed the screenshot case in the rebuilt DevTools project (step 3, investment empty): clicked `data-testid="land-demand-back-home"`; the active route remained `pages/land-demand/index?step=3`, `requiredReturnDialogVisible` became `true`, and the rendered TDesign dialog text was `确认返回工作台当前还有必填项未填写，是否确认返回？继续填写确认返回`. Captured `.tmp/required-return-dialog-final-after-rebuild.png`.
- The project was reopened with AppID `wx491b28d7d178edbe`, IDE control port `40637`, and automator port `11228`; this is a DevTools simulator runtime check, not a physical-device check.
- Full `pnpm test` passed 171 tests across 37 files; `pnpm lint`, `pnpm typecheck`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` passed.

## Return prompt checks explicit save state (2026-08-04, actual results)

- Added `hasLocalDraft` to the LandDemand Store so local recovery snapshots are not mistaken for an explicit `暂存` operation. A new form, local draft, or dirty form now opens the return confirmation dialog.
- `暂存并返回` now navigates only when the existing server save mutation succeeds. `不暂存，直接返回` removes the local draft before returning, while a successfully persisted record returns directly without prompting.
- Focused wizard/store tests passed 29 tests; full `pnpm test` passed 171 tests across 37 files. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` all passed.
- Closed the project, cleaned the WeChat DevTools compile cache, reopened the exact worktree on port 40637, and confirmed `islogin` returned `{"login":true}`. No live screenshot claim was made because the DevTools runtime bridge is not available in this session.

## Restore wizard action-bar appearance (2026-08-04, actual results)

- Reverted the unintended wrapper/`block` changes to `src/features/land-demand/components/wizard-actions.vue`; the original TDesign button markup, variants, sizing, and fixed-bar layout are restored.
- Kept only the requested behavior fix in the page: changing steps clears a stale `暂存成功` notification, while step navigation remains local-draft persistence rather than an explicit server save.
- Focused tests passed 29 tests; full `pnpm test` passed 171 tests across 37 files. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` all passed.
- Closed the project, cleaned the WeChat DevTools compile cache, reopened the exact worktree on port 40637, and confirmed `islogin` returned `{"login":true}`. No live screenshot claim was made because the DevTools runtime bridge is not available in this session.

## Wizard previous-step button hit area and stale notice fix (2026-08-04, actual results)

- Wrapped each fixed action-bar TDesign button in its own equal-width flex item and made each button `block`, so `上一步`, `暂存`, and `下一步` have separate, non-overlapping tap regions.
- Step changes now clear any earlier `暂存成功` message before moving, so local draft persistence during navigation is not presented as an explicit save success.
- Focused wizard/store/step-controller tests passed 32 tests; full `pnpm test` passed 171 tests across 37 files. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` all passed. Generated WXML contains the previous button inside its dedicated action item.
- Closed the project, cleaned the WeChat DevTools compile cache, reopened the exact worktree on port 40637, and confirmed `islogin` returned `{"login":true}`. No live screenshot claim was made because the DevTools runtime bridge is not available in this session.

## TDesign nullable property warnings (2026-08-04, actual results)

- The pasted runtime log contained nullable TDesign properties: dialog `content`, cell `title`/`note`, picker `title`/`value`, picker-item `options`, and the custom SinglePicker `value` prop.
- SinglePicker now has optional runtime props with concrete defaults, normalizes option/value data, passes empty string/array fallbacks, and only mounts the hidden picker after it is opened. All parent SinglePicker bindings normalize nullable form values before crossing the component boundary.
- VerificationDialog now avoids mounting an empty dialog and passes an empty string when its description is not ready. The generated WXML contains the fallback expressions and conditional picker/dialog mounting.
- Focused wizard/review tests passed 26 tests; full `pnpm test` passed 170 tests across 37 files. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` all exited 0.
- Closed the project, cleaned the DevTools compile cache, and reopened the exact worktree on port 40637; `islogin` returned `{"login":true}`. The two `__dev__/WAAutoService.js` and `__dev__/WAServiceMainContext.js` preload messages are DevTools-injected resource timing warnings, not project component warnings.
- The `wv ide logs` bridge remained waiting and timed out after 19 seconds, so no live-console-cleared claim is made from the log bridge; the code/build and DevTools CLI results above are independently verified.

## Login underline and land-demand bottom spacing (2026-08-04, actual results)

- Removed the outer `.login__field` border so each login TDesign input keeps only its own underline; the generated login WXSS no longer contains a second field border.
- Reduced the edit wizard's bottom reservation from `220rpx` to the fixed action bar's calculated height, including the safe-area inset. The read-only detail route now applies `padding-bottom: 0`, leaving only the compact PageShell bottom padding instead of reserving an unused action bar.
- Focused wizard/review tests passed 26 tests; full `pnpm test` passed 170 tests across 37 files. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` all exited 0.
- Closed the project, cleaned the DevTools compile cache, and reopened the exact worktree on port 40637; `islogin` returned `{"login":true}`. A live screenshot was not claimed because the DevTools runtime bridge is not available in this session.

## Verification code placeholder width fix (2026-08-04, actual results)

- Shortened the active resend indicator to only show the remaining seconds, such as `58秒`, so it no longer consumes the verification input's width.
- Added an explicit TDesign input class with `width: 100%` and changed the flex basis to `0` with `min-width: 0`; generated WXML retains the full `请输入验证码` placeholder.
- Focused tests passed 26 tests; `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` exited 0. Closed the project, cleaned the DevTools compile cache, and reopened the exact worktree on port 40637 successfully.

## Verification dialog single-line input layout (2026-08-04, actual results)

- Removed the duplicate `N秒后可重新发送` line below the input. The right side now keeps only the compact TDesign countdown or resend action.
- The input wrapper and native TDesign input control now explicitly occupy the remaining row width; the resend area has a fixed right-aligned width, and the dialog content has reduced horizontal padding so the input no longer collapses on the left or leaves an unexplained gap on the right.
- Focused review/submit tests passed 13 tests; full `pnpm test` passed 166 tests across 36 files; `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` passed.
- Closed the project, cleaned the WeChat DevTools compile cache, and reopened the exact worktree on port 40637; all CLI commands returned success. Physical-device behavior was not directly observed here.
- `wv screenshot --page pages/land-demand/index --json` timed out after 30 seconds, so no runtime screenshot claim is made.

## Verification dialog description spacing (2026-08-04, actual results)

- Changed the dialog description from `验证码已发送至 ${phone}` to `已发送至 ${phone}`, removing the redundant three-character label.
- Added a `16rpx` top margin before the verification input row so the third line is visually separated from the description.
- The focused review contract passed 5 tests; `pnpm typecheck`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` passed. DevTools close, cache clean, and reopen on port 40637 all returned success.

## Verification code alignment and submission time format (2026-08-04, actual results)

- Restored matching horizontal padding between the TDesign input value and its underline so entered digits align with the line.
- Added `src/platform/date-time.ts` to format ISO timestamps as `YYYY-MM-DD HH:mm:ss`; the success page now uses it for `提交时间`, removing the `T`, milliseconds, and `Z` suffix.
- Full `pnpm test` passed 168 tests across 37 files; `pnpm lint`, `pnpm typecheck`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` passed. DevTools close, cache clean, and reopen on port 40637 all returned success.

## Verification resend after a successful submission (2026-08-04, actual results)

- Successful verification now removes the consumed challenge from storage instead of leaving its old `retryAt` cooldown behind. After returning to edit a submitted record, the next verification request can send immediately.
- Unsuccessful challenges still retain the 60-second resend limit and invalid-attempt protection.
- Repository and submit tests passed 17 tests; full `pnpm test` passed 168 tests across 37 files; `pnpm lint`, `pnpm stylelint`, `pnpm typecheck`, `pnpm build`, and `pnpm verify:generated-runtime` passed. DevTools close, cache clean, and reopen on port 40637 all returned success.

## Verification mutation failure recovery (2026-08-04, actual results)

- Active, non-invalidated challenges inside the 60-second resend interval are now returned from storage instead of throwing `please wait`; this lets a page reopened after cancel or reload reuse the existing challenge and reopen the dialog.
- Legacy challenges without the new `createdAt` marker, including old successful or exhausted records, are treated as stale and replaced immediately. New challenges invalidated after five incorrect attempts remain locked until their retry interval ends.
- Focused repository/submit tests passed 19 tests; full `pnpm test` passed 170 tests across 37 files. `pnpm lint`, `pnpm stylelint`, `pnpm typecheck`, `pnpm build`, and `pnpm verify:generated-runtime` all exited 0.
- Closed the project, cleaned the WeChat DevTools compile cache, and reopened the exact worktree on port 40637; all CLI commands returned success. Physical-device behavior was not directly observed here.

## Direct save-and-return notification (2026-08-04, actual results)

- Removed the return confirmation dialog. `返回工作台` now runs the existing server-side temporary-save mutation directly and only navigates after the save succeeds; save validation or mutation errors keep the user on the filling page.
- Successful return navigation passes `notice=saved` to the workbench. The workbench reads that route notice and renders a top-positioned TDesign success message with `暂存成功`, including an inset shadow so it is not clipped by the page edge.
- The focused wizard contract passed 22 tests; full `pnpm test` passed 171 tests across 37 files. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` all passed. The build produced a 757 KB main package.
- Closed the project, cleaned the WeChat DevTools compile cache, reopened the exact worktree on port 40637, and confirmed `islogin` returned `{\"login\":true}`. No live screenshot claim was made because the DevTools runtime bridge is not available in this session.

## Submitted-edit progress and verification-submit loading (2026-08-05, actual results)

- The TDesign verification dialog now passes an object to `confirm-btn`; its `loading` and `disabled` values follow the page's pending verification/save mutations, so the `确认提交` button shows a loading state while submission is running.
- When a submitted record is opened for editing, Store initialization treats the record as having reached step 5. Returning to any earlier step therefore keeps steps 2–5 active blue; the current valid step can still render green and an incomplete current step can render red. Step 5 is never marked incomplete.
- Focused land-demand review/store/wizard/navigation tests passed 39 tests across 4 files; the full suite passed 172 tests across 37 files with Vitest limited to one thread after the default fork pool emitted an unrelated worker-exit error. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` all passed. The final build reported a 769 KB main package, and generated `dist/features/land-demand/components/verification-dialog.wxml` binds `confirm-btn` to the loading-aware object.
- Runtime recheck was blocked after the build because IDE port 40637 was listening but project automator port 11228 was not. Stale automator wrappers for this project were closed; one clean MCP reconnect still timed out after 45 seconds. No new runtime screenshot or DevTools interaction is claimed for this change.

## Required-field confirmation before returning to the workbench (2026-08-05, actual results)

- The `返回工作台` action now runs full submission validation before starting the save/loading flow. When required fields are missing, it opens a TDesign confirmation dialog; `继续填写` closes the dialog and stays on the current page, while `确认返回` proceeds with the existing temporary-save-and-return flow. Invalid formats and mutation failures continue to block navigation and show the existing error message.
- The generated `dist/pages/land-demand/index.wxml` contains `required-return-dialog`, the confirmation text, and the `继续填写` / `确认返回` buttons.
- The focused wizard contract passed 22 tests; full `pnpm test` passed 171 tests across 37 files. `pnpm lint`, `pnpm typecheck`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` all passed. The build produced a 760 KB main package.
- Closed the project, cleaned the WeChat DevTools compile cache, reopened the exact worktree on port 40637, and confirmed `islogin` returned `{\"login\":true}`. No live screenshot claim was made because the DevTools runtime bridge is not available in this session.

## Return loading animation before success notification (2026-08-04, actual results)

- Added a TDesign button loading state for `返回工作台`. The state starts before the temporary-save mutation, disables repeat taps, and remains active through the route replacement.
- The return path suppresses the filling-page success message and passes `notice=saved` only after the save succeeds. The workbench then displays `暂存成功` after navigation; save or navigation failures clear the loading state and keep the user informed on the current page.
- `pnpm lint`, `pnpm typecheck`, `pnpm stylelint`, full `pnpm test` (171 tests across 37 files), `pnpm build`, and `pnpm verify:generated-runtime` all passed. The build produced a 757 KB main package.
- Closed the project, cleaned the WeChat DevTools compile cache, reopened the exact worktree on port 40637, and confirmed `islogin` returned `{\"login\":true}`. No live screenshot claim was made because the DevTools runtime bridge is not available in this session.

## Ensure visible return loading animation (2026-08-04, actual results)

- The Mock repository resolves immediately, so a button-only loading state could be replaced before the WeChat renderer painted it. Added a TDesign fullscreen `t-loading` overlay, yielded one render tick before saving, and enforced a 500 ms minimum loading display.
- The success notice remains suppressed during the return operation and is still emitted only through the workbench route notice after navigation.
- Full `pnpm test` passed 171 tests across 37 files; `pnpm lint`, `pnpm typecheck`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` passed. Generated `dist/pages/land-demand/index.json` registers `t-loading` and generated WXML contains the conditional fullscreen loader.
- Closed the project, cleaned the WeChat DevTools compile cache, reopened the exact worktree on port 40637, and confirmed `islogin` returned `{\"login\":true}`. No live screenshot claim was made because the DevTools runtime bridge is not available in this session.

## Remove artificial loading delay, fix message inset, and restore draft progress (2026-08-04, actual results)

- Removed the artificial 500 ms minimum loading duration. The return flow keeps only one render tick before the fast Mock save so the loading overlay can paint without adding a fixed wait.
- Replaced ineffective page-level Message width overrides with TDesign Message's `offset: [16, 16]` on both filling and workbench notifications, giving the success message visible left/right margins and shadow.
- Temporary saves now retain local step metadata while final submitted records still remove it. Returning from step 4 therefore restores step 4 and the workbench progress rail instead of resetting to step 1.
- Full `pnpm test` passed 171 tests across 37 files; `pnpm lint`, `pnpm typecheck`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` passed. Closed the project, cleaned the WeChat DevTools compile cache, reopened the exact worktree on port 40637, and confirmed `islogin` returned `{\"login\":true}`. No live screenshot claim was made because the DevTools runtime bridge is not available in this session.

## Preserve maximum progress and block invalid return (2026-08-05, actual results)

- Returning from step 3 after previously reaching step 5 now preserves `progressStep=5` in the local draft metadata, so the workbench rail and label remain at step 5 while the editable page can stay on step 3.
- The return action validates the full form before navigation. If a value cannot be temporarily saved, it stays on the filling page, jumps to the first invalid step, keeps field errors visible, and shows a TDesign error message: `当前填报内容存在问题，请先修正后再返回工作台`.
- Full `pnpm test` passed 171 tests across 37 files; `pnpm lint`, `pnpm typecheck`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` passed. Closed the project, cleaned the WeChat DevTools compile cache, reopened the exact worktree on port 40637, and confirmed `islogin` returned `{\"login\":true}`. No live screenshot claim was made because the DevTools runtime bridge is not available in this session.

## Wizard previous-step tap fix (2026-08-05, actual results)

- Changed the fixed action bar to use explicit `handlePrevious`, `handleSave`, and `handleNext` event handlers instead of inline emits. All three TDesign buttons now use `block` and `min-width: 0`, giving each button an independent full hit area while preserving the existing order and labels.

## Preserve blue future steps and color the current step (2026-08-05, actual results)

- Both the filling-page wizard rail and workbench card now distinguish the current step from the maximum reached step. Reaching step 5 and returning to step 3 keeps steps 4–5 blue instead of gray.
- A valid current step renders green; a current step with a missing required value renders red. The remaining reached steps stay blue in both progress rails.
- Fixed a Wevu runtime edge case where a transient `null` incomplete-step prop caused the whole dynamic class object to fall back to the gray base style. Progress classes now use null-safe expressions and preserve the current-step fallback.
- WeChat DevTools runtime verification on IDE port 40637 / automator port 11228, iPhone 12/13 Pro simulator, SDK 3.17.1: captured `.tmp/progress-current-step3.png` (current step green, later steps blue), `.tmp/progress-current-step3-incomplete.png` (current step red, later steps blue), `.tmp/home-progress-final-restored.png` (workbench green state), and `.tmp/home-progress-incomplete-final.png` (workbench red state). Test data was restored to the original investment value `43` after the red-state check.
- Full `pnpm test` passed 171 tests across 37 files; `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` passed. Final build main package: 763 KB.
- Focused wizard and step-controller tests passed 25 tests; full `pnpm test` passed 171 tests across 37 files. `pnpm lint`, `pnpm typecheck`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` passed.
- Closed the project, cleaned the WeChat DevTools compile cache, reopened the exact worktree on port 40637, and confirmed `islogin` returned `{\"login\":true}`. No live screenshot claim was made because the DevTools runtime bridge is not available in this session.

## Restore verification dialog confirm button (2026-08-05, actual results)

- The dynamic `confirm-btn` object was not recognized by the Wevu-generated TDesign Dialog at runtime, leaving only the cancel button. Replaced it with the Dialog's `confirm-btn` slot containing a TDesign Button, preserving the `确认提交` label and binding its loading/disabled states to the submission mutation.
- Focused review/store tests passed 13 tests; `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` passed. Generated WXML contains `data-testid="verification-confirm"` and `loading="{{props.loading}}"`.
- IDE port 40637 is listening, but project automator port 11228 is not; no new runtime screenshot claim was made.

## Restore native verification dialog actions and guard incomplete codes (2026-08-05, actual results)

- Reverted the custom `confirm-btn` slot and restored TDesign Dialog's native `confirm-btn="确认提交"`, `cancel-btn="取消"`, and `button-layout="horizontal"`; generated WXML now contains the native footer actions and no custom confirmation button slot.
- Added a six-digit guard in both the page handler and submit controller. Typing one character or tapping confirm early now only shows `请输入6位验证码` and never calls the verification mutation.
- Added a dedicated fullscreen TDesign loading state for verification submission and route transition, without reusing the draft-save loading state.
- Focused review/wizard/submit tests passed 36 tests; `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` passed. Build main package: 770 KB.
- IDE port 40637 is listening, but project automator port 11228 is not; no new DevTools runtime screenshot or interaction is claimed.

## Contextual loading for page transitions and submissions (2026-08-05, actual results)

- Added the shared `usePageTransitionLoading` guard and `PageTransitionLoading` TDesign fullscreen loader. User-triggered transitions now cover login, workbench entry/view/edit/logout, wizard step changes, detail navigation, success-page actions, invalid-record redirects, and the error-page return action.
- Loading text is contextual: `正在登录`, `正在加载`, `正在暂存`, `正在发送验证码`, `正在提交`, `正在返回工作台`, and `正在返回首页`. The success page shows `正在提交` only while its submission result is still loading; `已提交` remains a completed status after the record is loaded.
- Draft save, verification-code sending, final verification submission, and route transitions disable duplicate actions. The fixed wizard action bar also receives the transition state so step buttons cannot be tapped repeatedly.
- Focused tests passed 33 tests; full Vitest passed 175 tests across 38 files with one thread. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, `pnpm analyze:budget`, and `git diff --check` passed. Build main package: 777 KB.
- IDE port 40637 is listening, but project automator port 11228 is not; no new DevTools runtime screenshot or interaction is claimed.

## Restore clickable verification submit button (2026-08-05, actual results)

- The native TDesign Dialog `@confirm` event was present in generated WXML but did not produce a response in the current Wevu runtime. Restored the previously runtime-verified TDesign `cancel-btn` and `confirm-btn` slots, each containing a real TDesign `t-button` with explicit `@tap` handlers.
- Changed the confirmation label to `提交`. The submit button is disabled until six digits are entered, and the page/controller six-digit guard remains in place so partial input cannot trigger a verification mutation.
- Focused review/wizard/submit tests passed 36 tests; `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` passed. Generated WXML contains `data-testid="verification-submit"`, `@tap` dispatch, and the `提交` label.
- A new DevTools automator connection was attempted on project port 11228 after the rebuild and timed out after 30 seconds; IDE port 40637 remains listening. No live click or screenshot is claimed.

## Preserve submitted state during unchanged edits and show last submission time (2026-08-05, actual results)

- Editing a submitted record now keeps a form signature for the original submitted snapshot. Local step saves no longer erase the comparison state, so returning without a real field change does not downgrade the record to `草稿待完善`.
- A changed submitted record is still saved as a draft status, while a verified final submission restores status `已提交`.
- Added `lastSubmittedAt` to the Mock record. Draft updates preserve it, final submissions refresh it, and the workbench card displays `上次提交：YYYY-MM-DD HH:mm:ss`. Existing submitted records fall back to their historical `updatetime` when the new field is absent.
- Focused tests passed 47 tests across 4 files; full Vitest passed 177 tests across 38 files. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, `pnpm analyze:budget`, and `git diff --check` passed. The build reported a 780 KB main package.
- IDE port 40637 is listening, but project automator port 11228 is not; no live DevTools screenshot or interaction is claimed for this change.

## Fix home workbench dynamic data rendering (2026-08-05, actual results)

- Moved the home page shell markup into `src/pages/home/index.vue` so the enterprise card, status, progress, submission time, and action labels are owned by the page's main WXML instead of a generated scoped-slot sidecar.
- `dist/pages/home/index.wxml` now contains the home dynamic bindings directly, and `dist/pages/home` contains no generated scoped-slot sidecar files.
- `pnpm test` passed 177 tests across 38 files; `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm verify:generated-runtime`, and `git diff --check` passed. The preceding build completed successfully with a 781 KB main package.
- Real WeChat DevTools window observation: `.tmp/devtools-after-focus-click.png` displayed `宁波示范智造有限公司`, `91330200MA2DEMO001`, `已提交`, completed progress, last submission time, and both action buttons after the rebuild.
- MCP automator connection was retried and timed out. IDE port 40637 is listening, but project automator port 11228 is not, so MCP page-state/screenshot calls remain unavailable; the screenshot evidence above was captured from the actual DevTools window rather than claimed as MCP output.

## Login form validation and loading cleanup (2026-08-05, actual results)

- Changed the username placeholder and empty-field validation message to `请输入统一社会信用代码`.
- Removed the demo account/password hint card below the form.
- Bound TDesign input error status and tips to the existing field validation refs, so tapping login with empty fields now shows feedback in both inputs.
- Removed the button-level loading prop from login. The page-level TDesign transition loading remains the single `正在登录` indicator, while the button stays disabled during the request.
- Focused tests passed 8 tests; full Vitest passed 177 tests across 38 files. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, and `pnpm verify:generated-runtime` passed. Build main package: 781 KB.

## Remove duplicate button loading indicators (2026-08-05, actual results)

- Removed `loading` bindings from all TDesign buttons in the login, workbench, filling, detail, success, review, verification-dialog, and wizard-action views.
- Kept the existing disabled guards so repeated taps remain blocked while the corresponding page-level transition, save, verification, or submit loader is visible.
- Full Vitest passed 177 tests across 38 files; `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` passed. Build main package: 780 KB.

## Scroll to the first invalid field on Next (2026-08-05, actual results)

- The Next handler now validates the current step, renders its field errors, and scrolls the first invalid field into view. The scroll query uses the existing `data-testid` hooks, reads the field and viewport positions through the Mini Program selector query, and keeps the target 32px below the top header.
- Validation failures remain on the current step; only a valid step transitions to the next step. The selector query and page-scroll adapter fail safely when the host runtime is not ready.
- Added unit coverage for selector-query scroll calculation and the page-level first-error mapping. Full Vitest passed 179 tests across 38 files; `pnpm lint`, `pnpm typecheck`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` passed. Build main package: 784 KB.
- WeChat DevTools automator is not available in this session, so no live-device click or screenshot is claimed for this change.

## Fix nested-step selector scope for invalid-field scrolling (2026-08-05, actual results)

- The first implementation queried child-field `data-testid` values from the page context. Generated WXML confirms those controls live inside the `BasicInfoStep`, `LandInfoStep`, `ProjectInfoStep`, and `FinanceContactStep` component WXML, so the page-level selector could return no node.
- Each step component now creates its own Wevu `useSelectorQuery()` factory and watches the current step errors. It locates that component's first invalid field after the error state renders, then uses the shared page-scroll adapter to move the viewport.
- Full Vitest passed 179 tests across 38 files; `pnpm lint`, `pnpm typecheck`, `pnpm stylelint`, `pnpm build`, `pnpm verify:generated-runtime`, and `git diff --check` passed. Build main package: 784 KB.
- WeChat DevTools automator was retried after rebuilding but timed out after 10 seconds; no live-device click or screenshot is claimed.

## Use native field wrappers as scroll anchors (2026-08-05, actual results)

- The screenshot showed the error feedback appearing while the viewport stayed at the bottom of step 3. The scroll target was changed from TDesign component hosts to native outer `view` wrappers such as `area-field`, `investment-field`, and `phone-field`, which are directly queryable inside each step component.
- Rebuilt WXML contains the native wrapper anchors in every step component. Full build completed with a 785 KB main package; focused wizard tests passed 23 tests after adding anchor coverage.
- The DevTools automator connection remains unavailable, so live-device scroll verification still requires a manual DevTools Compile/Refresh and click test.

## Use page-level cross-component scroll selectors (2026-08-06, actual results)

- Replaced the child-component `createSelectorQuery().in(component)` plus `selectViewport()` calculation with the WeChat page-level `pageScrollTo({ selector, offsetTop, duration })` API. This avoids mixing a component-scoped field query with the page viewport scroll position.
- Added native `id` anchors to all 29 required-field wrappers and targets them with selectors such as `#project-info-step >>> #investment-field`. The invalid-field watcher and step-switch top-scroll path now wait for a native render turn before scrolling.
- Focused scroll/wizard tests passed 26 tests; full `pnpm test` passed 179 tests across 38 files. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm verify:generated-runtime`, `pnpm build`, and `git diff --check` passed. The rebuilt main package is 785 KB and generated WXML contains both the step component IDs and field anchor IDs.
- A live MCP reconnect timed out after 10 seconds because the project automator port is unavailable; the two stale `cli.bat auto --auto-port 11228` wrappers created by that attempt were stopped. No live-device scroll result is claimed.

## Remove duplicate step-validation feedback banner (2026-08-06, actual results)

- Removed the bottom green `请先完成第 X 步的必填项` feedback assignment from Next-step validation and submission-target redirects. The field-level required errors and first-invalid-field scrolling remain unchanged.
- Kept the shared feedback area for actual send-code failures, verification progress, and return-to-workbench failures; only the duplicate validation summary is suppressed.
- Focused wizard tests passed 24 tests; full Vitest passed 180 tests across 38 files. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm verify:generated-runtime`, `pnpm build`, and `git diff --check` passed. The rebuilt main package is 785 KB.
- WeChat DevTools automator remains unavailable in this session, so no new live-device screenshot or interaction is claimed.

## Remove doubled field separators (2026-08-06, actual results)

- Removed the generic `.field` bottom border. TDesign `t-input`, `t-cell`, and `t-checkbox` controls already render their own separators, so the outer border was drawing a second line at the same boundary.
- Added a contract test to prevent the outer field separator from being reintroduced.
- Focused wizard tests passed 25 tests; full Vitest passed 181 tests across 38 files. `pnpm typecheck`, `pnpm lint`, `pnpm stylelint`, `pnpm verify:generated-runtime`, `pnpm build`, and `git diff --check` passed. The rebuilt main package is 785 KB.
- No live WeChat DevTools screenshot or interaction is claimed for this change.

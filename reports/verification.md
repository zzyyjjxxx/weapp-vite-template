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

## DevTools E2E 现场复核（2026-07-30）

- `pnpm exec wv ide info`：34 秒内无输出并超时；仅停止了该命令遗留的
  2 个 Node 子进程。
- `pnpm build`：退出 0；微信小程序构建成功，主包 715 KB。
- 首次 `pnpm test:e2e`：退出 1；9 个串行场景中第 1 个在 fixture 建立前
  失败，Automator 无法连接 `ws://127.0.0.1:10541`，其余 8 个未运行。
- 官方微信 CLI `islogin`：退出 0，返回 `{"login":true}`；IDE 服务端口
  `40637` 成功启动，排除登录失效。
- 官方微信 CLI `auto --project <workspace> --auto-port 10541
  --trust-project`：退出 0，确认真实 AppID `wx75dcf8b64b21bcdf` 并报告
  `auto` 成功；10541 随后由目标项目 DevTools 进程监听。
- 第二次 `pnpm test:e2e`：退出 1；已连接 Automator，但第 1 个场景在首个
  运行时协议调用处 15 秒超时，随后 fixture teardown 达到 120 秒上限；
  其余 8 个串行场景未运行。没有进入登录、填表或提交业务断言。
- 通过官方 `close --project <workspace>` 只关闭本次目标项目窗口后重新
  `auto`，等待 15 秒再运行最小 `current-page` 探测；10541 正常监听，
  但协议仍不可用，CLI 报告 websocket 无法连接并因端口已被目标 DevTools
  占用而无法启动替代会话。
- `pnpm typecheck:e2e`：退出 0。
- `pnpm test tests/unit/e2e tests/smoke/runtime-e2e-contract.test.ts`：
  退出 0，2 个文件/11 个测试通过，Driver 映射和运行时 E2E 静态合约有效。
- 本轮没有生成 `.tmp/e2e-login.png` 或 `.tmp/e2e-review.png`，因此没有
  执行 `wv compare`，也没有创建或更新视觉基线。

结论：构建、E2E 类型检查、Driver 与静态合约通过；真实 9 场景 DevTools
E2E 仍受当前 DevTools Automator 的“端口监听但首个协议调用超时”阻塞，
不能标记为通过。该结论已通过登录检查、真实 AppID、目标项目关闭重开、
端口监听和最小协议探测交叉验证，不是业务断言失败。

## DevTools 组件警告清理与 E2E 完成（2026-07-30）

- 使用微信开发者工具 SDK 3.17.0、真实 Automator 端口 10541 串行执行
  `pnpm test:e2e`，退出 0：9 个场景全部通过，耗时 43.2 秒。
- E2E fixture 在整套运行期间收集控制台消息，并把
  `[Component] property` 作为套件级失败条件；本次通过结果中该类警告为
  0。开发者工具自身的 preload、worker 不支持提示不属于组件属性警告，
  未伪报为全控制台零消息。
- 修复了 TDesign `radio-group.options`、`checkbox-group.options`、
  `input.status/tips`、`cell.note`、`cascader.options` 以及级联组件内部
  `search.placeholder` 收到非声明类型值的问题。行业级联选择器只在选项
  数组已就绪且弹层可见时挂载。
- 9 个真实运行时场景覆盖：登录、草稿保存恢复、层高保留、园区选择、
  国民经济行业恢复并打开级联选择器、产业方向切换清空、融资条件校验、
  验证码提交、成功页查看、已提交记录编辑再暂存，以及最后执行的冷启动
  会话恢复。
- `.tmp/e2e-login.png`（18,630 字节）与
  `.tmp/e2e-review.png`（63,625 字节）均由本次 E2E 生成并人工检查：
  登录页和确认提交页显示正常、可读，无异常遮挡。
- 最终 `pnpm verify` 退出 0：`prepare`、应用/E2E 类型检查、零警告
  `lint`、`stylelint`、35 个测试文件/158 个测试、微信小程序构建、生成
  产物契约和包体预算全部通过；主包 690 KB。
- `git diff --check` 退出 0，仅输出 Windows 工作区未来 LF→CRLF 转换
  提示，没有空白错误。

结论：此前 Automator 连接阻塞已解除，用户报告的连续组件属性警告已全部
清理；真实微信开发者工具 9 场景 E2E 和仓库完整质量门禁均通过。

## 验证码重复 Mutation 修复（2026-07-31）

- 现场错误定位到验证码发送 Mutation：有效验证码已写入 Storage 后，确认页
  的重复触发被误判为 60 秒内重新发送，记录
  `[mutation.failed] / 请稍后再试`。
- 首次加上 `[mutation.failed]` 套件级门禁后，真实 `pnpm test:e2e` 的 9 个
  业务断言虽通过，但 teardown 捕获 `mutationId: 5` 并令命令退出 1，证明
  原问题可稳定复现。
- 修复后，未消费且仍有效的验证码会恢复原挑战，不生成新验证码；页面关闭
  弹窗后保留挑战并可直接重开。发送验证码、验证码校验与最终持久化均增加
  同一操作的并发去重；验证码成功消费或错误次数耗尽后仍保留 60 秒重发限制。
- E2E 提交场景连续执行两次“验证并提交”，两次均显示同一个 Mock 验证码，
  随后完成验证码校验、正式提交、成功页和记录回看。最终
  `pnpm test:e2e` 退出 0，9/9 通过，耗时 45.5 秒；整套运行中
  `[mutation.failed]` 与 `[Component] property` 均为 0。
- 聚焦 Repository、提交控制器、确认页和 E2E 契约测试为 4 个文件/25 个
  测试通过。
- 最终 `pnpm verify` 退出 0：类型检查、零警告 lint、stylelint、35 个测试
  文件/161 个测试、微信小程序构建、生成产物契约和包体预算全部通过；主包
  692 KB。

结论：确认提交的验证码请求现在具备幂等恢复与并发去重，现场
`mutation.failed: 请稍后再试` 已在真实微信开发者工具运行时消除。

## 法人手机号验证弹窗可见性修复（2026-07-31）

- 用户截图显示 TDesign `t-dialog` 只渲染“法人手机号验证”标题，默认 slot
  内的手机号、输入框、Mock 验证码和操作按钮没有出现在真实视口；组件树
  方法仍可被 Automator 调用，因此此前业务 E2E 未能发现视觉缺失。
- 将验证码交互内容改为原生小程序遮罩与卡片结构，保留 TDesign Input 和
  Button。生成模板不再依赖原生组件默认 slot 的 Vue 嵌套透传。
- E2E 增加 `.tmp/e2e-verification.png` 截图（56,927 字节）。已人工检查：
  标题、手机号 `13800000000`、六位验证码输入框、Mock 验证码 `123456`、
  取消和确认提交按钮均完整显示；空验证码时确认按钮正确禁用。
- 真实 `pnpm test:e2e` 退出 0：共享单一 Automator 的 9 个串行场景全部
  通过，耗时 46.2 秒；随后实际输入验证码并完成提交、成功页和记录回看。
- 最终 `pnpm verify` 退出 0：类型检查、零警告 lint、stylelint、35 个测试
  文件/161 个测试、微信小程序构建、生成产物契约和包体预算全部通过；主包
  692 KB。

结论：验证码弹窗的视觉内容与真实交互均已在微信开发者工具 SDK 3.17.0
中验证通过，不再存在“只有标题、无法输入或确认”的空弹窗。

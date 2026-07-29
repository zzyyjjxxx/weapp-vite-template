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

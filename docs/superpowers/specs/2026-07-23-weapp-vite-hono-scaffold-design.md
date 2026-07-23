# weapp-vite + Wevu + Hono 完整脚手架设计

> 状态：已获用户确认的设计
> 日期：2026-07-23
> 目标目录：`/Users/mang/Project/weapp-vite-template`

## 目标

在当前目录直接搭建一个可运行、可测试、可由 Codex 维护的微信小程序脚手架。前端遵循现有方案文档中的 `weapp-vite + Wevu Vue SFC + TDesign MiniProgram + @tanstack/query-core` 路线，并新增同仓的 Hono 轻量测试后端，形成可验证的登录和订单垂直切片。

脚手架完成后应能够：

- 启动一个本地 Hono API，提供健康检查、登录、刷新 Token、用户资料和订单接口；
- 启动微信小程序前端，并通过项目 HTTP Client 请求 Hono；
- 展示登录、订单列表、订单详情和取消订单流程；
- 用 Query Core 管理服务端状态，用 Wevu Store 管理会话和客户端状态；
- 用 Vitest 验证 HTTP、Query、路由、Store 和 Hono 路由；
- 用 `pnpm prepare`、类型检查、Lint、测试、构建和包体分析完成静态验收；
- 在微信开发者工具或 weapp-vite MCP 可用时完成运行时路由、交互、Console 和截图验收。

## 范围

### 包含

- 当前目录作为项目根目录；
- 根目录 `src/` 作为小程序前端源码；
- `server/` 作为 Hono Node.js 测试后端；
- 主包首页、我的、登录、错误页；
- 订单分包的列表和详情页；
- Auth Store、Preferences Store、Router、HTTP Client、Token 单飞刷新和 Query Core Adapter；
- TDesign Resolver、SCSS Design Tokens、基础 Loading/Empty/Error/Page Shell 组件；
- 根 `AGENTS.md`、HTTP 和 Query 局部规则、项目 Agent Skills、Codex MCP 配置、CI 配置和团队文档；
- Hono 内存 Fixture、统一响应 Envelope、Bearer 鉴权和取消订单 Mutation；
- 前端和后端的可重复测试。

### 不包含

- 真实生产后端、数据库、微信 AppSecret、支付或生产发布；
- 真实业务账号和不可逆写操作；
- H5 作为首要运行目标；
- Vue Mini、标准 Pinia、Pinia Colada、Vue Router、Axios 或原生 Web TailwindCSS 工作流；允许按小程序编译链需要使用 `weapp-tailwindcss`；
- 微前端、动态模块加载、远程组件和完整离线写队列；
- 自动上传体验版或正式版本。

## 方案比较与决策

### 方案 A：根目录前端 + `server/` Hono（采用）

根目录保持方案文档要求的 `src/`、`features/`、`shared/` 和 `stores/` 布局，Hono 放在 `server/`，通过根 `package.json` 统一脚本管理。

优点：

- 与既有方案文档目录一致；
- 当前目录可以直接运行，不需要额外进入子项目；
- 前端和 API 的边界清晰，后续可将 Hono 替换为真实后端；
- Hono App 可脱离端口直接用 `app.request()` 测试。

### 方案 B：`apps/miniprogram` + `apps/api` workspace

前端和后端各自拥有 package 与 TypeScript 配置，通过 pnpm workspace 管理。

不采用的原因：隔离更强，但会偏离已确认的根 `src/` 布局，并增加脚本、依赖和文档维护成本；当前目标是先获得一套完整可运行的基础设施和首个垂直切片。

### 方案 C：只做前端 mock adapter

前端请求层直接返回本地 Fixture，不启动真实 HTTP 服务。

不采用的原因：无法验证 URL、Header、Envelope、401 刷新、HTTP 错误和跨层请求行为；用户已明确允许使用 Hono，因此使用真实本地 HTTP 边界更有价值。

## 总体架构

```text
微信小程序页面 / 组件
        ↓
领域 Query / Mutation
        ↓
项目 Query Adapter（Query Core Observer → Wevu Ref）
        ↓
领域 Service
        ↓
项目 HTTP Client
        ↓
wevu/fetch
        ↓
Hono API（本地测试后端）
        ↓
内存 Fixture
```

客户端状态与服务端状态严格分离：

| 数据 | 所有者 |
|---|---|
| accessToken、refreshToken、当前账号、初始化标记 | Wevu Auth Store |
| 主题、Feature Flag 等短小偏好 | Wevu Store |
| 订单列表、订单详情、请求状态、失效和重取 | Query Core |
| 弹窗、当前筛选输入、当前 Tab | 页面本地 `ref` |
| 可分享的详情 ID、筛选参数 | 路由 Query |

依赖方向固定为：

```text
pages / components
    ↓
features / composables
    ↓
services
    ↓
shared/http + shared/query
    ↓
wevu/fetch + @tanstack/query-core
    ↓
wevu/api / 微信宿主
```

禁止页面直接导入 `wx.request`、`wpi.request`、`fetch` 或原生导航 API；禁止 Service 弹 Toast；禁止把订单列表长期复制到 Store。

## 目录与职责

```text
.
├── .agents/
│   └── skills/
│       ├── wevu-page/SKILL.md
│       ├── mini-program-routing/SKILL.md
│       ├── mini-program-api/SKILL.md
│       ├── wevu-query-state/SKILL.md
│       └── mini-program-runtime-acceptance/SKILL.md
├── .codex/config.toml
├── .github/workflows/verify.yml
├── docs/
│   ├── architecture.md
│   ├── routing.md
│   ├── http-client.md
│   ├── query-state.md
│   ├── ui-guidelines.md
│   ├── testing.md
│   └── agent-workflow.md
├── server/
│   ├── app.ts
│   ├── index.ts
│   ├── envelope.ts
│   ├── fixtures.ts
│   ├── types.ts
│   ├── middleware/auth.ts
│   └── routes/
│       ├── auth.ts
│       ├── orders.ts
│       └── profile.ts
├── src/
│   ├── app.vue
│   ├── app.json.ts
│   ├── pages/
│   │   ├── home/index.vue
│   │   ├── profile/index.vue
│   │   ├── login/index.vue
│   │   └── error/index.vue
│   ├── subpackages/order/pages/
│   │   ├── list/index.vue
│   │   └── detail/index.vue
│   ├── components/ui/
│   │   ├── app-empty/
│   │   ├── app-error/
│   │   ├── app-loading/
│   │   └── page-shell/
│   ├── features/
│   │   ├── auth/
│   │   │   ├── models.ts
│   │   │   ├── service.ts
│   │   │   ├── queries.ts
│   │   │   └── query-keys.ts
│   │   └── order/
│   │       ├── models.ts
│   │       ├── service.ts
│   │       ├── queries.ts
│   │       └── query-keys.ts
│   ├── router/
│   │   ├── index.ts
│   │   ├── navigation.ts
│   │   ├── query.ts
│   │   ├── route-meta.ts
│   │   └── types.ts
│   ├── stores/
│   │   ├── auth.ts
│   │   ├── app.ts
│   │   ├── preferences.ts
│   │   └── plugins/
│   ├── shared/
│   │   ├── env.ts
│   │   ├── http/
│   │   ├── logger/
│   │   └── query/
│   ├── platform/
│   ├── styles/
│   └── types/
├── tests/
│   ├── unit/http/
│   ├── unit/query/
│   ├── unit/router/
│   ├── unit/stores/
│   ├── helpers/
│   └── fixtures/
├── AGENTS.md
├── .env.example
├── .npmrc
├── package.json
├── project.config.json
├── tsconfig.app.json
├── tsconfig.server.json
├── vite.config.ts
└── vitest.config.ts
```

`weapp-vite` 生成的 `.weapp-vite/` 文件属于生成产物，由 `pnpm prepare` 生成，不作为手写源码维护。`pnpm-lock.yaml` 提交并作为依赖版本事实来源。

## Hono 测试后端设计

### 运行方式

- Node.js 入口：`server/index.ts`；
- Hono App：`server/app.ts`；
- 默认监听：`127.0.0.1:8787`；
- 前端默认 API Base URL：`http://127.0.0.1:8787/api`；
- 可通过环境变量覆盖端口和前端 API 地址；
- CORS 只允许本地开发来源；
- 所有数据保存在进程内，服务重启后恢复 Fixture 初始状态。

### 统一响应

成功响应：

```ts
interface ApiEnvelope<T> {
  code: 'SUCCESS'
  message: string
  data: T
  traceId: string
}
```

错误响应仍使用同一 Envelope；HTTP 状态码表达传输层结果，`code` 表达业务结果。前端 HTTP Client 负责将它们归一为 `ApiError`。

### API 合同

| 方法 | 路径 | 鉴权 | 用途 |
|---|---|---|---|
| GET | `/api/health` | 无 | 测试服务健康状态 |
| POST | `/api/auth/login` | 无 | 校验固定测试账号并创建会话 |
| POST | `/api/auth/refresh` | 无 | 用 refreshToken 创建新 accessToken |
| GET | `/api/profile` | Bearer | 返回当前测试用户 |
| GET | `/api/orders` | Bearer | 分页、筛选订单 |
| GET | `/api/orders/:id` | Bearer | 返回订单详情 |
| POST | `/api/orders/:id/cancel` | Bearer | 将可取消订单标记为已取消 |

固定测试账号：`demo / demo123`。Token 只用于本地测试，不写入日志，不作为安全凭据示例。

### 订单行为

- 初始 Fixture 至少包含待支付、处理中、已完成和可取消订单；
- 列表支持 `page`、`pageSize`、`status`、`keyword`；
- 详情使用稳定 ID；
- 只有可取消状态允许取消；
- 取消成功后更新内存 Fixture，前端精确更新详情并失效订单列表 Query；
- 找不到订单返回 404；状态不允许取消返回业务错误。

## 前端功能设计

### 页面

- 首页：展示项目状态、登录状态和进入订单列表的入口；
- 登录页：固定账号登录，成功后返回原始 `returnTo` 或首页；
- 订单列表：Query 分页/筛选、Loading、Empty、Error、Retry 和后台刷新；
- 订单详情：校验 `id`，展示详情，执行取消订单 Mutation；
- 我的：展示当前用户、退出登录和登录入口；
- 错误页：处理未捕获的页面级错误和返回首页。

### 路由

- 自动路由扫描 `src/pages/**` 和声明的 `src/subpackages/order/pages/**`；
- 受保护订单页同时使用 Router 守卫和页面入口 Guard；
- 所有导航经过 `src/router/navigation.ts`；
- Tab 页只使用无 Query 的 `switchTab` 语义；
- 详情页 Query 参数集中由 `src/router/query.ts` 解析；
- 禁止 Vue Router 和散落的原生导航调用。

### HTTP 与 Query

- `src/shared/http/transport.ts` 只负责单次请求；
- `src/shared/http/client.ts` 负责会话读取、401 重放和日志；
- `src/shared/http/token-refresh.ts` 使用单飞 Promise，刷新失败时清理私有缓存；
- `src/features/*/service.ts` 是领域 API 唯一入口；
- `src/shared/query/` 只依赖 `@tanstack/query-core` 和 Wevu，不依赖领域模块；
- 每个 Query Hook 只创建一个 Observer，并在卸载时退订；
- Mutation 默认不重试，取消订单成功后做最小范围失效。

## 错误、Loading 与日志

错误类型固定为：`cancelled`、`timeout`、`network`、`http`、`business`、`unauthorized`、`forbidden`、`decode`、`unknown`。

页面根据错误类型和状态处理，不根据原始错误字符串分支。Service 和 Transport 不弹 Toast；页面或 UI 反馈层拥有用户可见提示。

日志只记录事件名、请求方法、耗时、HTTP 状态、错误类型、错误码、Trace ID 和 Query Hash，不记录 Authorization、Token、完整请求体、响应体或个人信息。

## 测试与验收设计

### Vitest

覆盖以下行为：

- URL 和 Query 编码、Header 合并、Envelope 解码；
- 204、非 JSON、HTTP 错误、业务错误、网络错误、超时和取消；
- 并发 401 只触发一次刷新，刷新失败清理会话；
- Query Key 稳定性、Observer 退订、`enabled`、失效和 Mutation；
- 路由 Query、必填参数、登录重定向和受保护页直达；
- Auth Store 派生状态、持久化白名单和退出清理；
- Hono 健康检查、登录、鉴权、订单列表、详情、取消和错误状态。

### 静态验证

```bash
pnpm install --frozen-lockfile
pnpm prepare
pnpm typecheck
pnpm lint
pnpm test
pnpm build
pnpm analyze:budget
```

根脚本 `pnpm verify` 按上述顺序执行。CI 执行同一套静态流程，并上传测试覆盖率和分析报告。

### 运行时验证

目标路由和交互：

1. 打开首页；
2. 未登录进入订单页，确认跳转登录；
3. 使用 `demo / demo123` 登录；
4. 返回订单列表，确认成功、Loading 和空/错误分支；
5. 进入订单详情；
6. 取消可取消订单；
7. 返回列表，确认 Query 失效刷新；
8. 退出登录，确认私有缓存和路由状态清理；
9. 检查页面栈、Console 和关键节点；
10. UI 变化时采集截图并进行视觉比较。

如果微信开发者工具或 weapp-vite MCP 不可用，报告中明确列出未完成的运行时步骤，不以构建结果替代运行时证据。

## 依赖与安全约束

- `weapp-vite` 与 `wevu` 使用官方脚手架生成的精确匹配版本；
- `@tanstack/query-core`、`tdesign-miniprogram`、Hono 和 Node adapter 锁定在 `pnpm-lock.yaml`；
- 禁止安装 Pinia Colada、标准 Pinia、Vue Router、Axios 和浏览器 Vue UI 库；允许 `weapp-tailwindcss`，但不引入原生 Web TailwindCSS 配置、类名扫描和浏览器运行时工作流；
- Hono Fixture 不保存真实密钥，不模拟生产支付或生产权限；
- CORS、日志和测试账号只服务本地开发；
- Codex MCP 仅启用 weapp-vite 相关工具，不暴露发布和上传工具；
- `AGENTS.md` 约束页面、HTTP、Query、Store、依赖和运行时验收边界。

## Definition of Done

- 根目录可安装依赖并生成 weapp-vite 产物；
- Hono API 可独立启动并通过测试；
- 登录、订单列表、订单详情、取消订单流程代码完整；
- HTTP、Query、Router、Store 和 Hono 路由有可重复测试；
- 未引入禁止依赖，页面没有直接调用底层网络或导航 API；
- `pnpm prepare`、`pnpm typecheck`、`pnpm lint`、`pnpm test`、`pnpm build` 和 `pnpm analyze:budget` 有实际输出记录；
- 若环境支持，完成 MCP/DevTools 运行时证据；否则明确记录未验证项；
- 完成报告列出文件、命令、决策、运行时证据和剩余风险。

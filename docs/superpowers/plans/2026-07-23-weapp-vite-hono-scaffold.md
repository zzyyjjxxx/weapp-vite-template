# weapp-vite + Wevu + Hono Scaffold Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** 在当前目录建立可运行、可测试、可由 Codex 维护的微信小程序脚手架，并用同仓 Hono 内存 API 验证登录和订单垂直切片。

**Architecture:** 根目录的 src/ 是 weapp-vite/Wevu 小程序前端，server/ 是纯 Hono App 和 Node 启动入口。页面只依赖领域 Query/Mutation，领域 Service 只依赖项目 HTTP Client，HTTP Client 通过 wevu/fetch 访问本地 Hono；Query Core 通过项目 Adapter 桥接到 Wevu Ref，Auth Store 只保存会话和客户端状态。

**Tech Stack:** weapp-vite, wevu, Wevu Vue SFC, tdesign-miniprogram, @tanstack/query-core, hono, @hono/node-server, vitest, vue-tsc, eslint, sass, pnpm。

## Global Constraints

- 当前目录 /Users/mang/Project/weapp-vite-template 是项目根目录。
- weapp-vite 与 wevu 使用官方脚手架生成的精确匹配版本，并提交 pnpm-lock.yaml。
- 页面运行时 API 只能从 wevu 导入；禁止在业务代码中使用标准 vue 运行时入口。
- 禁止安装或导入 pinia、@pinia/colada、vue-router、axios、浏览器版 Vue UI 库和默认 TailwindCSS。
- 普通 JSON 请求唯一入口是 src/shared/http/client.ts；领域 API 唯一入口是 src/features/*/service.ts。
- 页面不直接调用 fetch、wpi、wx.request 或原生导航 API。
- Query Core 只放服务端状态；Wevu Store 只放会话、偏好和其他短小客户端状态。
- 本地 Hono API 只使用内存 Fixture，不连接生产数据库、不包含生产凭据、不执行真实支付或发布。
- 新增生产代码遵循 TDD：先写一个能正确失败的测试，确认失败后再实现最小代码；配置文件和官方生成文件是例外。
- 每个任务完成后只暂存本任务文件并创建一个独立 Git 提交；不得把 .DS_Store 或用户原始方案文档带入任务提交。
- 未完成 pnpm verify、构建和可用的运行时检查前，不宣称脚手架完成。

---

## File Map

~~~text
src/                         # Wevu 小程序源码
server/                      # Hono 测试后端
tests/                       # Vitest 测试
docs/                        # 项目事实来源
.agents/skills/              # 项目级 Agent 工作流
.codex/config.toml           # weapp-vite MCP 允许列表
.github/workflows/verify.yml # 静态 CI
AGENTS.md
.env.example
.gitignore
.npmrc
package.json
project.config.json
tsconfig.app.json
tsconfig.server.json
vite.config.ts
vitest.config.ts
~~~

---

### Task 1: Bootstrap the root toolchain

**Files:**

- Create or modify: package.json
- Create or modify: pnpm-lock.yaml
- Create or modify: vite.config.ts
- Create or modify: tsconfig.app.json
- Create: tsconfig.server.json
- Create: vitest.config.ts
- Create: project.config.json
- Create: .npmrc
- Create: .gitignore
- Create: .env.example
- Create: server/index.ts
- Create: server/app.ts
- Test: tests/smoke/toolchain.test.ts

**Interfaces:**

- Package scripts: dev:weapp, dev:api, dev, prepare, typecheck, typecheck:app, typecheck:server, lint, test, test:coverage, build, build:server, analyze:budget, verify。
- server/app.ts exports app: Hono and never opens a port。
- server/index.ts starts serve({ fetch: app.fetch, port })。
- Frontend and server have separate TypeScript projects。

- [ ] Step 1: Inspect actual CLI contracts.

Run:

~~~bash
pnpm create weapp-vite --help
pnpm exec wv --help
~~~

Expected: help text, no new project. Record the actual current template and option names before using the generator。

- [ ] Step 2: Generate the official Wevu template into the current root.

Use the exact command exposed by the help output. It must select the official Wevu template, preserve docs/ and .git/, and produce src/, vite.config.ts, package.json, and generated .weapp-vite/ after prepare。

If a non-empty root is rejected, generate a temporary sibling project with the same template, copy only generated project files into this root with a controlled operation, and do not copy a second .git directory or overwrite the approved design/plan docs。

- [ ] Step 3: Add the initial smoke test before custom behavior.

~~~ts
import { describe, expect, it } from 'vitest'

describe('toolchain bootstrap', () => {
  it('loads the test runner', () => {
    expect('weapp-vite-wevu-hono').toBe('weapp-vite-wevu-hono')
  })
})
~~~

Run:

~~~bash
pnpm test tests/smoke/toolchain.test.ts
~~~

Expected: PASS。

- [ ] Step 4: Add separated scripts and server TypeScript configuration.

Use this script contract, changing only syntax proven necessary by the installed CLI:

~~~json
{
  "scripts": {
    "dev:weapp": "wv dev -p weapp",
    "dev:api": "tsx server/index.ts",
    "prepare": "wv prepare",
    "typecheck": "pnpm typecheck:app && pnpm typecheck:server",
    "typecheck:app": "vue-tsc -p tsconfig.app.json --noEmit",
    "typecheck:server": "tsc -p tsconfig.server.json --noEmit",
    "lint": "eslint .",
    "test": "vitest run",
    "test:coverage": "vitest run --coverage",
    "build": "wv build -p weapp",
    "build:server": "tsc -p tsconfig.server.json --outDir dist/server",
    "analyze:budget": "wv analyze -p weapp --budget-check",
    "verify": "pnpm prepare && pnpm typecheck && pnpm lint && pnpm test && pnpm build && pnpm analyze:budget"
  }
}
~~~

Use a separate server project:

~~~json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "useUnknownInCatchVariables": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "types": ["node"]
  },
  "include": ["server/**/*.ts", "tests/server/**/*.ts"]
}
~~~

Use a root .npmrc with save-exact=true and strict-peer-dependencies=true. Ignore generated output, node_modules, .env, .DS_Store, coverage, reports, .tmp, and current screenshot output, while retaining .env.example and approved baselines。

- [ ] Step 5: Add local project defaults.

.env.example:

~~~ini
VITE_API_BASE_URL=http://127.0.0.1:8787/api
API_HOST=127.0.0.1
API_PORT=8787
~~~

project.config.json must use a placeholder app ID, enable ES6, point to the generated Wevu output, and set urlCheck false only for this local test project. Document the production legal-domain requirement in docs/testing.md。

- [ ] Step 6: Verify and commit the bootstrap.

Run:

~~~bash
pnpm install
pnpm test tests/smoke/toolchain.test.ts
pnpm prepare
~~~

Expected: all exit 0. Then stage only Task 1 files and commit:

~~~bash
git add package.json pnpm-lock.yaml vite.config.ts tsconfig.app.json tsconfig.server.json vitest.config.ts project.config.json .npmrc .gitignore .env.example server/index.ts server/app.ts tests/smoke/toolchain.test.ts
git commit -m "chore: bootstrap wevu and hono toolchain"
~~~

Do not add the original plan document or .DS_Store。

---

### Task 2: Build the Hono test API with tests first

**Files:**

- Create: server/types.ts
- Create: server/envelope.ts
- Create: server/fixtures.ts
- Create: server/middleware/auth.ts
- Create: server/routes/auth.ts
- Create: server/routes/orders.ts
- Create: server/routes/profile.ts
- Modify: server/app.ts
- Modify: server/index.ts
- Create: tests/server/app.test.ts
- Create: tests/server/auth.test.ts
- Create: tests/server/orders.test.ts

**Interfaces:**

- server/app.ts exports app and never opens a port。
- server/types.ts exports AuthSession, User, Order, OrderStatus, OrderListInput, OrderListResult。
- server/envelope.ts exports success<T>(data, message?) and failure(code, message, status)。
- Auth middleware returns a typed test user or a Response。
- Routes expose the approved health, auth, profile, order list, detail, and cancellation API。

- [ ] Step 1: Write the failing API tests.

~~~ts
import { describe, expect, it } from 'vitest'
import { app } from '../../server/app'

describe('Hono test API', () => {
  it('returns a healthy envelope', async () => {
    const response = await app.request('/api/health')
    expect(response.status).toBe(200)
    expect(await response.json()).toMatchObject({
      code: 'SUCCESS',
      data: { status: 'ok' }
    })
  })

  it('rejects invalid credentials', async () => {
    const response = await app.request('/api/auth/login', {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ username: 'demo', password: 'wrong' })
    })
    expect(response.status).toBe(401)
  })

  it('requires a bearer token for orders', async () => {
    const response = await app.request('/api/orders')
    expect(response.status).toBe(401)
  })
})
~~~

Run pnpm test tests/server/app.test.ts. Expected: assertion failure caused by missing routes, not a test-loader error. Correct setup errors before implementing server behavior。

- [ ] Step 2: Implement deterministic Fixtures and Envelope。

Order status is pending, processing, completed, or cancelled. Fixture orders use stable IDs, numbers, amounts, ISO dates, labels, and canCancel. The success shape is:

~~~ts
interface ApiEnvelope<T> {
  code: 'SUCCESS'
  message: string
  data: T
  traceId: string
}
~~~

All errors use the same shape with a stable business code and HTTP status. Generate trace IDs per request and never log credentials。

- [ ] Step 3: Implement auth sessions and middleware。

The local account is demo / demo123. Store access and refresh tokens in process Maps. Login creates a session; refresh rotates access token; profile requires Authorization: Bearer accessToken. Missing, malformed, unknown, or expired tokens return 401 with UNAUTHORIZED and never reach a protected handler。

- [ ] Step 4: Implement order routes。

~~~text
GET  /api/orders?page=1&pageSize=10&status=&keyword=
GET  /api/orders/:id
POST /api/orders/:id/cancel
~~~

Validate page and pageSize, filter by status and case-insensitive keyword, return total/page/pageSize/items, and return 404 for an unknown order. Only pending and processing can be cancelled. Successful cancellation updates the in-memory item to cancelled; invalid state returns HTTP 409 with ORDER_NOT_CANCELLABLE。

- [ ] Step 5: Add the remaining API tests。

Verify successful login, refresh, profile, pagination, filtering, detail 404, cancellation success, cancellation conflict, malformed JSON, and missing authorization. Use a helper that logs in per test; do not stub global fetch。

- [ ] Step 6: Verify and commit。

~~~bash
pnpm test tests/server
pnpm typecheck:server
pnpm build:server
git add server tests/server
git commit -m "feat: add hono test api"
~~~

Expected: tests pass, server typecheck exits 0, and dist/server is generated。

---

### Task 3: Implement the HTTP boundary and safe logging

**Files:**

- Create: src/shared/env.ts
- Create: src/shared/http/types.ts
- Create: src/shared/http/errors.ts
- Create: src/shared/http/url.ts
- Create: src/shared/http/abort.ts
- Create: src/shared/http/transport.ts
- Create: src/shared/http/client.ts
- Create: src/shared/http/token-refresh.ts
- Create: src/shared/logger/index.ts
- Create: src/shared/http/AGENTS.override.md
- Create: tests/unit/http/url.test.ts
- Create: tests/unit/http/errors.test.ts
- Create: tests/unit/http/transport.test.ts
- Create: tests/unit/http/token-refresh.test.ts
- Create: tests/helpers/deferred.ts
- Create: tests/helpers/fake-storage.ts

**Interfaces:**

- buildUrl(baseUrl, path, query) returns an encoded URL without the browser URL global。
- mergeSignalWithTimeout(external, timeoutMs) returns signal, cleanup, and didTimeout。
- transportRequest<TResponse, TBody>(options, accessToken?) performs one request and maps failures to ApiError。
- request<TResponse, TBody>(options) is the only public JSON request function。
- refreshAccessTokenSingleFlight() returns Promise<AuthSession> and never calls public request()。
- sanitizeError(error) returns only approved fields。

- [ ] Step 1: Write failing URL, error, and abort tests。

~~~ts
import { describe, expect, it } from 'vitest'
import { buildUrl } from '@/shared/http/url'

describe('buildUrl', () => {
  it('normalizes slashes and encodes repeated values', () => {
    expect(buildUrl('http://api.test/', '/orders', {
      keyword: 'a b',
      status: ['pending', 'processing'],
      empty: undefined
    })).toBe('http://api.test/orders?keyword=a%20b&status=pending&status=processing')
  })
})
~~~

Add tests for every ApiError kind, external abort, timeout detection, cleanup, 204, success Envelope, non-JSON error text, HTTP errors, and business errors. Run focused tests and confirm expected failure before writing implementations。

- [ ] Step 2: Implement pure HTTP types and utilities。

~~~ts
export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE' | 'HEAD'
export type QueryPrimitive = string | number | boolean | null | undefined
export type RequestAuthMode = 'required' | 'optional' | 'none'

export interface RequestOptions<TBody = unknown> {
  path: string
  method?: HttpMethod
  query?: Record<string, QueryPrimitive | QueryPrimitive[]>
  body?: TBody
  headers?: Record<string, string>
  auth?: RequestAuthMode
  signal?: AbortSignal
  timeoutMs?: number
  skipTokenRefresh?: boolean
}
~~~

ApiError includes kind, status, code, traceId, retryable, and cause. buildUrl repeats array values and skips null/undefined. mergeSignalWithTimeout uses one AbortController and deterministic cleanup。

- [ ] Step 3: Implement transport using wevu/fetch。

Transport must use env.apiBaseUrl, merge accept/caller headers/content type/Bearer auth, avoid bodies for GET/HEAD when absent, treat 204 as undefined, decode the approved Envelope, map 401/403/other HTTP errors before business-code validation, map TypeError to network, map abort to cancelled/timeout, and always clean up in finally. Tests use an injected transport seam rather than global wx stubs。

- [ ] Step 4: Implement logger, public client, and single-flight refresh。

logger.debug/info/warn/error accept an event and LogContext. sanitizeError never serializes request/response objects. request reads Auth Store through explicit actions, rejects missing required tokens, refreshes once on unauthorized, and replays once with skipTokenRefresh true. Two concurrent 401 requests must share one refresh Promise; refresh failure clears the session and private Query cache through an injected boundary。

- [ ] Step 5: Verify and commit。

~~~bash
pnpm test tests/unit/http
pnpm typecheck:app
pnpm lint src/shared/http src/shared/logger
git add src/shared/env.ts src/shared/http src/shared/logger tests/unit/http tests/helpers/deferred.ts tests/helpers/fake-storage.ts
git commit -m "feat: add typed http boundary"
~~~

Expected: focused tests and typecheck pass; no full-body or credential logging exists。

---

### Task 4: Add the Query Core to Wevu adapter

**Files:**

- Create: src/shared/query/client.ts
- Create: src/shared/query/lifecycle.ts
- Create: src/shared/query/types.ts
- Create: src/shared/query/use-query.ts
- Create: src/shared/query/use-mutation.ts
- Create: src/shared/query/private-cache.ts
- Create: src/shared/query/AGENTS.override.md
- Create: tests/unit/query/client.test.ts
- Create: tests/unit/query/adapter.test.ts
- Create: tests/unit/query/mutation.test.ts

**Interfaces:**

- queryClient is the one project QueryClient。
- useQuery(resolveOptions) creates one QueryObserver, bridges results through Wevu refs, calls setOptions on input changes, and unsubscribes on unmount。
- useMutation(resolveOptions) creates one MutationObserver, exposes mutate/mutateAsync/state/reset, and disposes deterministically。
- clearPrivateQueryCaches() removes authenticated query data without importing domain code。
- setupQueryOnlineManager() connects Query Core onlineManager to a platform adapter。

- [ ] Step 1: Write failing adapter tests。

The first test proves disposal:

~~~ts
it('unsubscribes one observer when the hook scope is disposed', async () => {
  const hook = createQueryTestScope({
    queryKey: ['test', 'one'],
    queryFn: async () => 'ok'
  })
  await hook.flush()
  expect(hook.data.value).toBe('ok')
  hook.dispose()
  expect(hook.observerSubscriberCount()).toBe(0)
})
~~~

Add tests for key changes, enabled false, deduplication, cancellation, invalidation, mutation success/error/reset, and no observer leak. Confirm failure before implementation。

- [ ] Step 2: Implement QueryClient defaults and logging。

Use staleTime 30 seconds, gcTime 5 minutes, refetchOnWindowFocus false, refetchOnReconnect true, at most two retries for retryable ApiError, and mutation retry 0. Query and Mutation cache callbacks log only hashes/IDs and sanitized errors。

- [ ] Step 3: Implement useQuery。

Use installed Query Core types, not a framework adapter:

~~~ts
const observer = new QueryObserver(queryClient, resolveOptions())
const result = shallowRef(observer.getCurrentResult())
const unsubscribe = observer.subscribe((next) => { result.value = next })
const stop = watchEffect(() => { observer.setOptions(resolveOptions()) })

onUnmounted(() => {
  stop()
  unsubscribe()
})
~~~

Return result, data, error, status, fetchStatus, isPending, isFetching, isError, isSuccess, and refetch. Preserve Query Core AbortSignal in domain query functions。

- [ ] Step 4: Implement useMutation, cache clearing, and online lifecycle。

Use one MutationObserver per hook, expose mutate/mutateAsync/reset and state refs, and remove only private authenticated queries. Network status is provided by a platform adapter so tests do not need global wx。

- [ ] Step 5: Verify and commit。

~~~bash
pnpm test tests/unit/query
pnpm typecheck:app
pnpm lint src/shared/query
git add src/shared/query tests/unit/query
git commit -m "feat: add wevu query core adapter"
~~~

---

### Task 5: Add stores, persistence, and typed navigation

**Files:**

- Create: src/stores/auth.ts
- Create: src/stores/app.ts
- Create: src/stores/preferences.ts
- Create: src/stores/plugins/index.ts
- Create: src/stores/plugins/persistence.ts
- Create: src/stores/plugins/logging.ts
- Create: src/platform/storage.ts
- Create: src/platform/network-status.ts
- Create: src/router/types.ts
- Create: src/router/query.ts
- Create: src/router/route-meta.ts
- Create: src/router/navigation.ts
- Create: src/router/index.ts
- Create: tests/unit/stores/auth.test.ts
- Create: tests/unit/stores/persistence.test.ts
- Create: tests/unit/router/query.test.ts
- Create: tests/unit/router/navigation.test.ts

**Interfaces:**

- AuthSession contains accessToken, refreshToken, expiresAt, userId, and optional tenantId。
- useAuthStore exposes session, initialized, isAuthenticated, setSession, clearSession, getAccessToken, getRefreshToken, and markInitialized。
- StorageAdapter exposes get<T>, set<T>, and remove。
- parseRequiredString, parseOptionalNumber, and parseEnum reject invalid route input。
- navigate(path, query?), replace(path, query?), and buildLoginRedirect(returnTo) are the only application navigation APIs。

- [ ] Step 1: Write failing Store and route tests。

~~~ts
it('is authenticated only while a non-expired session exists', () => {
  const auth = createAuthStoreForTest()
  auth.setSession({
    accessToken: 'access',
    refreshToken: 'refresh',
    expiresAt: Date.now() + 60_000,
    userId: 'user-demo'
  })
  expect(auth.isAuthenticated).toBe(true)
  auth.clearSession()
  expect(auth.isAuthenticated).toBe(false)
})

it('rejects a missing required route query value', () => {
  expect(() => parseRequiredString(undefined, 'id')).toThrow('id')
})
~~~

Add persistence whitelist/version tests, login returnTo encoding tests, and Tab-query rejection tests. Confirm correct failure before implementation。

- [ ] Step 2: Implement Auth Store and explicit persistence。

Persist only:

~~~ts
interface PersistedAuthStateV1 {
  version: 1
  session: AuthSession | null
}
~~~

Use injectable storage in tests and the Wevu/wpi adapter in runtime. Hydrate once during setupStorePlugins; clear persisted state on logout. Never serialize the whole Store, Query data, loading state, errors, or functions。

- [ ] Step 3: Implement route metadata, parsing, and navigation。

Mark home/profile as Tab, login as public, and order list/detail as authenticated. encodeQuery repeats arrays, skips null/undefined, and uses encodeURIComponent. buildLoginRedirect encodes returnTo once and prevents login loops. navigate uses typed auto routes and switchTab only for Tab routes。

- [ ] Step 4: Implement platform network status and initialization hooks。

Wrap wpi network events in src/platform/network-status.ts. The final app shell will call setupStorePlugins, setupRouter, and setupQueryOnlineManager at top level before any Store use。

- [ ] Step 5: Verify and commit。

~~~bash
pnpm test tests/unit/stores tests/unit/router
pnpm typecheck:app
pnpm lint src/stores src/router src/platform
git add src/stores src/router src/platform tests/unit/stores tests/unit/router
git commit -m "feat: add auth store and typed navigation"
~~~

---

### Task 6: Add domain services, Query keys, UI primitives, and pages

**Files:**

- Create: src/features/auth/models.ts
- Create: src/features/auth/query-keys.ts
- Create: src/features/auth/service.ts
- Create: src/features/auth/queries.ts
- Create: src/features/order/models.ts
- Create: src/features/order/query-keys.ts
- Create: src/features/order/service.ts
- Create: src/features/order/queries.ts
- Create: src/components/ui/page-shell/index.vue
- Create: src/components/ui/app-loading/index.vue
- Create: src/components/ui/app-empty/index.vue
- Create: src/components/ui/app-error/index.vue
- Create: src/styles/tokens.scss
- Create: src/styles/reset.scss
- Create: src/styles/utilities.scss
- Create or modify: src/app.vue
- Create or modify: src/app.json.ts
- Create: src/pages/home/index.vue
- Create: src/pages/profile/index.vue
- Create: src/pages/login/index.vue
- Create: src/pages/error/index.vue
- Create: src/subpackages/order/pages/list/index.vue
- Create: src/subpackages/order/pages/detail/index.vue
- Create: tests/unit/features/order.test.ts
- Create: tests/unit/features/auth.test.ts

**Interfaces:**

- Auth Service: login(input, options?), refresh(refreshToken), getProfile(options?)。
- Order Service: getOrders(input, options?), getOrder(id, options?), cancelOrder(id)。
- orderKeys.all, orderKeys.list(input), and orderKeys.detail(id) are stable serializable Query Keys。
- useOrderListQuery(inputRef) and useOrderDetailQuery(idRef) use Query Core options and pass signal to Service。
- useCancelOrderMutation invalidates the smallest affected list prefix after success。

- [ ] Step 1: Write failing domain tests。

~~~ts
it('includes every list input in the order list key', () => {
  expect(orderKeys.list({ page: 1, pageSize: 10, status: 'pending', keyword: '' }))
    .not.toEqual(orderKeys.list({ page: 2, pageSize: 10, status: 'pending', keyword: '' }))
})

it('encodes an order id through the Service boundary', async () => {
  const request = createRequestSpy()
  await getOrder('order/1', { request })
  expect(request).toHaveBeenCalledWith(expect.objectContaining({
    path: '/orders/order%2F1',
    method: 'GET'
  }))
})
~~~

Confirm failure because domain modules do not exist。

- [ ] Step 2: Implement domain models and Services。

OrderListInput contains page, pageSize, optional status and keyword; OrderListResult contains items, total, page, and pageSize. Services call only request(), pass AbortSignal for GETs, encode IDs, and never display UI. Login uses auth none; profile/orders require auth。

- [ ] Step 3: Implement Query and Mutation wrappers。

List keys include every filter and use a 30-second staleTime. Detail is disabled until an ID exists. Cancel Mutation sets updated detail data, invalidates the smallest list prefix, and leaves Toast ownership to the page/UI layer。

- [ ] Step 4: Implement UI primitives and styles。

Use project tokens: primary #0052d9, success #00a870, warning #ed7b2f, error #d54941, text #1d2129/#4e5969, page #f5f7fa, card #ffffff, spacing 16rpx/24rpx/32rpx/48rpx. UI primitives are semantic, have no domain imports, and use TDesign without adding another complete UI library。

- [ ] Step 5: Implement app shell and pages。

app.vue initializes Store plugins, Router, and Query online manager at top level and toggles focus on Wevu onShow/onHide. app.json.ts consumes generated routes and defines home/profile Tab entries. vite.config.ts declares subpackages/order and TDesign Resolver。

Each page uses script setup lang=ts, definePageJson, runtime imports from wevu, explicit TDesign values/events where needed, and states for initial loading, empty, initial error, retry, background refresh, direct-entry auth, and mutation feedback。

- [ ] Step 6: Verify and commit。

~~~bash
pnpm prepare
pnpm test tests/unit/features
pnpm typecheck:app
pnpm lint src/features src/components src/pages src/subpackages src/app.vue src/app.json.ts
pnpm build
git add src/features src/components src/styles src/app.vue src/app.json.ts src/pages src/subpackages vite.config.ts tests/unit/features
git commit -m "feat: add auth and order vertical slice"
~~~

Expected: generated routes contain home, profile, login, error, and order subpackage; build exits 0。

---

### Task 7: Add Agent rules, project Skills, docs, MCP, CI, and reporting

**Files:**

- Create: AGENTS.md
- Create: src/shared/http/AGENTS.override.md
- Create: src/shared/query/AGENTS.override.md
- Create: .agents/skills/wevu-page/SKILL.md
- Create: .agents/skills/mini-program-routing/SKILL.md
- Create: .agents/skills/mini-program-api/SKILL.md
- Create: .agents/skills/wevu-query-state/SKILL.md
- Create: .agents/skills/mini-program-runtime-acceptance/SKILL.md
- Create: .codex/config.toml
- Create: docs/architecture.md
- Create: docs/routing.md
- Create: docs/http-client.md
- Create: docs/query-state.md
- Create: docs/ui-guidelines.md
- Create: docs/testing.md
- Create: docs/agent-workflow.md
- Create: .github/workflows/verify.yml
- Create: reports/.gitkeep
- Create: .screenshots/baseline/.gitkeep

**Interfaces:**

- Root AGENTS.md is the concise rule and command map。
- HTTP/Query overrides apply only to their subsystem。
- Each Skill states trigger, files to read, procedure, and completion evidence。
- .codex/config.toml has an explicit weapp-vite MCP allowlist and approval-gated interaction/CLI/screenshot tools。
- CI runs install, prepare, typecheck, lint, coverage, build, and analyze-budget without publishing。

- [ ] Step 1: Write Agent rules。

Root AGENTS.md must require reading the nearest override, checking Git state, reading the relevant docs, loading a Skill, and running a focused test before changes. It must enforce Wevu SFC, domain-only page dependencies, Query/Store ownership, forbidden dependencies, local-only Hono, no credentials, generated-file policy, and completion evidence.

HTTP override prohibits UI feedback, full-body logging, and transport/client circular dependencies. Query override prohibits domain imports, framework adapters, observer leaks, and full-cache persistence。

- [ ] Step 2: Add the five local Skills。

Create the page, routing, API, Query, and runtime-acceptance Skills. Each must state when it applies and when it does not. Runtime acceptance must report route/query, page stack, interactions, Console, screenshot, diff, and unverified states。

- [ ] Step 3: Add durable docs。

Create architecture.md, routing.md, http-client.md, query-state.md, ui-guidelines.md, testing.md, and agent-workflow.md. Each links to the approved design and uses actual scripts/files, without duplicating unrelated background。

- [ ] Step 4: Add MCP and CI。

Use the actual output of pnpm exec wv mcp print codex as the command source. Commit an explicit allowlist, prompt approval for CLI/navigation/tap/input, screenshot approval, and no upload/publish tool. CI is:

~~~yaml
- run: pnpm install --frozen-lockfile
- run: pnpm prepare
- run: pnpm typecheck
- run: pnpm lint
- run: pnpm test:coverage
- run: pnpm build
- run: pnpm analyze:budget
~~~

Do not describe generic hosted CI as DevTools runtime CI。

- [ ] Step 5: Verify and commit。

Run:

~~~bash
pnpm mcp:print
pnpm mcp:doctor
git diff --check
git add AGENTS.md src/shared/http/AGENTS.override.md src/shared/query/AGENTS.override.md .agents .codex docs .github reports .screenshots
git commit -m "chore: add agent harness and ci"
~~~

If the current CLI lacks an MCP script, record the actual unavailable command in docs/agent-workflow.md and report runtime tooling as incomplete; do not invent a passing result。

---

### Task 8: Full verification and runtime evidence

**Files:**

- Modify: reports/verification.md
- Create as available: .screenshots/baseline/home/default.png
- Create as available: .tmp/screenshots/*
- Create as available: .tmp/diffs/*

**Interfaces:**

- reports/verification.md is the durable evidence record。
- Runtime evidence is conditional on DevTools/MCP availability and lists unverified states。

- [ ] Step 1: Run complete static verification separately。

~~~bash
pnpm install --frozen-lockfile
pnpm prepare
pnpm typecheck
pnpm lint
pnpm test:coverage
pnpm build
pnpm analyze:budget
~~~

Every command must exit 0. A failure requires diagnosis, a focused fix, a rerun, and a separate fix commit before proceeding。

- [ ] Step 2: Run Hono smoke flow。

Start pnpm dev:api and verify:

~~~bash
curl -fsS http://127.0.0.1:8787/api/health
~~~

Use the Hono/Vitest request tests for login, profile, orders, detail, cancel, and refresh. Do not use real credentials。

- [ ] Step 3: Attempt local runtime acceptance。

Run pnpm mcp:doctor. If MCP/DevTools is available, connect, route to home, login, open protected orders, open detail, cancel an order, inspect page stack and Console, and capture screenshots. If unavailable, record the exact step and keep runtime status incomplete。

- [ ] Step 4: Write reports/verification.md with actual evidence。

~~~md
# Verification report

## Commands
- pnpm prepare — actual result
- pnpm typecheck — actual result
- pnpm lint — actual result
- pnpm test:coverage — actual result and test count
- pnpm build — actual result
- pnpm analyze:budget — actual result

## Hono
- Health: actual result
- Auth/orders tests: actual result

## Runtime
- Route/query: actual route or unavailable
- Interactions: actual interactions or unavailable
- Console: actual result or unavailable
- Screenshot/diff: actual paths or unavailable

## Remaining risks
- Only verified remaining risks
~~~

- [ ] Step 5: Review and commit evidence。

~~~bash
git diff --check
git status --short --branch
git log --oneline --decorate -12
git add reports/verification.md .screenshots/baseline .tmp/screenshots .tmp/diffs
git commit -m "test: record scaffold verification evidence"
git status --short --branch
~~~

Report intentionally untracked user files separately。

---

## Plan self-review

- Spec coverage: root layout, Hono API, frontend vertical slice, HTTP boundary, Query Adapter, Store, Router, UI, Agent harness, CI, tests, and runtime acceptance are all assigned。
- Placeholder scan: no unfinished or unspecified implementation step; version-sensitive generator and MCP commands require inspecting actual local CLI output。
- Type consistency: AuthSession, Order, RequestOptions, ApiError, queryClient, useQuery, useMutation, StorageAdapter, and route helper names are reused consistently。
- Commit isolation: every task stages only its own files and creates a dedicated commit; verification fixes are separate commits。
- Scope: one coherent scaffold with one Hono-backed vertical slice, not unrelated business features。

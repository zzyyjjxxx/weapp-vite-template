# 企业用地需求填报小程序实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将现有登录/订单/Hono 示例脚手架改造成可登录、五步填报、暂存、验证码提交、草稿恢复和重新修改的企业用地需求小程序。

**Architecture:** 页面和步骤组件只消费 Store 与 Query/Mutation；业务调用进入可替换的 Mock Service，再由基于小程序 Storage 的 Repository 持久化。所有业务规则集中在纯 TypeScript 模型、校验和 Payload 适配器中，UI 使用 Wevu、TDesign、小程序原生节点与构建期 Tailwind 工具类。

**Tech Stack:** TypeScript 6、weapp-vite 6.18.6、Wevu 6.18.6、TDesign MiniProgram 1.15.3、Tailwind CSS 4、weapp-tailwindcss 5.2.0、TanStack Query Core 5、Vitest 4、Playwright Test、weapp-ide-cli Automator。

## Global Constraints

- 企业登录必须保留，默认 Mock 账号为 `demo / demo123`。
- 五步顺序固定为：基本信息、用地需求、投资项目、融资及联系人、信息确认与提交。
- `deploy_landtype` 为单选；`deploy_height`、`deploy_weight` 始终显示且选填。
- `is_financing` 新建或缺失时默认“没有”；只有“有”时融资金额和时间显示且正式提交必填。
- `investment`、`pred_ys`、`pred_tax`、`pred_rdex` 单位为万元且必填。
- `pred_unitenergy` 单位为万元/吨标煤且必填。
- `projectdata` 使用不限制字符数的 `text` 语义。
- `project_hydm` 保存 `industryCode`；选项显示 `industryName（industryCode）`。
- 国民行业只使用数字 `pid` 在 `181..439` 之间的 515 条子项和 150 个父节点。
- 页面和组件不得直接访问 Storage、Mock Repository、`fetch`、`wx.request` 或原始导航 API。
- 运行时 API 从 `wevu` 导入；路由通过项目类型化导航层调用。
- 每个任务先观察聚焦测试按预期失败，再写生产代码，验证后单独提交。
- 不修改或提交用户拥有的 `.codex/config.toml`、`.mcp.json`、`CLAUDE.md` 和 `.DS_Store`。

---

## 文件结构

```text
src/
  features/auth/{models,repository,service,queries,query-keys}.ts
  features/land-demand/
    {models,defaults,visibility,validation,payload,repository,service,queries,query-keys}.ts
    dictionaries/{parks,land-types,industry-tracks,industries.generated}.ts
    components/*.vue
  pages/login/index.vue
  pages/home/index.vue
  pages/land-demand/index.vue
  pages/land-demand/success.vue
  stores/{auth,land-demand}.ts
e2e/
  fixtures/mini-program.ts
  support/mini-program-driver.ts
  land-demand.spec.ts
scripts/generate-industry-dictionary.mjs
tests/unit/features/*.test.ts
tests/unit/stores/*.test.ts
tests/smoke/product-shape.test.ts
```

## Task 1：收敛产品骨架和构建工具链

**Files:**
- Create: `tests/smoke/product-shape.test.ts`
- Modify: `package.json`
- Modify: `pnpm-lock.yaml`
- Modify: `vite.config.ts`
- Modify: `src/app.vue`
- Create: `src/styles/tailwind.css`
- Modify: `src/components/ui/page-shell/index.vue`
- Modify: `src/pages/home/index.vue`
- Modify: `src/router/route-meta.ts`
- Delete: `server/**`
- Delete: `src/features/order/**`
- Delete: `src/subpackages/order/**`
- Delete: `src/pages/profile/**`
- Delete: `tests/server/**`
- Delete: `tests/unit/features/order*.test.ts`
- Delete: `tests/unit/components/app-tab-bar.test.ts`
- Delete: `src/components/ui/app-tab-bar/**`
- Delete: `tsconfig.server.json`
- Delete: `scripts/dev-all.mjs`

**Interfaces:**
- Produces scripts `dev`, `dev:open`, `prepare`, `build`, `typecheck`, `test`, `test:coverage`, `test:e2e`, `lint`, `stylelint`, `analyze:budget`, `verify`.
- Produces main-package routes only; no Hono server or order subpackage.

- [ ] **Step 1: Write the failing product-shape test**

```ts
import { readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'

const pkg = JSON.parse(readFileSync('package.json', 'utf8'))
const vite = readFileSync('vite.config.ts', 'utf8')

describe('land-demand product shape', () => {
  it('has no Hono server or order subpackage scripts', () => {
    expect(pkg.dependencies).not.toHaveProperty('hono')
    expect(pkg.scripts).not.toHaveProperty('dev:api')
    expect(pkg.scripts).not.toHaveProperty('typecheck:server')
    expect(vite).not.toContain('subpackages/order')
  })

  it('declares the product test commands', () => {
    expect(pkg.scripts).toHaveProperty('test:e2e')
    expect(pkg.scripts.verify).toContain('pnpm build')
  })
})
```

- [ ] **Step 2: Run the test and confirm RED**

Run: `pnpm test tests/smoke/product-shape.test.ts`

Expected: FAIL because Hono scripts/dependencies and the order subpackage still exist and `test:e2e` is absent.

- [ ] **Step 3: Update package/config and delete demo surfaces**

Set the relevant package scripts to:

```json
{
  "dev": "wv dev -p weapp",
  "dev:open": "wv dev -p weapp --open",
  "typecheck": "vue-tsc --noEmit -p .weapp-vite/tsconfig.app.json",
  "test:e2e": "playwright test",
  "verify": "pnpm prepare && pnpm typecheck && pnpm lint && pnpm stylelint && pnpm test && pnpm build && pnpm analyze:budget"
}
```

Remove Hono/server dependencies and add direct development dependencies `tailwindcss@4.3.3`, `@playwright/test`, and `weapp-ide-cli@6.0.0`. Keep `weapp-tailwindcss@5.2.0`. Configure the plugin exactly as follows and import `@/styles/tailwind.css` from `src/app.vue`:

```ts
import { weappTailwindcss } from 'weapp-tailwindcss/vite'

const tailwindEntry = fileURLToPath(new URL('./src/styles/tailwind.css', import.meta.url))

plugins: [weappTailwindcss({ cssEntries: [tailwindEntry] })],
```

`src/styles/tailwind.css` contains exactly `@import "tailwindcss";`. Remove tab-bar imports/props from `PageShell`, replace the old home content with a compile-safe temporary product shell, and remove `weapp.subPackages` from `vite.config.ts` so Task 1 remains type-correct after deleting profile/order routes.

- [ ] **Step 4: Refresh generated routes and run GREEN checks**

Run:

```text
pnpm install
pnpm prepare
pnpm test tests/smoke/product-shape.test.ts
pnpm typecheck
```

Expected: all commands exit 0; generated routes contain no order/profile paths.

- [ ] **Step 5: Commit**

```text
git add package.json pnpm-lock.yaml vite.config.ts src tests tsconfig.json
git commit -m "chore: focus scaffold on land demand product"
```

## Task 2：实现 Mock 企业登录和持久化会话

**Files:**
- Create: `src/features/auth/repository.ts`
- Rewrite: `src/features/auth/models.ts`
- Rewrite: `src/features/auth/service.ts`
- Rewrite: `src/features/auth/queries.ts`
- Modify: `src/stores/auth.ts`
- Modify: `src/stores/plugins/persistence.ts`
- Rewrite: `tests/unit/features/auth.test.ts`
- Rewrite: `tests/unit/stores/auth.test.ts`
- Modify: `tests/unit/stores/persistence.test.ts`
- Delete: `src/shared/http/**`
- Delete: `tests/unit/http/**`

**Interfaces:**

```ts
export interface LoginInput { username: string; password: string }
export interface EnterpriseProfile {
  id: string
  username: string
  businessname: string
  creditcode: string
  county: string
  region: string
  contact: string
  office: string
  phone: string
}
export interface AuthSession {
  token: string
  expiresAt: number
  enterprise: EnterpriseProfile
}
export interface AuthRepository {
  login(input: LoginInput): Promise<AuthSession>
}
export function createMockAuthRepository(options: {
  now?: () => number
  delayMs?: number
}): AuthRepository
export function configureAuthRepository(repository?: AuthRepository): void
```

- [ ] **Step 1: Write failing repository and store tests**

```ts
it('logs in the demo enterprise and rejects invalid credentials', async () => {
  const repository = createMockAuthRepository({ now: () => 1_000 })
  const session = await repository.login({ username: 'demo', password: 'demo123' })
  expect(session.enterprise.creditcode).toBe('91330200MA2DEMO001')
  expect(session.expiresAt).toBeGreaterThan(1_000)
  await expect(repository.login({ username: 'demo', password: 'wrong' }))
    .rejects.toThrow('账号或密码错误')
})
```

The persistence test must include these concrete assertions using the existing test Store harness:

```ts
storage.set('land-demand.auth', { version: 1, session })
createPersistencePlugin(storage)(context)
expect(context.store.session).toEqual(session)

storage.set('land-demand.auth', { version: 1, session: { token: 42 } })
createPersistencePlugin(storage)(malformedContext)
expect(malformedContext.store.session).toBeNull()
```

- [ ] **Step 2: Run RED**

Run: `pnpm test tests/unit/features/auth.test.ts tests/unit/stores/auth.test.ts tests/unit/stores/persistence.test.ts`

Expected: FAIL because the repository and new session shape do not exist.

- [ ] **Step 3: Implement minimal auth domain**

Create one deterministic enterprise fixture with the exact credentials and profile above. `createMockAuthRepository` returns a cloned session, never exposes the password, and throws a domain `Error` with the Chinese message for invalid credentials. The auth Store exposes:

```ts
setSession(session: AuthSession): void
clearSession(): void
markInitialized(): void
isAuthenticated: ComputedRef<boolean>
enterprise: ComputedRef<EnterpriseProfile | undefined>
```

Update `useLoginMutation` to call the configured repository through `login()` and persist on success.

- [ ] **Step 4: Run GREEN and remove obsolete HTTP code**

Run:

```text
pnpm test tests/unit/features/auth.test.ts tests/unit/stores/auth.test.ts tests/unit/stores/persistence.test.ts
pnpm typecheck
```

Expected: PASS and exit 0.

- [ ] **Step 5: Commit**

```text
git add src/features/auth src/stores src/shared tests/unit package.json pnpm-lock.yaml
git commit -m "feat: add mock enterprise authentication"
```

## Task 3：生成并验证业务字典

**Files:**
- Create: `scripts/generate-industry-dictionary.mjs`
- Create: `src/features/land-demand/dictionaries/industries.generated.ts`
- Create: `src/features/land-demand/dictionaries/parks.ts`
- Create: `src/features/land-demand/dictionaries/land-types.ts`
- Create: `src/features/land-demand/dictionaries/industry-tracks.ts`
- Create: `tests/unit/features/industry-dictionary.test.ts`
- Create: `tests/unit/features/business-dictionaries.test.ts`

**Interfaces:**

```ts
export interface IndustryLeaf {
  industryCode: string
  industryName: string
  pid: string
  label: string
}
export interface IndustryGroup {
  value: string
  label: string
  children: IndustryLeaf[]
}
export const INDUSTRY_GROUPS: readonly IndustryGroup[]
export const PARK_OPTIONS: readonly { value: string; label: string }[]
export const EXPECT_PARK_OPTIONS: typeof PARK_OPTIONS
export const LAND_TYPE_OPTIONS: readonly string[]
export const INDUSTRY_TRACK_DIRECTIONS: Readonly<Record<string, readonly string[]>>
export function getIndustryLabel(code: string): string | undefined
export function getDirections(track: string): readonly string[]
```

- [ ] **Step 1: Write failing dictionary tests**

```ts
it('contains exactly the selected national industries', () => {
  const leaves = INDUSTRY_GROUPS.flatMap(group => group.children)
  expect(INDUSTRY_GROUPS).toHaveLength(150)
  expect(leaves).toHaveLength(515)
  expect(leaves.every(item => Number(item.pid) >= 181 && Number(item.pid) <= 439)).toBe(true)
  expect(leaves.every(item => item.label === `${item.industryName}（${item.industryCode}）`)).toBe(true)
})

it('keeps Ningbo mutually exclusive metadata and land type single values', () => {
  expect(PARK_OPTIONS[0]).toEqual({ value: '330200', label: '宁波市' })
  expect(new Set(PARK_OPTIONS.map(item => item.value)).size).toBe(13)
  expect(LAND_TYPE_OPTIONS).toEqual(['小微园', '租售型闲置空间', '租售型标准厂房', '以上皆可'])
})
```

- [ ] **Step 2: Run RED**

Run: `pnpm test tests/unit/features/industry-dictionary.test.ts tests/unit/features/business-dictionaries.test.ts`

Expected: FAIL because dictionary modules are missing.

- [ ] **Step 3: Implement the SQL generator and generate the national dictionary**

The generator accepts the SQL path as its only argument, parses `INSERT INTO m_industryinfo VALUES (...)`, keeps rows with numeric `pid` in `181..439`, resolves each parent through `industryCode === pid`, sorts parents and leaves numerically, and writes deterministic TypeScript. Run:

```text
node scripts/generate-industry-dictionary.mjs "C:\Users\18556\Desktop\ydxq小程序\m_industryinfo.sql"
```

The generated module must contain all data needed at runtime; it must not retain the desktop path or parse SQL in the mini program.

- [ ] **Step 4: Add the confirmed regional, land-type and track-direction dictionaries**

Use the 13 region ID/name pairs and four land types from the design spec. `EXPECT_PARK_OPTIONS` reuses the same 13 regional options for the Mock single-select and saves the selected ID; `PARK_OPTIONS` remains the multi-select source for `deploy_park`. Implement every `pid_name/name` pair from Appendix A of this plan as `INDUSTRY_TRACK_DIRECTIONS`; `getDirections` returns an empty readonly array for unknown names.

- [ ] **Step 5: Run GREEN**

Run:

```text
pnpm test tests/unit/features/industry-dictionary.test.ts tests/unit/features/business-dictionaries.test.ts
pnpm typecheck
```

Expected: 515 leaves, 150 parents, all dictionary tests PASS.

- [ ] **Step 6: Commit**

```text
git add scripts/generate-industry-dictionary.mjs src/features/land-demand/dictionaries tests/unit/features
git commit -m "feat: add land demand dictionaries"
```

## Task 4：实现统一表单、可见性、校验和 Payload

**Files:**
- Create: `src/features/land-demand/models.ts`
- Create: `src/features/land-demand/defaults.ts`
- Create: `src/features/land-demand/visibility.ts`
- Create: `src/features/land-demand/validation.ts`
- Create: `src/features/land-demand/payload.ts`
- Create: `tests/unit/features/land-demand-defaults.test.ts`
- Create: `tests/unit/features/land-demand-visibility.test.ts`
- Create: `tests/unit/features/land-demand-validation.test.ts`
- Create: `tests/unit/features/land-demand-payload.test.ts`

**Interfaces:**

```ts
export type YesNo = '是' | '否'
export type FinancingChoice = '有' | '没有'
export type LandDemandStatus = '1' | '2'
export interface LandDemandForm {
  county: string; region: string; businessname: string; creditcode: string
  area: string; building_area: string; expect_park: string; expect_time: string
  is_deploy: YesNo | ''; deploy_park: string[]
  is_specialuse: YesNo | ''; deploy_landtype: string
  deploy_height: string; deploy_weight: string
  investment: string; project_hydm: string; keyindustry: string; futureindustry: string
  pred_ys: string; pred_tax: string; pred_rdex: string; pred_unitenergy: string
  projectdata: string
  is_financing: FinancingChoice; financing_money: string; financing_time: string
  contact: string; office: string; phone: string
}
export interface LandDemandRecord extends Omit<LandDemandForm, 'deploy_park'> {
  deploy_park: string
  landusedemand: LandDemandStatus
  updatetime: string
  updateuser: string
  newproject?: '1'
  industryCode?: string
  is_energy?: string
  energy?: string
  energy_time?: string
  qyhydm?: string
  registrationType?: number
}
export type SaveLandDemandPayload = Omit<LandDemandRecord, 'updatetime' | 'updateuser' | 'newproject' | 'industryCode'>
export type UpdateLandDemandPayload = Omit<LandDemandRecord, 'county' | 'region' | 'businessname' | 'updatetime' | 'updateuser'> & { newproject: '1' }
export interface LandDemandDraft { form: LandDemandForm; currentStep: 1 | 2 | 3 | 4 | 5; savedAt: number }
export interface FieldError { field: keyof LandDemandForm; step: 1 | 2 | 3 | 4; message: string }
export function createLandDemandForm(enterprise: EnterpriseProfile, record?: Partial<LandDemandRecord>): LandDemandForm
export function validateDraft(form: LandDemandForm): FieldError[]
export function validateSubmission(form: LandDemandForm): FieldError[]
export function selectDeployPark(current: readonly string[], next: string): string[]
export function applySpecialUseChoice(form: LandDemandForm, value: YesNo): LandDemandForm
export function applyFinancingChoice(form: LandDemandForm, value: FinancingChoice): LandDemandForm
export function applyTrackChoice(form: LandDemandForm, value: string): LandDemandForm
export function buildSavePayload(form: LandDemandForm, status: LandDemandStatus): SaveLandDemandPayload
export function buildUpdatePayload(form: LandDemandForm, original: LandDemandRecord, status: LandDemandStatus): UpdateLandDemandPayload
```

- [ ] **Step 1: Write failing defaults and rule tests**

```ts
it('defaults missing financing demand to 没有', () => {
  expect(createLandDemandForm(enterprise, { is_financing: '' }).is_financing).toBe('没有')
})

it('does not hide or clear optional height and weight with special use', () => {
  const next = applySpecialUseChoice({ ...form, deploy_height: '8', deploy_weight: '2' }, '否')
  expect(next.deploy_landtype).toBe('')
  expect(next.deploy_height).toBe('8')
  expect(next.deploy_weight).toBe('2')
})

it('makes all four project metrics required for submission', () => {
  const errors = validateSubmission({ ...validForm, pred_rdex: '', pred_unitenergy: '' })
  expect(errors.map(error => error.field)).toEqual(expect.arrayContaining(['pred_rdex', 'pred_unitenergy']))
})
```

Add these concrete cases to the same test files:

```ts
expect(validateDraft({ ...form, area: '' })).toEqual([])
expect(validateDraft({ ...form, area: '-1' })[0]?.field).toBe('area')
expect(validateSubmission({ ...validForm, investment: '' }).some(error => error.field === 'investment')).toBe(true)
expect(validateSubmission({ ...validForm, phone: '123' }).some(error => error.field === 'phone')).toBe(true)
expect(validateSubmission({ ...validForm, projectdata: '项'.repeat(2_000) }).some(error => error.field === 'projectdata')).toBe(false)
expect(selectDeployPark(['330203'], '330200')).toEqual(['330200'])
expect(selectDeployPark(['330200'], '330205')).toEqual(['330205'])
expect(applyTrackChoice({ ...form, futureindustry: '具身大模型（大脑与小脑）' }, '生物医药').futureindustry).toBe('')
expect(applyFinancingChoice({ ...form, financing_money: '100', financing_time: '2027-06' }, '没有'))
  .toMatchObject({ financing_money: '', financing_time: '' })
expect(buildSavePayload(validForm, '2').landusedemand).toBe('2')
expect(buildUpdatePayload(validForm, original, '1')).toMatchObject({ landusedemand: '1', newproject: '1' })
```

- [ ] **Step 2: Run RED**

Run: `pnpm test tests/unit/features/land-demand-*.test.ts`

Expected: FAIL because the form domain does not exist.

- [ ] **Step 3: Implement pure domain functions**

Use immutable object/array returns. `validateDraft` validates only non-empty fields; `validateSubmission` adds required checks. `projectdata` is checked only for non-empty content. `buildUpdatePayload` preserves original hidden values (`industryCode`, energy fields, `qyhydm`, `registrationType`) and never changes empty optional numeric values to zero.

- [ ] **Step 4: Run GREEN**

Run:

```text
pnpm test tests/unit/features/land-demand-*.test.ts
pnpm typecheck
```

Expected: all domain tests PASS.

- [ ] **Step 5: Commit**

```text
git add src/features/land-demand tests/unit/features
git commit -m "feat: add land demand form domain"
```

## Task 5：实现填报 Repository、Service、Query 和 Store

**Files:**
- Create: `src/features/land-demand/repository.ts`
- Create: `src/features/land-demand/service.ts`
- Create: `src/features/land-demand/query-keys.ts`
- Create: `src/features/land-demand/queries.ts`
- Create: `src/stores/land-demand.ts`
- Create: `tests/unit/features/land-demand-repository.test.ts`
- Create: `tests/unit/features/land-demand-service.test.ts`
- Create: `tests/unit/stores/land-demand.test.ts`

**Interfaces:**

```ts
export interface VerificationChallenge { phone: string; expiresAt: number; retryAt: number; mockCode: string }
export interface LandDemandRepository {
  get(creditcode: string): Promise<LandDemandRecord | undefined>
  save(payload: SaveLandDemandPayload): Promise<LandDemandRecord>
  update(payload: UpdateLandDemandPayload): Promise<LandDemandRecord>
  getDraft(creditcode: string): LandDemandDraft | undefined
  setDraft(creditcode: string, draft: LandDemandDraft): void
  removeDraft(creditcode: string): void
  sendCode(phone: string): Promise<VerificationChallenge>
  verifyCode(phone: string, code: string): Promise<void>
}
export function createMockLandDemandRepository(options: {
  storage: StorageAdapter
  now?: () => number
  randomCode?: () => string
  delayMs?: number
}): LandDemandRepository
export function configureLandDemandRepository(repository?: LandDemandRepository): void
export function getLandDemandInfo(creditcode: string, options?: { repository?: LandDemandRepository }): Promise<LandDemandRecord | undefined>
export function saveLandDemand(form: LandDemandForm, status: LandDemandStatus, options?: { repository?: LandDemandRepository; updateuser?: string }): Promise<LandDemandRecord>
export function updateLandDemand(form: LandDemandForm, original: LandDemandRecord, status: LandDemandStatus, options?: { repository?: LandDemandRepository; updateuser?: string }): Promise<LandDemandRecord>
export function sendVerificationCode(phone: string, options?: { repository?: LandDemandRepository }): Promise<VerificationChallenge>
export function verifyVerificationCode(phone: string, code: string, options?: { repository?: LandDemandRepository }): Promise<void>
```

Store API:

```ts
initialize(enterprise: EnterpriseProfile, record?: LandDemandRecord, draft?: LandDemandDraft): void
patch(patch: Partial<LandDemandForm>): void
goToStep(step: 1 | 2 | 3 | 4 | 5): void
saveLocalDraft(): void
discardLocalDraft(): void
markPersisted(record: LandDemandRecord): void
form: Ref<LandDemandForm>
currentStep: Ref<1 | 2 | 3 | 4 | 5>
hasRecord: Ref<boolean>
isDirty: Ref<boolean>
```

- [ ] **Step 1: Write failing persistence and workflow tests**

```ts
it('switches from save to update after the first draft persistence', async () => {
  const saved = await saveLandDemand(form, '2', { repository })
  expect(saved.landusedemand).toBe('2')
  expect(await repository.get(form.creditcode)).toBeDefined()
  const updated = await updateLandDemand({ ...form, area: '31' }, saved, '2', { repository })
  expect(updated.area).toBe('31')
})

it('expires codes after five minutes and after five incorrect attempts', async () => {
  const challenge = await repository.sendCode('13800000000')
  expect(challenge.mockCode).toBe('123456')
  for (let index = 0; index < 5; index += 1)
    await expect(repository.verifyCode('13800000000', '000000')).rejects.toThrow()
  await expect(repository.verifyCode('13800000000', '123456')).rejects.toThrow('验证码已失效')
})
```

Add these concrete Repository/Store assertions:

```ts
await repository.save(savePayload)
await expect(repository.save(savePayload)).rejects.toThrow('填报记录已存在')

const challenge = await repository.sendCode('13800000000')
await expect(repository.sendCode('13800000000')).rejects.toThrow('请稍后再试')
await repository.verifyCode('13800000000', challenge.mockCode)
await expect(repository.verifyCode('13800000000', challenge.mockCode)).rejects.toThrow('验证码已失效')

repository.setDraft(creditcode, { form, currentStep: 3, savedAt: 1_000 })
expect(repository.getDraft(creditcode)).toMatchObject({ currentStep: 3 })

store.initialize(enterprise, undefined, { form, currentStep: 3, savedAt: 1_000 })
expect(store.currentStep.value).toBe(3)
store.patch({ area: '31' })
expect(store.isDirty.value).toBe(true)
```

In the Query test, execute the mutation and assert `queryClient.getQueryData(landDemandKeys.detail(creditcode))` equals the returned record.

- [ ] **Step 2: Run RED**

Run: `pnpm test tests/unit/features/land-demand-repository.test.ts tests/unit/features/land-demand-service.test.ts tests/unit/stores/land-demand.test.ts`

Expected: FAIL because repository/service/store modules are missing.

- [ ] **Step 3: Implement Repository, Service, Query and Store**

Use storage keys exactly as specified in the design. Clone all returned records. Generate `updatetime` from `now()` and `updateuser` from the logged-in username passed by the Service. Query keys are:

```ts
export const landDemandKeys = {
  all: ['land-demand'] as const,
  detail: (creditcode: string) => [...landDemandKeys.all, 'detail', creditcode] as const,
}
```

The save/update mutations update the exact detail cache. The Store never duplicates the persisted Query record; it owns only the editable form snapshot and local draft metadata.

- [ ] **Step 4: Run GREEN**

Run:

```text
pnpm test tests/unit/features/land-demand-repository.test.ts tests/unit/features/land-demand-service.test.ts tests/unit/stores/land-demand.test.ts
pnpm typecheck
```

Expected: PASS and exit 0.

- [ ] **Step 5: Commit**

```text
git add src/features/land-demand src/stores/land-demand.ts tests/unit
git commit -m "feat: add mock land demand workflow"
```

## Task 6：实现登录页、首页和产品导航

**Files:**
- Rewrite: `src/pages/login/index.vue`
- Rewrite: `src/pages/home/index.vue`
- Modify: `src/components/ui/page-shell/index.vue`
- Modify: `src/router/route-meta.ts`
- Modify: `src/router/navigation.ts`
- Create: `src/pages/land-demand/index.vue`
- Create: `src/pages/land-demand/success.vue`
- Rewrite: `tests/unit/router/route-meta.test.ts`
- Create: `tests/unit/features/product-navigation.test.ts`

**Interfaces:**
- `/pages/login/index` is public.
- `/pages/home/index` is authenticated and is the post-login landing page.
- `/pages/land-demand/index` and `/pages/land-demand/success` are authenticated non-tab routes.
- Stable test IDs: `username`, `password`, `login-submit`, `land-demand-status`, `land-demand-primary`, `logout`.

- [ ] **Step 1: Write failing route and source-contract tests**

```ts
it('protects every product page except login', () => {
  expect(resolveRouteMeta('/pages/login/index')?.auth).not.toBe(true)
  expect(resolveRouteMeta('/pages/home/index')?.auth).toBe(true)
  expect(resolveRouteMeta('/pages/land-demand/index')?.auth).toBe(true)
  expect(resolveRouteMeta('/pages/land-demand/success')?.auth).toBe(true)
})

it('exposes stable login and home automation hooks', () => {
  expect(readFileSync('src/pages/login/index.vue', 'utf8')).toContain('data-testid="login-submit"')
  expect(readFileSync('src/pages/home/index.vue', 'utf8')).toContain('data-testid="land-demand-primary"')
})
```

- [ ] **Step 2: Run RED**

Run: `pnpm test tests/unit/router/route-meta.test.ts tests/unit/features/product-navigation.test.ts`

Expected: FAIL because routes and test IDs do not yet match the product.

- [ ] **Step 3: Implement pages with TDesign and typed navigation**

Login uses `t-input`, `t-button`, field errors and `useLoginMutation`. Home reads the authenticated enterprise and `useLandDemandQuery(creditcode)`, then renders exactly one primary state action:

```ts
const primaryLabel = computed(() => {
  if (!record.value) return '开始填报'
  return record.value.landusedemand === '1' ? '查看填报' : '继续填写'
})
```

Use `navigate`/`replace`; do not call raw navigation. Add the two new page files as temporary minimal route targets before `pnpm prepare`; their full UI is implemented in Tasks 7 and 8.

- [ ] **Step 4: Run GREEN and route generation**

Run:

```text
pnpm prepare
pnpm test tests/unit/router/route-meta.test.ts tests/unit/features/product-navigation.test.ts
pnpm typecheck
pnpm build
```

Expected: routes generated, tests PASS, build exits 0.

- [ ] **Step 5: Commit**

```text
git add src/pages src/components/ui/page-shell src/router tests/unit
git commit -m "feat: add land demand entry flow"
```

## Task 7：实现前四步向导组件和编辑页

**Files:**
- Create: `src/features/land-demand/components/wizard-progress.vue`
- Create: `src/features/land-demand/components/basic-info-step.vue`
- Create: `src/features/land-demand/components/land-info-step.vue`
- Create: `src/features/land-demand/components/project-info-step.vue`
- Create: `src/features/land-demand/components/finance-contact-step.vue`
- Create: `src/features/land-demand/components/wizard-actions.vue`
- Rewrite: `src/pages/land-demand/index.vue`
- Create: `tests/unit/components/land-demand-wizard.test.ts`
- Create: `tests/unit/features/land-demand-step-controller.test.ts`

**Interfaces:**

Each step receives:

```ts
defineProps<{ form: LandDemandForm; errors: readonly FieldError[] }>()
const emit = defineEmits<{ change: [patch: Partial<LandDemandForm>] }>()
```

The page owns the Store form and passes a fresh snapshot. Child components never mutate props. `WizardActions` emits `previous`, `save`, and `next`.

Controller helpers:

```ts
export function previousStep(step: 1 | 2 | 3 | 4 | 5): 1 | 2 | 3 | 4 | 5
export function nextStep(step: 1 | 2 | 3 | 4 | 5): 1 | 2 | 3 | 4 | 5
export function resolveSubmissionTarget(errors: readonly FieldError[]): 1 | 2 | 3 | 4 | undefined
```

- [ ] **Step 1: Write failing component-contract and controller tests**

```ts
it('keeps height and weight outside special-use conditional markup', () => {
  const source = readFileSync('src/features/land-demand/components/land-info-step.vue', 'utf8')
  expect(source).toMatch(/data-testid="deploy-height"/)
  expect(source).toMatch(/data-testid="deploy-weight"/)
  expect(source).not.toMatch(/v-if="[^\"]*is_specialuse[^\"]*"[^]*data-testid="deploy-height"/)
})

it('navigates to the first step containing a submission error', () => {
  const result = resolveSubmissionTarget([{ field: 'pred_tax', step: 3, message: '必填' }])
  expect(result).toBe(3)
})
```

The source-contract test checks the exact hooks:

```ts
const sources = stepFiles.map(file => readFileSync(file, 'utf8')).join('\n')
for (const id of [
  'area', 'building-area', 'expect-park', 'expect-time', 'is-deploy', 'deploy-park',
  'is-specialuse', 'deploy-landtype', 'deploy-height', 'deploy-weight', 'investment',
  'project-hydm', 'keyindustry', 'futureindustry', 'pred-ys', 'pred-tax', 'pred-rdex',
  'pred-unitenergy', 'projectdata', 'is-financing', 'financing-money', 'financing-time',
  'contact', 'office', 'phone',
]) expect(sources).toContain(`data-testid="${id}"`)
expect(sources).toMatch(/<t-(input|radio-group|checkbox-group|cascader|picker)/)
expect(previousStep(1)).toBe(1)
expect(nextStep(4)).toBe(5)
```

- [ ] **Step 2: Run RED**

Run: `pnpm test tests/unit/components/land-demand-wizard.test.ts tests/unit/features/land-demand-step-controller.test.ts`

Expected: FAIL because components and controller helpers are missing.

- [ ] **Step 3: Implement progress, steps and sticky actions**

Use TDesign inputs/radio/checkbox/cascader/picker components with native `<view>`, `<text>`, and `<scroll-view>` structure. Implement event value readers that consume `event.detail`. For each change emit a partial patch, for example:

```ts
function changeSpecialUse(event: unknown): void {
  emit('change', { is_specialuse: readStringDetail(event) as YesNo })
}
```

The page applies pure domain transitions before patching the Store, opens TDesign Dialog before destructive clearing, persists a local draft on step changes, and only calls the backend-equivalent mutation on explicit暂存.

- [ ] **Step 4: Run GREEN and component build checks**

Run:

```text
pnpm test tests/unit/components/land-demand-wizard.test.ts tests/unit/features/land-demand-step-controller.test.ts
pnpm prepare
pnpm typecheck
pnpm stylelint
pnpm build
```

Expected: PASS; generated component declarations include used TDesign components.

- [ ] **Step 5: Commit**

```text
git add src/features/land-demand src/pages/land-demand tests/unit/components tests/unit/features
git commit -m "feat: add land demand form wizard"
```

## Task 8：实现预览、验证码提交和成功页

**Files:**
- Create: `src/features/land-demand/components/review-step.vue`
- Create: `src/features/land-demand/components/verification-dialog.vue`
- Modify: `src/pages/land-demand/index.vue`
- Rewrite: `src/pages/land-demand/success.vue`
- Create: `tests/unit/components/land-demand-review.test.ts`
- Create: `tests/unit/features/land-demand-submit.test.ts`

**Interfaces:**

```ts
export interface SubmitControllerDeps {
  sendCode(phone: string): Promise<VerificationChallenge>
  verifyCode(phone: string, code: string): Promise<void>
  persist(status: '1'): Promise<LandDemandRecord>
}
export function createSubmitController(deps: SubmitControllerDeps): {
  requestCode(form: LandDemandForm, accepted: boolean): Promise<{ errors: FieldError[]; challenge?: VerificationChallenge }>
  submitCode(phone: string, code: string): Promise<LandDemandRecord>
}
```

Stable test IDs: `review-accept`, `review-submit`, `verification-code`, `verification-submit`, `mock-code`, `submit-success`.

Review presentation interface:

```ts
export interface ReviewItem { field: keyof LandDemandForm; label: string; value: string }
export interface ReviewGroup { step: 1 | 2 | 3 | 4; title: string; items: ReviewItem[] }
export function buildReviewGroups(form: LandDemandForm): ReviewGroup[]
```

- [ ] **Step 1: Write failing review and submission tests**

```ts
it('does not request a code before form and promise validation pass', async () => {
  const sendCode = vi.fn()
  const controller = createSubmitController({ sendCode, verifyCode: vi.fn(), persist: vi.fn() })
  const result = await controller.requestCode(invalidForm, false)
  expect(result.errors.length).toBeGreaterThan(0)
  expect(sendCode).not.toHaveBeenCalled()
})

it('verifies once before persisting status 1', async () => {
  const events: string[] = []
  const controller = createSubmitController({
    sendCode: async () => challenge,
    verifyCode: async () => { events.push('verify') },
    persist: async () => { events.push('persist'); return submittedRecord },
  })
  await controller.submitCode(validForm.phone, '123456')
  expect(events).toEqual(['verify', 'persist'])
})
```

Add these review assertions:

```ts
expect(buildReviewGroups({ ...validForm, is_deploy: '否', deploy_park: [] }).flatMap(group => group.items)
  .some(item => item.field === 'deploy_park')).toBe(false)
expect(buildReviewGroups({ ...validForm, is_financing: '没有' }).flatMap(group => group.items)
  .some(item => item.field === 'financing_money')).toBe(false)
expect(buildReviewGroups({ ...validForm, project_hydm: '1811' }).flatMap(group => group.items)
  .find(item => item.field === 'project_hydm')?.value).toBe('运动机织服装制造（1811）')
```

- [ ] **Step 2: Run RED**

Run: `pnpm test tests/unit/components/land-demand-review.test.ts tests/unit/features/land-demand-submit.test.ts`

Expected: FAIL because review and submit controller are absent.

- [ ] **Step 3: Implement preview, verification and result flow**

Review groups mirror the four steps and emit `edit(step)`. The code Dialog displays `challenge.mockCode` under a “Mock 测试验证码” label, locks while verifying, and keeps the form on errors. On success: update Query cache, clear local draft, mark Store persisted, and `replace('/pages/land-demand/success')`.

- [ ] **Step 4: Run GREEN**

Run:

```text
pnpm test tests/unit/components/land-demand-review.test.ts tests/unit/features/land-demand-submit.test.ts
pnpm typecheck
pnpm stylelint
pnpm build
```

Expected: PASS and exit 0.

- [ ] **Step 5: Commit**

```text
git add src/features/land-demand src/pages/land-demand tests/unit
git commit -m "feat: add verified land demand submission"
```

## Task 9：添加 Playwright-Automator E2E 和运行时截图

**Files:**
- Create: `playwright.config.ts`
- Create: `e2e/support/mini-program-driver.ts`
- Create: `e2e/fixtures/mini-program.ts`
- Create: `e2e/land-demand.spec.ts`
- Modify: `.gitignore`
- Create: `.screenshots/baseline/login.png`
- Create: `.screenshots/baseline/land-demand-review.png`

**Interfaces:**

```ts
export interface MiniProgramLocator {
  tap(): Promise<void>
  fill(value: string): Promise<void>
  text(): Promise<string>
  expectVisible(): Promise<void>
}
export interface MiniProgramDriver {
  relaunch(path: string): Promise<void>
  getByTestId(id: string): MiniProgramLocator
  expectPath(path: string): Promise<void>
  screenshot(path: string): Promise<void>
  clearStorage(): Promise<void>
}
```

- [ ] **Step 1: Write the E2E scenarios against the desired driver API**

```ts
test('logs in, saves a draft and restores it', async ({ miniProgram }) => {
  await miniProgram.clearStorage()
  await miniProgram.relaunch('/pages/login/index')
  await miniProgram.getByTestId('username').fill('demo')
  await miniProgram.getByTestId('password').fill('demo123')
  await miniProgram.getByTestId('login-submit').tap()
  await miniProgram.expectPath('/pages/home/index')
  await miniProgram.getByTestId('land-demand-primary').tap()
  await miniProgram.getByTestId('next-step').tap()
  await miniProgram.getByTestId('area').fill('30')
  await miniProgram.getByTestId('save-draft').tap()
  await miniProgram.relaunch('/pages/home/index')
  await miniProgram.getByTestId('land-demand-primary').tap()
  expect(await miniProgram.getByTestId('area').text()).toContain('30')
})
```

The E2E file also contains these explicit scenario assertions:

```ts
test('keeps height while changing other-land acceptance', async ({ miniProgram }) => {
  await miniProgram.getByTestId('deploy-height').fill('8')
  await miniProgram.getByTestId('is-specialuse-no').tap()
  expect(await miniProgram.getByTestId('deploy-height').text()).toContain('8')
})

test('requires financing details only when financing is 有', async ({ miniProgram }) => {
  await miniProgram.getByTestId('is-financing-yes').tap()
  await miniProgram.getByTestId('next-step').tap()
  await miniProgram.getByTestId('review-submit').tap()
  await miniProgram.getByTestId('financing-money-error').expectVisible()
  await miniProgram.getByTestId('financing-time-error').expectVisible()
})

test('submits with the mock code and reopens the existing record', async ({ miniProgram }) => {
  await miniProgram.getByTestId('review-accept').tap()
  await miniProgram.getByTestId('review-submit').tap()
  expect(await miniProgram.getByTestId('mock-code').text()).toContain('123456')
  await miniProgram.getByTestId('verification-code').fill('123456')
  await miniProgram.getByTestId('verification-submit').tap()
  await miniProgram.expectPath('/pages/land-demand/success')
  await miniProgram.getByTestId('back-home').tap()
  expect(await miniProgram.getByTestId('land-demand-status').text()).toContain('已提交')
  await miniProgram.getByTestId('land-demand-primary').tap()
  expect(await miniProgram.getByTestId('area').text()).not.toBe('')
})
```

The park scenario selects `330203` then `330200` and asserts only “宁波市” remains. The national-industry scenario selects parent `181`, leaf `1811`, submits a draft, relaunches, and asserts the displayed value is `运动机织服装制造（1811）`. The track scenario changes from “智能机器人/具身大模型（大脑与小脑）” to “生物医药” and asserts the direction field is empty.

- [ ] **Step 2: Run E2E and confirm RED**

Run: `pnpm build && pnpm test:e2e`

Expected: FAIL because the driver/fixture is not implemented; if DevTools is not logged in, record the exact `re-login` failure and restore login before judging product behavior.

- [ ] **Step 3: Implement the Playwright fixture and automator mapping**

Configure `workers: 1`, `fullyParallel: false`, a 60-second test timeout, and one `weapp` project. Use `withMiniProgram({ projectPath: 'dist', preferOpenedSession: true, trustProject: true })`; map `[data-testid="..."]` to `MiniProgramPage.$`, `input(value)`, `tap()`, `text()`, page path and `evaluate` storage reset. Close the shared session in fixture teardown.

Change the screenshot ignore rules to retain only baselines:

```gitignore
!.screenshots/
.screenshots/*
!.screenshots/baseline/
!.screenshots/baseline/*.png
```

- [ ] **Step 4: Run runtime E2E and native screenshot acceptance**

Prerequisites: WeChat DevTools logged in and service port enabled.

Run:

```text
pnpm build
pnpm test:e2e
wv screenshot --project ./dist --page pages/login/index --output .tmp/login.png --json
wv compare --project ./dist --page pages/login/index --baseline .screenshots/baseline/login.png --diff-output .tmp/login.diff.png --max-diff-pixels 100 --json
```

Expected: all E2E scenarios PASS; screenshot exists; compare stays within the recorded threshold. If an environment prerequisite remains unavailable, do not commit generated baselines or report E2E success.

- [ ] **Step 5: Commit**

```text
git add playwright.config.ts e2e .gitignore .screenshots/baseline package.json pnpm-lock.yaml
git commit -m "test: add land demand runtime e2e"
```

## Task 10：迁移全部文档、CI 并执行最终验证

**Files:**
- Rewrite: `README.md`
- Rewrite: `AGENTS.md`
- Rewrite: `docs/architecture.md`
- Rewrite: `docs/routing.md`
- Rewrite: `docs/http-client.md`
- Rewrite: `docs/query-state.md`
- Rewrite: `docs/ui-guidelines.md`
- Rewrite: `docs/testing.md`
- Rewrite: `docs/agent-workflow.md`
- Modify: `.github/workflows/verify.yml`
- Modify: `reports/verification.md`
- Delete: `.env.example` if no product setting remains

**Interfaces:**
- README documents Mock login, five steps, scripts and DevTools prerequisites.
- `docs/http-client.md` becomes Mock Service/Repository and real-backend replacement guidance.
- CI runs install, prepare, typecheck, lint, stylelint, coverage, build and budget; runtime E2E stays a documented DevTools job because hosted Linux CI lacks WeChat DevTools.

- [ ] **Step 1: Write a failing documentation consistency test**

Add to `tests/smoke/product-shape.test.ts`:

```ts
const docs = [
  'docs/architecture.md',
  'docs/routing.md',
  'docs/http-client.md',
  'docs/query-state.md',
  'docs/ui-guidelines.md',
  'docs/testing.md',
  'docs/agent-workflow.md',
]

it('documents only the land demand product', () => {
  for (const file of ['README.md', ...docs]) {
    const source = readFileSync(file, 'utf8')
    expect(source).toContain('用地需求')
    expect(source).not.toMatch(/订单取消|Hono 测试后端|demo order/i)
  }
})
```

- [ ] **Step 2: Run RED**

Run: `pnpm test tests/smoke/product-shape.test.ts`

Expected: FAIL because existing documentation describes the scaffold, Hono and orders.

- [ ] **Step 3: Rewrite documentation and CI**

Document the confirmed design without copying stale scaffold claims. Include the exact Mock account, field units, conditional rules, national-industry SQL extraction rule, storage keys, test layers, scripts, real-interface replacement boundary and current runtime prerequisite. Update `reports/verification.md` with command, date, exit code and runtime observations only after each command runs.

- [ ] **Step 4: Run GREEN documentation test**

Run: `pnpm test tests/smoke/product-shape.test.ts`

Expected: PASS.

- [ ] **Step 5: Run the complete verification gate**

Run each command separately and retain actual output:

```text
pnpm prepare
pnpm typecheck
pnpm lint
pnpm stylelint
pnpm test
pnpm test:coverage
pnpm build
pnpm analyze:budget
pnpm test:e2e
```

Then run `git diff --check` and inspect `git status --short`. Do not classify pre-existing CRLF/order warnings as product failures unless the changed files introduced them. A DevTools `re-login` or disabled service port must be reported as an unavailable runtime check, not as a passing E2E result.

- [ ] **Step 6: Commit**

```text
git add README.md AGENTS.md docs .github/workflows/verify.yml reports/verification.md tests/smoke/product-shape.test.ts
git commit -m "docs: align repository with land demand product"
```

## Appendix A：产业赛道与项目发展方向

Implement the following complete mapping. Keys and values are stored as names.

```text
化工新材料 -> 高端合成树脂（高端聚烯烃、工程塑料及特种工程塑料） | 高性能纤维及复合材料 | 特种橡胶和弹性体 | 功能化学品（电子化学品） | 其他
高端金属材料 -> 先进钢铁材料（高端特殊钢） | 铜合金材料 | 铝镁合金材料 | 钛合金材料 | 其他高端金属材料（超高纯金属、高品质高温合金） | 其他
磁性材料 -> 稀土金属 | 稀土永磁材料（钕铁硼磁体、钐钴磁体、铈磁体） | 软磁材料 | 其他
新能源及智能汽车 -> 新能源汽车 | 汽车零部件（新一代动力电池、智能底盘、核心动力系统） | 自动驾驶大模型 | 其他
关键基础件 -> 精密模具 | 精密轴承 | 伺服电机 | 高端气动件 | 其他
工业母机 -> 智能数控机床 | 高端等材装备 | 增材制造装备 | 高端功能部件（精密丝杠、高端导轨、数控系统） | 其他
安全应急装备 -> 应急救援装备（面对极端复杂场景需求的新型装备） | 应急防护装备（个体防护装备） | 应急监测预警装备（应急通信保障装备） | 其他
智能家电 -> 智能厨房家电（智能集成厨具、高端食材存储电器、净水电器） | 智能清洁家电（全屋清洁、织物清洁、宠物清洁家电） | 智能环境家电（空气净化设备、温湿调节设备） | 智能个护小家电（健康美容、人体护理家电） | 其他
现代纺织与服装 -> 时尚服装服饰（职业装、高端商务定制服饰、高性能功能运动装） | 高性能高附加值纤维和面料（生物基及废旧循环低碳纤维、多场景安全防护功能复合面料、高支高密轻奢贴身基底面料） | 高端家纺品（数字化织造高端棉制家纺套件、绿色温控填充类功能寝具、再生纤维复合功能家纺产品） | 其他
时尚文创 -> 智能办公系统集成（智能办公设备、AI文具） | 休闲运动装备（时尚露营装备、智能健身器材、高端垂钓用具） | 文创及潮玩（桌游、盲盒、手办） | 历史经典（青瓷、木雕、竹根雕） | 其他
半导体与集成电路 -> 先进半导体材料（关键电子材料、宽禁带半导体、石墨材料） | 高端制造与先进封测（集成电路先进制造、集成电路先进封装、微机电系统） | 集成电路专用装备（制造装备与封测装备） | 其他
新型光电显示 -> 新型光电显示材料（光学膜材料、发光材料） | 新型光电显示器件（光电传感、光芯片、光互连器件） | 新型光电显示终端（AR/VR终端、车载显示） | 其他
智能传感与仪器仪表 -> 高精密智能传感器（力传感器、MEMS传感器、视觉传感器） | 高精密仪器仪表 | 其他
新型储能 -> 新型电化学储能系统（钠离子电池） | 其他
下一代风光电 -> 新型光伏（叠层光伏产品） | 新型风电（深远海风电机组） | 其他
人工智能与高端软件 -> 通用人工智能（算力服务（芯片）、数据服务（数据语料、数据安全工具）、算法模型（垂类模型、智能体、世界模型、开源模型、开发工具）、智能终端） | 基础软件和工业软件（研发设计与仿真软件（几何内核、求解器、云化计算机辅助设计、计算机辅助工程）、生产制造与运营管理软件（智能软件）、工业基础软件（操作系统）） | 其他
智能机器人 -> 整机研发及制造（工业机器人、特种机器人、服务机器人、人形机器人） | 关键核心零部件（执行器和控制器、智能传感器和新型材料、机器肢和灵巧手） | 具身大模型（大脑与小脑） | 其他
生物医药 -> 创新药（新型抗体药物、类器官、细胞和基因治疗、核药） | 高端医疗器械（新一代医学影像诊断装备、监护与生命支持、微创手术器材、人工心脏瓣膜） | 医药外包服务 | 其他
航空航天 -> 航空装备（中小型航空发动机、大飞机发动机零部件） | 航天装备（火箭发动机、临近空间装备、先进地面终端） | 其他
高技术船舶与海工装备 -> 高技术船舶（液化天然气船、极地运输船舶、高端油化船） | 高端海工装备（通信海缆） | 其他
低空装备 -> 无人机（行业级无人机） | eVTOL（电动垂直起降航空器） | 其他
其他 -> 其他
```

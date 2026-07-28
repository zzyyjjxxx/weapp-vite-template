# 企业用地需求填报小程序设计

## 1. 目标与范围

本项目基于现有 weapp-vite + Wevu 脚手架，改造为企业用地需求填报小程序。应用保留企业账号登录，通过五步向导完成填报、暂存、验证码提交、已提交记录修改和草稿恢复。

所有业务接口均由小程序端 Mock Service 与持久化 Repository 提供。页面和组件不直接读取 Mock 数据或小程序 Storage，未来接入真实后端时只替换 Service/Repository 实现。

项目使用：

- Wevu Vue SFC 编写页面和组件。
- TDesign MiniProgram 提供表单、选择器、Dialog、Toast 和 Loading。
- Tailwind CSS 作为直接开发依赖，通过 weapp-tailwindcss 在构建期转换，不引入浏览器运行时。
- 小程序原生节点提供页面结构和必要的输入事件边界。
- TanStack Query Core 管理 Mock 服务端状态。
- Wevu Store 管理登录会话和当前编辑草稿等客户端状态。

现有订单、个人中心、Hono 后端及其示例测试不属于目标产品，将在对应实施阶段删除。

## 2. 技术路线

采用前端 Mock Repository 方案：

```text
页面与步骤组件
    -> LandDemand Store
    -> Query/Mutation
    -> Mock Service
    -> 小程序 Storage Mock Repository
```

各层职责如下：

- 页面容器：组合查询状态、编辑状态、步骤导航和提交行为。
- 步骤组件：接收只读表单快照，渲染字段并通过事件上报变更。
- Store：保存当前步骤、本地编辑草稿、未保存状态和登录会话。
- Query/Mutation：查询填报记录，执行暂存、提交和验证码操作。
- Mock Service：模拟真实接口语义、业务状态和错误。
- Repository：读写小程序 Storage，并提供可替换的内存适配器用于测试。

页面不得调用 `fetch`、`wx.request`、`wpi` 或直接访问 Repository。

## 3. 路由与页面

主路由为：

```text
/pages/login/index
/pages/home/index
/pages/land-demand/index
/pages/land-demand/success
```

### 3.1 登录页

登录页使用 Mock 企业账号。登录成功后持久化会话并跳转首页。默认演示账号为：

```text
账号：demo
密码：demo123
企业：宁波示范智造有限公司
信用代码：91330200MA2DEMO001
```

### 3.2 首页

首页显示企业名称、信用代码和填报状态：

- 查询不到记录：未填报，主按钮为“开始填报”。
- `landusedemand="2"`：草稿，主按钮为“继续填写”。
- `landusedemand="1"`：已提交，提供“查看详情”和“修改填报”。

首页同时提供退出登录。退出仅清理当前会话，不删除企业填报记录和本地草稿。

### 3.3 五步填报页

五步在同一个页面容器内切换，避免创建复杂页面栈：

1. 基本信息。
2. 用地需求。
3. 投资项目。
4. 融资及联系人。
5. 信息确认与提交。

底部操作为：

```text
第一步：暂存 | 下一步
第二至四步：上一步 | 暂存 | 下一步
第五步：返回修改 | 暂存 | 提交
```

### 3.4 成功页

提交成功页显示企业名称、提交时间和已提交状态，并提供返回首页和查看填报信息入口。

## 4. 表单步骤与字段

### 4.1 第一步：基本信息

以下字段由登录企业资料自动带入并只读：

- `businessname`：企业名称。
- `creditcode`：信用代码。
- `county`：所属区县。
- `region`：所属乡镇。

企业资料有误时提示联系管理员维护，不在本表单修改企业主体信息。

### 4.2 第二步：用地需求

字段包括：

- `area`：用地面积，必填，单位为亩。
- `building_area`：项目建筑面积，必填，单位为平方米。
- `expect_park`：期望获得土地位置，必填，单选。
- `expect_time`：期望拿到土地时间，必填，年月选择。
- `is_deploy`：是否接受跨区域调配，必填。
- `deploy_park`：期望调配区域，条件必填，多选，显示名称并保存 ID。
- `is_specialuse`：是否接受其他用地形式，必填。
- `deploy_landtype`：期望用地形式，条件必填，单选并保存名称。
- `deploy_height`：期望层高，选填，单位为米。
- `deploy_weight`：期望承重，选填，单位以 PC 端口径为准。

动态规则：

- `is_deploy=是` 时显示 `deploy_park`，正式提交时必填。
- `is_deploy=否` 时隐藏并在用户确认后清空 `deploy_park`。
- `is_specialuse=是` 时显示 `deploy_landtype`，正式提交时必填。
- `is_specialuse=否` 时隐藏并在用户确认后清空 `deploy_landtype`。
- `deploy_height` 和 `deploy_weight` 始终显示，不受 `is_specialuse` 控制，也不是必填字段。

调配区域字典：

| ID | 名称 |
| --- | --- |
| 330200 | 宁波市 |
| 330203 | 海曙区 |
| 330205 | 江北区 |
| 330206 | 北仑区 |
| 330211 | 镇海区 |
| 330212 | 鄞州区 |
| 330213 | 奉化区 |
| 330225 | 象山县 |
| 330226 | 宁海县 |
| 330262 | 高新区 |
| 330281 | 余姚市 |
| 330282 | 慈溪市 |
| 3302821 | 前湾新区 |

“宁波市”表示全市均可，与任何具体区县互斥。

期望用地形式为单选：

- 小微园。
- 租售型闲置空间。
- 租售型标准厂房。
- 以上皆可。

### 4.3 第三步：投资项目

字段包括：

- `investment`：固定资产投资额，必填。
- `project_hydm`：项目所属国民行业，必填，保存行业编码。
- `keyindustry`：项目所属产业赛道，必填，保存名称。
- `futureindustry`：项目发展方向，必填，保存名称。
- `pred_ys`：项目预计营收，必填。
- `pred_tax`：项目预计税收，必填。
- `pred_rdex`：项目预计研发费用，选填。
- `pred_unitenergy`：项目单位能耗增加值，选填。
- `projectdata`：项目建设内容，必填，最长 255 个字符。

产业赛道变化时必须清空 `futureindustry`，再按新的赛道名称加载发展方向。产业赛道为“其他”时，发展方向自动设置为“其他”。产业赛道和发展方向使用已确认的完整静态字典。

### 4.4 国民行业字典

原始数据来自开发环境文件：

```text
C:\Users\18556\Desktop\ydxq小程序\m_industryinfo.sql
```

SQL 表结构为：

```text
id
industryCode
industryName
pid
```

构建仓库内静态字典时，只保留满足以下条件的记录：

```text
pid 为纯数字
Number(pid) >= 181
Number(pid) <= 439
```

当前 SQL 共 1964 条记录，筛选结果为 515 条子行业，覆盖 150 个父节点。

行业选择器使用两级级联：

- 父节点值使用子项的 `pid`。
- 父节点名称通过查找 `industryCode === pid` 的记录获得。
- 第二级显示属于该 `pid` 的具体行业。
- 父节点只用于分组，不能作为最终提交值。
- 子项显示为 `industryName（industryCode）`。
- 表单和 Mock 接口只保存最终子项的 `industryCode`。

例如：

```text
父节点：机织服装制造
子项：运动机织服装制造（1811）
保存值：1811
```

小程序运行时只使用生成后的类型化静态字典，不读取 SQL 或桌面文件。

### 4.5 第四步：融资及联系人

字段包括：

- `is_financing`：是否有融资需求，必填；新建表单默认“没有”。
- `financing_money`：融资金额，有融资需求时必填。
- `financing_time`：期望融资时间，有融资需求时必填。
- `contact`：法人姓名，必填。
- `office`：联系人职务，选填。
- `phone`：法人手机号，必填，用于提交验证码。

动态规则：

- `is_financing=没有` 时隐藏融资金额和期望时间，两项不必填。
- `is_financing=有` 时显示两项，正式提交时均必填。
- 从“有”切换为“没有”时，确认后清空融资金额和期望时间。

### 4.6 第五步：确认与提交

按前四步分组预览。条件字段无效时不展示，例如无融资需求时不展示融资金额和期望时间。每组提供返回对应步骤的修改入口。

正式提交前必须勾选：

> 本企业承诺所填写的信息真实、准确、完整，并同意相关部门根据项目服务需要使用以上信息。

## 5. 状态、暂存与提交

### 5.1 初始化

使用当前企业 `creditcode` 查询 `landusedemand_info` 的 Mock 等价接口：

```text
无记录 -> hasRecord=false -> 新增模式
有记录 -> hasRecord=true -> 回显并进入修改模式
```

第一次新增成功后立即设置 `hasRecord=true` 并刷新查询缓存，防止后续暂存重复新增。

### 5.2 暂存

暂存设置 `landusedemand="2"`，不发送验证码，不要求完成全部必填项，只校验已填写字段的格式。保存失败时保留当前表单和编辑缓存。

### 5.3 提交

提交执行：

1. 全部基础必填校验。
2. 条件必填校验。
3. 真实性承诺校验。
4. 向法人手机号发送六位验证码。
5. 校验验证码。
6. 设置 `landusedemand="1"`。
7. 调用新增或修改 Mock 命令。
8. 清理本地编辑缓存并进入成功页。

已提交记录只有在用户再次暂存或提交后才改变持久化状态。仅进入编辑页不改变记录状态。

## 6. Mock Service 与 Repository

业务接口为：

```text
login(input)
getCurrentEnterprise()
getLandDemandInfo(creditcode)
saveLandDemand(payload)
updateLandDemand(payload)
sendVerificationCode(phone)
verifyVerificationCode(phone, code)
getIndustryOptions()
getParkOptions()
```

Storage 键为：

```text
mock:enterprises
mock:land-demand:{creditcode}
draft:land-demand:{creditcode}
mock:verification:{phone}
auth:session
```

Mock 服务生成 `updatetime` 和 `updateuser`。修改命令固定 `newproject="1"`，`industryCode` 保留原值或省略，隐藏字段不得用空字符串覆盖原值。接口允许注入延迟和确定性失败，以测试加载、错误和重试状态。

验证码规则为：

- 六位数字。
- 五分钟有效。
- 同一手机号 60 秒内不可重复发送。
- 连续输错五次后失效。
- 验证成功后立即失效。
- 测试环境可注入固定验证码 `123456`。
- 页面在 Mock 环境显示当前验证码提示，不依赖短信服务或控制台。

## 7. 表单模型与 Payload

前端只维护一个统一表单模型，新增和修改通过适配器生成不同 Payload。

新增命令包含企业名称、信用代码、所属区县和所属乡镇。修改命令以 `creditcode` 为条件，不更新企业主体信息。

数值规则：

- `building_area`、`deploy_height`、`deploy_weight` 最多两位小数。
- `investment`、`financing_money`、`pred_tax`、`pred_rdex`、`pred_ys`、`pred_unitenergy` 不超过后端 `decimal(20,6)` 精度；页面默认最多允许两位小数。
- 数值不得为负数。
- 非必填数字为空时保持空值，不自动转换为零。

日期、是否值和多选分隔格式由 Payload 适配器统一处理，页面不拼接接口字符串。

## 8. 组件边界

目标组件结构为：

```text
features/land-demand/
  models.ts
  defaults.ts
  validation.ts
  payload.ts
  service.ts
  queries.ts
  repository.ts
  dictionaries/
  components/
    wizard-progress.vue
    basic-info-step.vue
    land-info-step.vue
    project-info-step.vue
    finance-contact-step.vue
    review-step.vue
    verification-dialog.vue
    wizard-actions.vue
```

步骤组件不修改传入 `props`，而是维护必要的本地响应式快照，通过静态绑定的变更事件向页面容器同步新快照。跨组件边界的事件数据从小程序事件的 `event.detail` 读取。

关键交互节点具有稳定的 `data-testid`，供自动化适配层定位。

## 9. UI 与交互反馈

- 字段级错误显示在字段附近。
- 正式提交失败时定位到第一个错误所在步骤。
- 全局成功和失败反馈使用 TDesign Toast。
- 清空条件字段前使用 TDesign Dialog 确认。
- 查询和提交期间显示 Loading 并锁定重复操作。
- 选择器显示业务名称，表单模型保存接口值。
- 页面固定底部操作区须为安全区域预留空间。

## 10. 测试策略

### 10.1 Vitest

单元和集成测试覆盖：

- 默认值、暂存校验和正式提交校验。
- 三组条件字段的显示、清空和必填规则。
- 层高与承重始终显示且选填。
- “宁波市”与具体调配区域互斥。
- 产业赛道与发展方向联动。
- 国民行业 515 条结果、150 个父节点、范围、显示和保存规则。
- 新增与修改 Payload。
- 隐藏字段保值与空值语义。
- 验证码生命周期。
- Repository 持久化、失败注入和隔离。
- 登录会话、草稿恢复和状态转换。
- 路由、页面 JSON 和 TDesign 自动导入。
- 构建产物不包含已删除示例。

### 10.2 Playwright 与小程序 Automator

使用 `@playwright/test` 作为运行器，使用 `weapp-ide-cli` 导出的 `withMiniProgram`、`MiniProgramPage` 和 `MiniProgramElement` 建立适配层：

```text
Playwright fixture
    -> MiniProgramDriver
    -> weapp-ide-cli automator
    -> 微信开发者工具小程序运行时
```

E2E 设置 `workers: 1` 串行运行，覆盖：

- 登录与会话恢复。
- 新建五步填报。
- 暂存与草稿恢复。
- 跨区域调配规则。
- 其他用地形式单选规则。
- 层高和承重独立规则。
- 国民行业级联和编码保存。
- 产业赛道联动。
- 融资默认值与条件必填。
- 验证码提交。
- 已提交记录修改。
- 关键页面截图。

E2E 需要微信开发者工具已登录并开启服务端口。若环境不可用，必须报告实际错误，不能用构建或单元测试代替运行时验收。

### 10.3 验证命令

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
wv screenshot
wv compare
```

## 11. 删除与迁移范围

删除：

- 订单分包、订单领域代码及相关测试。
- 个人中心示例、TabBar 示例和无用图标。
- Hono 服务端、服务器脚本、配置、依赖和测试。
- 不再使用的 HTTP 示例代码和环境变量。

保留并按新领域调整：

- Query Core 适配层。
- 小程序 Storage 平台适配器。
- 类型化路由。
- 必要的通用加载、空状态和错误组件。
- Wevu Store 基础设施。

## 12. 文档与 CI

重写：

- `README.md`。
- `AGENTS.md`。
- `docs/architecture.md`。
- `docs/routing.md`。
- `docs/query-state.md`。
- `docs/ui-guidelines.md`。
- `docs/testing.md`。
- `docs/agent-workflow.md`。

将 `docs/http-client.md` 改为 Mock Service/Repository 与未来真实接口替换说明。文档必须描述用地需求产品、字段规则、Mock 账号、字典来源、测试方式和运行时验收，不再把项目描述为订单/Hono 示例脚手架。

CI 移除服务器构建和测试步骤，增加新的单元、构建和可在具备 DevTools 环境时执行的 E2E 入口。

## 13. 交付与提交策略

实施按可独立验证的阶段拆分。每个阶段先写失败测试，再实现最小代码，通过聚焦检查后单独提交。最终执行完整静态检查、构建、单元测试、覆盖率、包体预算和可用的 DevTools E2E/截图验收。

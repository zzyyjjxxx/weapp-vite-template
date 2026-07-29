# 企业用地需求填报小程序

基于 Weapp-Vite、Wevu、TDesign MiniProgram、构建期 TailwindCSS 和 TanStack Query Core 的企业用地需求填报小程序。当前所有业务接口均由本地 Mock Service/Repository 实现；数据写入微信小程序 Storage，仅用于开发和验收，不代表生产后端、真实短信或正式企业数据。

## 本地体验

- Mock 账号：`demo`
- Mock 密码：`demo123`
- 测试验证码：界面在发送后显示 Repository 生成的六位 Mock 验证码；默认运行配置为 `123456`

登录后依次完成五步：基本信息、用地需求、投资项目、融资及联系人、信息确认与提交。暂存写入状态 `2`，正式提交在完整校验、承诺确认和验证码校验通过后写入状态 `1`。

企业名称、信用代码、所属区县和所属乡镇始终来自当前认证企业并只读展示，本地草稿不能覆盖这些归属字段。已提交记录在首页提供彼此独立的“查看详情”和“修改填报”：详情模式只展示四组确认信息，不提供暂存、承诺、验证码或提交动作；提交成功页会重新读取当前企业的已提交 Query 记录后才显示成功状态、企业名称与提交时间。

## 已确认的填报规则

- `deploy_landtype` 是单选；仅当 `is_specialuse=是` 时显示并在正式提交时必填。
- 期望层高和期望承重始终显示、始终选填，不受 `is_specialuse` 控制。
- 新建或缺失融资选择时，`is_financing` 默认“没有”；只有选择“有”时才显示并要求融资金额、期望融资时间。
- 固定资产投资额、项目预计营收、预计税收、预计研发费用的单位均为万元且必填；项目单位能耗增加值单位为万元/吨标煤且必填。
- 项目建设内容按 `text` 语义处理，界面不限制字符数。
- 国民行业保存 `industryCode`，显示 `industryName（industryCode）`；字典仅包含数值 `pid` 为 `181..439` 的数据，共 150 个父节点、515 个叶子项。
- `decimal(20,6)` 的投资、融资及项目指标最多 14 位整数，`decimal(10,2)` 的建筑面积、层高和承重最多 8 位整数；界面统一最多输入 2 位小数，空值与明确填写 `0` 保持不同语义。

## 常用命令

```powershell
pnpm install --frozen-lockfile
pnpm dev
pnpm dev:open
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

`pnpm verify` 执行静态项目门禁。运行时 E2E 需要 Windows 上已登录的微信开发者工具、已启用服务端口以及可用的 `dist` 构建；托管 Linux CI 无法提供这些条件，因此不会把构建成功当成运行时通过。若开发者工具返回 `re-login`，应先在开发者工具中重新登录，再重试 E2E。实际验证结果见 [reports/verification.md](reports/verification.md)。

## 文档

- [架构](docs/architecture.md)
- [路由](docs/routing.md)
- [Mock Service/Repository](docs/http-client.md)
- [Query 与 Store 状态归属](docs/query-state.md)
- [界面规范](docs/ui-guidelines.md)
- [测试](docs/testing.md)
- [Agent 工作流](docs/agent-workflow.md)

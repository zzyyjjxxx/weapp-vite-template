# 用地需求填报 Agent 工作流

## 开始前

1. 查看 `git status --short --branch`，保留用户拥有的修改。
2. 阅读根目录及最近的 `AGENTS.md`/override、匹配 Skill 和相关 `docs/` 来源。
3. 命令行为不清楚时先读 `node_modules/weapp-vite/dist/docs/index.md`，再按需读包内 README/MCP 文档。
4. 核对 Weapp-Vite 自动路由、Wevu 运行时、TDesign 组件解析与用地需求领域边界。
5. 先写能观察到预期失败的聚焦测试，再实现最小变更。

## 实现约束

- 页面和步骤组件只依赖 Store、Query/Mutation 与类型化导航，不直接调用 Storage、Repository、`fetch`、`wx.request` 或原始导航。
- 已持久化用地需求属于 Query Core；认证与正在编辑的表单属于 Store。
- 认证和用地需求记录使用 `src/features/*/http-repository.ts` 访问 `http://localhost:17163/`；验证码和本地草稿保持 Mock，并明确 `demo / demo123` 只是离线测试账号。
- 每个阶段独立验证和提交，不提交用户文件、规划辅助目录、`.DS_Store` 或临时截图。
- 路由或生成配置变化后运行 `pnpm prepare`，不要手改 `.weapp-vite/`。

## 产品审查清单

- 五步顺序、状态 `1/2`、新增/修改判断正确。
- `is_specialuse` 只控制单选用地形式；层高与承重始终可见且选填。
- 投资额、营收、税收、研发费用和单位能耗增加值单位及必填规则一致；项目建设内容无 UI 字数限制。
- 国民行业保存编码、显示名称与编码，严格使用 `pid 181..439` 的 150/515 字典。
- 私有 Query 缓存、Storage 键和退出登录隔离正确。

## 验证与运行时证据

先运行聚焦测试，再依次执行 `pnpm prepare`、app/E2E 类型检查、`pnpm lint`、stylelint、全量测试、coverage、build 和 budget。产品 lint 必须零警告通过，覆盖所有维护中的应用/测试/E2E TypeScript 与 Vue、生成器和根配置；只排除由 SQL 生成且有精确字典测试保护的 `industries.generated.ts`。样式由 Stylelint 独立检查，不能以换行差异掩盖语义 lint 错误。

微信运行时通过 `pnpm test:e2e`、`wv screenshot` 和 `wv compare` 验收。运行前确认开发者工具已登录且服务端口启用；`re-login` 必须报告为受阻。托管 Linux CI 不运行 DevTools E2E，构建成功也不能当作运行时成功。

完成报告需列出提交、每条实际命令及退出码、测试数量、构建预算、DevTools 观察、截图/对比文件和尚未验证的风险，禁止伪造或沿用旧项目证据。

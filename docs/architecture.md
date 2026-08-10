# 用地需求填报架构

本仓库只包含企业用地需求填报小程序。认证和用地需求记录通过可替换的 HTTP Service/Repository 访问本地开发 API；验证码与本地草稿仍使用 Mock 和微信小程序 Storage，不包含生产凭据、真实短信或发布流程。

## 分层

- `src/pages/`：登录、首页、五步填报和提交成功页，只消费 Store、Query/Mutation 与类型化导航。
- `src/features/auth/`：企业登录模型、HTTP/Mock Repository、Service 和 Mutation。
- `src/features/land-demand/`：表单模型、默认值、显隐规则、校验、Payload 适配、HTTP/Mock Repository、Query/Mutation、字典与步骤组件。
- `src/shared/query/`：把 `@tanstack/query-core` observer 桥接为 Wevu 响应式状态。
- `src/stores/`：认证会话、应用就绪状态以及未持久化的填报编辑状态。
- `src/router/`：生成路由类型、鉴权元数据与导航封装。
- `src/platform/`：HTTP、Storage、网络状态等宿主适配器。

## 数据流

```text
页面/步骤组件
  -> Wevu Store + Query/Mutation
  -> Auth 或 LandDemand Service
  -> 可替换 Repository 接口
  -> HTTP Repository -> 本地开发 API
  -> 验证码/草稿 Mock -> 微信小程序 Storage
```

页面和组件不得直接访问 Storage、Mock Repository、`fetch`、`wx.request` 或原始导航 API。服务端状态由 Query Core 负责，认证和正在编辑的小型客户端状态由 Wevu Store 负责；已持久化的用地需求记录不能复制进 Store 形成第二份真相。

企业名称、信用代码、区县和乡镇属于认证身份边界：基本信息组件只读，Store 在恢复本地草稿和接收局部补丁后都重新写入当前认证企业值，更新 Payload 的查询键始终取当前表单中已受保护的信用代码。详情 Query 将 TanStack Query 提供的 `AbortSignal` 传到 Service，Service 在 Repository 调用前后检查取消状态。

## 五步与状态

固定步骤为：基本信息、用地需求、投资项目、融资及联系人、信息确认与提交。点击步骤切换会保存本地编辑草稿；只有明确点击“暂存”或验证码提交才调用 Repository。`landusedemand=2` 表示草稿，`landusedemand=1` 表示已提交。

正式提交需要完整字段校验、真实性承诺以及本地 Mock 六位验证码校验。新增记录调用 HTTP 保存语义，已有记录调用 HTTP 修改语义；首次保存后 Query 缓存立即拥有记录，避免重复新增。

已提交记录有独立只读详情模式，只复用确认分组，不创建另一份记录状态，也不显示承诺、验证码、暂存或保存动作。成功页同样从当前企业 Query 记录派生，仅 `landusedemand=1` 才展示成功信息。

## 字典来源

- 国民行业由外部提供的 `m_industryinfo.sql` 经 `scripts/generate-industry-dictionary.mjs` 生成；仅选择数值 `pid` 为 `181..439` 的记录，产出 150 个父节点和 515 个叶子项。保存 `industryCode`，显示 `industryName（industryCode）`。
- 调配区域与期望位置共用 13 项宁波区域映射，显示名称、保存 ID；`330200/宁波市` 与具体区县互斥。
- 产业赛道和发展方向使用业务确认的 22 组名称映射，二者均保存名称；切换赛道会清空原发展方向。
- 用地形式为四个名称的单选字典。

### 宁波区域映射

| 保存 ID | 显示名称 | 保存 ID | 显示名称 |
|---|---|---|---|
| 330200 | 宁波市 | 330203 | 海曙区 |
| 330205 | 江北区 | 330206 | 北仑区 |
| 330211 | 镇海区 | 330212 | 鄞州区 |
| 330213 | 奉化区 | 330225 | 象山县 |
| 330226 | 宁海县 | 330262 | 高新区 |
| 330281 | 余姚市 | 330282 | 慈溪市 |
| 3302821 | 前湾新区 |  |  |

### 产业赛道映射

22 个父名称为：化工新材料、高端金属材料、磁性材料、新能源及智能汽车、关键基础件、工业母机、安全应急装备、智能家电、现代纺织与服装、时尚文创、半导体与集成电路、新型光电显示、智能传感与仪器仪表、新型储能、下一代风光电、人工智能与高端软件、智能机器人、生物医药、航空航天、高技术船舶与海工装备、低空装备、其他。每个父名称对应的完整发展方向由 `src/features/land-demand/dictionaries/industry-tracks.ts` 导出，来源是业务方确认的 `pid_name/name` 表；页面显示和保存的都是名称。

## UI 技术边界

页面和组件使用 Wevu Vue SFC；运行时 API从 `wevu` 导入。交互组件优先使用已安装的 TDesign MiniProgram，布局可使用小程序原生节点与由 `weapp-tailwindcss` 转换的构建期 Tailwind 工具类。仓库没有浏览器 Tailwind 运行时。

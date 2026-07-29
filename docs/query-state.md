# 用地需求 Query 与 Store 状态

项目使用 `@tanstack/query-core`，由 `src/shared/query/use-query.ts` 和 `use-mutation.ts` 将 observer 结果桥接为 Wevu ref，并在页面卸载时清理观察者。

## 归属

- Query Core：Mock 登录 Mutation、按信用代码查询的已持久化用地需求记录、保存/修改、验证码发送/验证及其状态和错误。
- Auth Store：认证会话、企业资料和初始化状态；通过 `land-demand.auth` 版本化持久化。
- LandDemand Store：五步位置、正在编辑的表单、是否存在记录和脏状态；它不拥有已持久化记录。
- Repository 本地草稿：步骤切换时保存的可恢复编辑快照，键为 `draft:land-demand:{creditcode}`。

## Query 键与私有缓存

用地需求详情键为 `['land-demand', 'detail', creditcode]`。详情查询和保存/修改后写入的详情缓存都标记 `meta.scope='private'`。退出登录会删除私有 Query 数据，避免下一企业读到上一企业缓存；Repository 中按信用代码隔离的 Mock 记录不因此删除。

保存/修改 Mutation 成功后直接更新精确详情缓存。页面从 Query 记录初始化 Store；本地草稿存在时优先恢复草稿。明确暂存或提交成功后，Store 标记已持久化并删除本地草稿。

## 约束

- 不把 Query 记录复制到持久化 Store。
- 组件通过事件向页面发送局部表单补丁，不直接修改 Props。
- Mutation 默认不重试，避免重复保存或重复发送验证码。
- 领域 Query/Mutation 接受 Repository 注入，单元测试不依赖微信运行时。
- 修改 `src/shared/query` 时需保持一个 Hook 实例一个 observer、卸载清理和私有缓存语义。

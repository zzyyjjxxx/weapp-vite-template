# 用地需求填报路由

路由由 Weapp-Vite 从 `src/pages/` 自动发现。新增、删除或移动页面后运行 `pnpm prepare`，检查 `.weapp-vite/typed-router.d.ts`；不要手工修改生成文件。

## 当前主包路由

- `/pages/login/index`：公开的企业登录页，运行时通过 HTTP Auth Repository 登录。
- `/pages/home/index`：鉴权首页，展示用地需求状态与填报入口。
- `/pages/land-demand/index`：鉴权的五步填报页；`mode=view` 为已持久化记录的只读详情，`mode=edit` 或缺省为编辑模式。
- `/pages/land-demand/success`：鉴权的提交成功页；仅在 Query 返回当前企业的 `landusedemand=1` 记录后展示成功内容。
- `/pages/error/index`：公开错误兜底页。

项目没有业务分包或原生 `tabBar`。`src/app.vue` 将 `autoRoutes.pages` 写入生成的 `app.json`。

## 导航与鉴权

页面通过 `src/router/navigation.ts` 的 `navigate`、`replace` 或 `replaceUrl` 导航。业务页面不得调用 `wx.navigateTo`、`wx.redirectTo` 或其他原始导航 API。

`src/router/route-meta.ts` 将首页、填报页、成功页标记为鉴权路由。未登录访问受保护路由时，Router Guard 跳转到登录页并携带编码后的 `returnTo`；登录成功后恢复目标地址。登录页和错误页保持公开。

## Query 参数

使用 `encodeQuery` 生成参数，使用 `parseRequiredString`、`parseOptionalNumber`、`parseEnum` 解析外部输入。填报页通过 `parseLandDemandMode` 将不可信 `mode` 收敛为 `view|edit`，未知值默认编辑。当前用地需求主流程不依赖记录 ID Query：企业信用代码来自受信认证会话，并作为记录查询键和修改条件。

首页对已提交记录显示两个稳定入口：`land-demand-view` 使用 `{ mode: 'view' }` 打开只读确认分组，`land-demand-edit` 使用 `{ mode: 'edit' }` 进入可编辑五步流程。成功页“查看填报信息”同样使用类型化导航进入只读模式；直接访问成功页但不存在已提交记录时返回首页，不显示虚假成功。
## 冷启动与直接访问保护

`app.json` 的 `entryPagePath` 固定为 `pages/login/index`，且项目没有原生
`tabBar`。首页使用普通 `push/replace` 导航，不使用 `switchTab`。

Router Guard 在每次鉴权决策时调用 `ensureActiveSession()`，以当前时间检查
会话是否过期。首页、填报页和提交成功页还通过共享的 `useProtectedPage`
在 `onLoad` 与 `onShow` 再次校验，覆盖绕过 Router 的原生直接启动；未授权
页面在跳转登录前不渲染受保护内容。

填报页在每次暂存、发送验证码、验证码校验以及最终持久化之前都会重新执行
前台会话检查。会话过期时先清除私有缓存并跳转登录，不调用对应 Mutation。

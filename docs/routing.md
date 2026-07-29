# 用地需求填报路由

路由由 Weapp-Vite 从 `src/pages/` 自动发现。新增、删除或移动页面后运行 `pnpm prepare`，检查 `.weapp-vite/typed-router.d.ts`；不要手工修改生成文件。

## 当前主包路由

- `/pages/login/index`：公开的 Mock 企业登录页。
- `/pages/home/index`：鉴权首页，展示用地需求状态与填报入口。
- `/pages/land-demand/index`：鉴权的五步填报页。
- `/pages/land-demand/success`：鉴权的提交成功页。
- `/pages/error/index`：公开错误兜底页。

项目没有业务分包或原生 `tabBar`。`src/app.vue` 将 `autoRoutes.pages` 写入生成的 `app.json`。

## 导航与鉴权

页面通过 `src/router/navigation.ts` 的 `navigate`、`replace` 或 `replaceUrl` 导航。业务页面不得调用 `wx.navigateTo`、`wx.redirectTo` 或其他原始导航 API。

`src/router/route-meta.ts` 将首页、填报页、成功页标记为鉴权路由。未登录访问受保护路由时，Router Guard 跳转到登录页并携带编码后的 `returnTo`；登录成功后恢复目标地址。登录页和错误页保持公开。

## Query 参数

使用 `encodeQuery` 生成参数，使用 `parseRequiredString`、`parseOptionalNumber`、`parseEnum` 解析外部输入。当前用地需求主流程不依赖记录 ID Query：企业信用代码来自受信认证会话，并作为记录查询键和修改条件。

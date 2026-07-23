# Routing

Routes are discovered from `src/` by weapp-vite autoRoutes. Run `pnpm prepare`
after adding or moving a page; inspect `.weapp-vite/typed-router.d.ts` rather
than editing it.

## Current routes

Main package:

- `/pages/home/index` — tab, public
- `/pages/profile/index` — tab, authenticated direct entry
- `/pages/login/index` — public
- `/pages/error/index` — public fallback

Order subpackage (`subpackages/order` in `vite.config.ts`):

- `/subpackages/order/pages/list/index` — authenticated
- `/subpackages/order/pages/detail/index` — authenticated, requires `id`

`src/app.vue` consumes both `autoRoutes.pages` and `autoRoutes.subPackages` so
the built `dist/app.json` contains the same main/subpackage split.

## Navigation

Pages call `navigate`, `replace`, `replaceUrl`, or `getRouter` through
`src/router/navigation.ts` and `src/router/index.ts`. Tab routes use
`switchTab`; other routes use the Wevu router. Raw `wx.navigateTo`, `wx.request`
and scattered native navigation are not allowed in page code.

`route-meta.ts` marks protected routes. `setupRouter()` adds a guard that
encodes a `returnTo` login redirect. Protected pages also check auth on direct
entry because a share/QR/scheme entry can bypass an internal navigation call.

## Query parsing

Use `encodeQuery` for navigation query strings and `parseRequiredString`,
`parseOptionalNumber`, and `parseEnum` for page input. Reject missing or invalid
IDs/status/page values instead of sending malformed requests.

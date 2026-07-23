---
name: mini-program-routing
description: Add or change generated mini-program routes, subpackages, tabs, query parameters, guards or login redirects.
---

# Mini-program routing workflow

1. Read `docs/routing.md`, root rules and the nearest override.
2. Load `$weapp-vite-best-practices` and `$wevu-best-practices`.
3. Keep startup/login/tab pages under `src/pages`; put order pages under the
   declared `src/subpackages/order` root.
4. Add or move the `.vue` page and run `pnpm prepare`.
5. Inspect `.weapp-vite/typed-router.d.ts`, route metadata and the navigation
   wrapper.
6. Use absolute physical paths, typed values, and validated query parsing.
7. Enforce protected routes in both the global guard and page entry.
8. Test internal navigation, direct entry, missing/invalid query, login return,
   tab behavior and back-stack behavior.
9. Use MCP to inspect the active page and page stack when available.

Never add Vue Router, scatter raw wx navigation calls, or manually edit
generated route declarations.

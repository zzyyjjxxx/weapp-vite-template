# Verification report

Date: 2026-07-23

## Commands

- `pnpm install --frozen-lockfile` — passed; lockfile already up to date.
- `pnpm --config.proxy=http://127.0.0.1:17897 --config.https-proxy=http://127.0.0.1:17897 install` — passed after updating `weapp-vite`, `wevu` and `@weapp-vite/dashboard` to `6.18.6`; pnpm also refreshed the generated release-age exclusions.
- `./node_modules/.bin/wv prepare` — passed with `weapp-vite` `6.18.6`; managed `.weapp-vite` support files remained synchronized.
- `pnpm prepare` — passed; generated `.weapp-vite` support files.
- `pnpm typecheck` — passed; app `vue-tsc` and server `tsc` both passed.
- `pnpm lint` — passed.
- `pnpm stylelint` — passed.
- `pnpm test:coverage` — passed before the icon change; 23 test files and 47
  tests passed. Overall statement coverage: 82.15%; line coverage: 82.55%.
- `pnpm verify` — passed after the `6.18.6` toolchain update; 24 test files
  and 48 tests passed, with lint, stylelint, typecheck, build and budget
  checks all passing.
- `pnpm vitest run tests/unit/components/app-icon.test.ts` — passed; the
  initial Reicon subset and both weights resolve to existing local SVG files.
- `pnpm build` — passed after the icon change; main package 538 KB and
  `subpackages/order` 25.4 KB.
- `pnpm build:server` — passed.
- `pnpm analyze:budget` — passed.
- `pnpm mcp:print` — passed with the generated Codex stdio configuration for
  this workspace.

The generated `dist/app.json` was inspected and contains the four main pages,
the `subpackages/order` root with list/detail pages, and home/profile tab items.

## Hono

- `pnpm dev:api` — started on `http://127.0.0.1:8787` and was stopped after
  smoke verification.
- `curl -fsS http://127.0.0.1:8787/api/health` — passed with `code: SUCCESS`
  and `data.status: ok`.
- Post-update Hono smoke — passed on `http://127.0.0.1:8787`; the same local
  in-memory fixture backend served the DevTools login and order-list flow.
- `curl` login using the local fixture account — returned `code: SUCCESS`; no
  token values are recorded here.
- Vitest server tests cover login, refresh, profile authorization, order list
  pagination/filtering, detail not-found, cancellation success/conflict,
  malformed JSON and missing authorization.

## Runtime

- `wechatide -c ide check_wechatide_status` — passed; the logged-in WeChat
  DevTools skill reported a valid session.
- `open_project_window`, `debug_clear_cache --action clearSession`,
  `simulator_refresh` and `simulator_open_page` — passed for the workspace
  project.
- Login guard — passed in the simulator: tapping `查看订单` while logged out
  opened `/pages/login/index?returnTo=%2Fsubpackages%2Forder%2Fpages%2Flist%2Findex`.
- Post-login route — passed; the observed page stack reached
  `/subpackages/order/pages/list/index` and loaded the four local fixture orders.
- `6.18.6` toolchain smoke — passed; the observed current page was
  `/subpackages/order/pages/list/index`, and screenshots are available at
  `.tmp/runtime-toolchain-6.18.6-home.png`,
  `.tmp/runtime-toolchain-6.18.6-login.png` and
  `.tmp/runtime-toolchain-6.18.6-orders.png`.
- Reicon `AppIcon` — passed in the simulator: the Home, Login and Order List
  page headers rendered the vendored SVG assets through the native `<image>`
  component. Screenshots are available at `.tmp/runtime-icons-home.png`,
  `.tmp/runtime-icons-login.png` and `.tmp/runtime-icons-orders.png`.
- Detail/cancel flow — passed for fixture `order-1002`: the observed route was
  `/subpackages/order/pages/detail/index?id=order-1002`; the detail screen
  displayed the order number, status, amount and cancel action, then updated
  to `已取消` and hid the cancel action after cancellation.
- DevTools Console — the error filter for `error|exception|unhandled|AbortController`
  returned no lines. The full buffer reported WeChatLib 3.17.0 and expected
  store lifecycle debug events only.
- Runtime screenshots are available at
  `.tmp/runtime-home-final.png`, `.tmp/runtime-login-final.png`,
  `.tmp/runtime-orders-final.png`, `.tmp/runtime-detail-final.png` and
  `.tmp/runtime-cancelled-final.png`.
- The DevTools automator could not resolve the Wevu scoped-slot `view` selector
  on the order subpackage. Home/login buttons were tapped through the element
  automator; list-to-detail and cancel were invoked through the observed page
  methods, with the resulting route, state and screenshots verified separately.
- `pnpm mcp:print` passed. `pnpm mcp:doctor` still reports that the generated
  Codex block is absent from `/Users/mang/.codex/config.toml`; this report uses
  the logged-in `wechatide` CLI runtime evidence above and does not claim that
  user-level MCP configuration was modified.
- Screenshot/diff — no baseline or diff claimed.

## Remaining risks

- The local Hono server is an in-memory test backend; restarting it resets the
  fixture data. No upload or publishing workflow was exercised.
- Direct pointer-level automator coverage for Wevu scoped-slot nodes remains
  limited by the installed DevTools automator selector bridge; screenshots and
  runtime state were still observed through the same simulator session.
- Production request legal domains, credentials and backend persistence are
  intentionally outside this local scaffold.

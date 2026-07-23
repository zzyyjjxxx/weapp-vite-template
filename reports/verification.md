# Verification report

Date: 2026-07-23

## Commands

- `pnpm install --frozen-lockfile` — passed; lockfile already up to date.
- `pnpm prepare` — passed; generated `.weapp-vite` support files.
- `pnpm typecheck` — passed; app `vue-tsc` and server `tsc` both passed.
- `pnpm lint` — passed.
- `pnpm stylelint` — passed.
- `pnpm test:coverage` — passed; 21 test files and 43 tests passed. Overall
  statement coverage: 82.01%; line coverage: 82.42%.
- `pnpm build` — passed; main package 488 KB and
  `subpackages/order` 23.6 KB.
- `pnpm build:server` — passed.
- `pnpm analyze:budget` — passed.
- `pnpm verify` — passed; it reran prepare, typecheck, lint, stylelint, tests,
  build and budget checks.

The generated `dist/app.json` was inspected and contains the four main pages,
the `subpackages/order` root with list/detail pages, and home/profile tab items.

## Hono

- `pnpm dev:api` — started on `http://127.0.0.1:8787` and was stopped after
  smoke verification.
- `curl -fsS http://127.0.0.1:8787/api/health` — passed with `code: SUCCESS`
  and `data.status: ok`.
- `curl` login using the local fixture account — returned `code: SUCCESS`; no
  token values are recorded here.
- Vitest server tests cover login, refresh, profile authorization, order list
  pagination/filtering, detail not-found, cancellation success/conflict,
  malformed JSON and missing authorization.

## Runtime

- MCP source config: `pnpm mcp:print` passed and printed the generated
  `weapp-vite-weapp-vite-template` server block.
- `pnpm mcp:doctor` — unavailable in this environment. It reported that the
  generated Codex block was missing from `/Users/mang/.codex/config.toml` and
  exited with status 1.
- Route/query, page stack, login/order interactions, DevTools Console and
  WeChat runtime screenshots — unverified because MCP/DevTools was not
  connected.
- Screenshot/diff — no baseline or diff claimed.

## Remaining risks

- WeChat DevTools runtime behavior still needs an environment with the MCP
  block connected and the local test server reachable.
- Production request legal domains, credentials and backend persistence are
  intentionally outside this local scaffold.

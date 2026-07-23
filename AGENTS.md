# AGENTS Guidelines

## Local Docs First

- After dependencies are installed, prefer reading local package docs under `node_modules/weapp-vite/dist/docs/` first.
- Start with `node_modules/weapp-vite/dist/docs/index.md`, then read `README.md` and `mcp.md` as needed.
- Prefer local package docs over stale model memory or old web pages when command behavior is unclear.

## CLI Entry

- This project supports both `weapp-vite` and `wv` CLI commands.
- Treat `weapp-vite dev` and `wv dev` as equivalent forms.
- Prefer project scripts such as `pnpm dev`, `pnpm build`, `pnpm open`, and `pnpm g` before ad-hoc shell commands.
- Use `weapp-vite prepare` or `wv prepare` when managed support files under `.weapp-vite/` need to be refreshed.
- Prefer `weapp-vite screenshot` or `wv screenshot` for mini-program screenshot acceptance.
- Prefer `weapp-vite compare` or `wv compare` for mini-program screenshot diff, baseline comparison, and visual regression checks.
- Prefer `weapp-vite ide logs --open` or `wv ide logs --open` for DevTools terminal log bridging.
- Do not default to generic browser screenshot tools when the target is the mini-program runtime in WeChat DevTools.

## AI Intent Routing

- When the request mentions screenshot, 截图, 页面快照, runtime screenshot, or capture the current mini-program page, default to `weapp-vite screenshot` / `wv screenshot`.
- When the request mentions screenshot compare, 截图对比, diff, baseline, visual regression, 像素对比, or acceptance comparison, default to `weapp-vite compare` / `wv compare`.
- Treat these commands as the primary screenshot contract for AI workflows in this project.
- Only fall back to generic browser screenshot tools when the target is explicitly the web runtime instead of WeChat DevTools.

## Weapp-vite Workflow

- Keep `vite.config.ts` as the source of truth for `weapp` config, output behavior, and IDE/MCP automation.
- Confirm `weapp.srcRoot`, routes, subpackages, and auto-import strategy before broad refactors.
- Prefer minimal scoped verification: targeted `pnpm build`, targeted tests, then broader checks only when required.
- If editing package source in a monorepo dependency, rebuild the touched package before validating downstream apps to avoid stale `dist`.
- Keep CLI ownership explicit: native `weapp-vite` commands first, IDE passthrough second.
- For Rust/native acceleration, treat JS ↔ Rust boundary crossings as a primary performance cost. Prefer batch analysis that sends source once, parses once, and returns structured results; only put fine-grained native APIs on hot paths when profiling proves a net win.
- Keep native AST fast paths optional and explicitly enabled. They must fall back to Babel/Oxc/Vue compiler behavior on load, parse, or runtime failure, and new native coverage needs correctness tests plus real profiling.

## WeChat DevTools

- Before using screenshot, preview, upload, automator, or `--open`, ensure WeChat DevTools is logged in and the service port is enabled.
- Prefer writing screenshots to workspace paths such as `.tmp/acceptance.png`.

## AI Skills

- Recommend installing shared skills with `npx skills add sonofmagic/skills`.
- In Codex/Claude environments, prefer these skills first when available:
  - `$weapp-vite-best-practices` for config, build, subpackage, route, DevTools CLI orchestration, and screenshot/compare command work.
  - `$weapp-vite-vue-sfc-best-practices` for `.vue` SFC macros, JSON blocks, and template compatibility.
  - `$release-and-changeset-best-practices` for issue delivery, changesets, release decisions, and PR workflow.
  - `$docs-and-website-sync` when documentation or AI guidance must be refreshed together with code changes.
  - Use `$weapp-vite-best-practices` for stateful HMR, pluginRoot/dist-plugin, Web runtime compatibility, and native AST profiling; use `$wevu-best-practices` for `wevu/router` navigation semantics.
  - Use `$weapp-devtools-e2e-best-practices` for serialized DevTools runtime suites, shared automator sessions, and known host compatibility skips.
  - `$wevu-best-practices` for `wevu` runtime lifecycle, state, store, and event contracts.

## Wevu Authoring

- Import runtime APIs from `wevu` in business code.
- Register lifecycle hooks synchronously in `setup()` and avoid hook registration after `await`.
- Prefer `ref`, `reactive`, `computed`, and explicit event contracts over large opaque state writes.
- Use `storeToRefs` when destructuring store state/getters.
- Treat mini-program runtime constraints as primary; do not assume Vue web-only behavior.

## Repository contract

- Read the nearest `AGENTS.md` or `AGENTS.override.md`, the matching Skill in
  `.agents/skills`, and the relevant file under `docs/` before editing.
- Keep pages and components on Wevu Vue SFC. Runtime APIs come from `wevu`,
  not standard Vue, browser DOM APIs, or browser UI libraries.
- Pages depend on domain Services/Queries/Mutations and typed navigation; they
  do not call `fetch`, `wpi`, `wx.request`, or raw navigation APIs.
- Server state belongs to `@tanstack/query-core` through
  `src/shared/query`; auth, preferences, and small client state belong to
  Wevu Store. Do not duplicate Query data into Store.
- The local Hono server is an in-memory test backend only. Never add real
  credentials, payment behavior, production data, or publishing workflows.
- `weapp-tailwindcss` is allowed as a mini-program compatibility tool, but this
  repository does not use native Web TailwindCSS configuration, class scanning,
  or browser Tailwind runtime. Current UI styles use SCSS tokens.
- Run `pnpm prepare` after route or generated-config changes. Do not manually
  edit `.weapp-vite/` declarations.
- Every implementation stage must have a focused test or build check and its
  own Git commit. Do not stage `.DS_Store` or the user's original plan file.

## Source-of-truth docs

- `docs/architecture.md` — package boundaries and data flow
- `docs/routing.md` — generated routes, guards, query parsing and navigation
- `docs/http-client.md` — envelope, auth, errors, refresh and cancellation
- `docs/query-state.md` — Query Core adapter and cache ownership
- `docs/ui-guidelines.md` — tokens, primitives and styling boundary
- `docs/testing.md` — static, API and conditional runtime verification
- `docs/agent-workflow.md` — Skills, MCP and completion evidence

## Completion evidence

Before claiming a task is complete, run the commands relevant to the change,
record their actual results, and state any unavailable DevTools/runtime checks.
For runtime-visible work, use the project runtime-acceptance Skill and do not
claim a screenshot, page-stack or Console result that was not observed.

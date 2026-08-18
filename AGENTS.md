# AGENTS Guidelines

## Product

This repository is the enterprise land-demand (企业用地需求) WeChat Mini Program. Runtime authentication and land-demand records use the local HTTP adapters described in `docs/http-client.md`; verification codes and local drafts remain Mock/WeChat Storage only. Never add production credentials, real SMS, publishing, or production data unless the user explicitly changes scope.

The five steps are 基本信息、用地需求、投资项目、联系人信息、信息确认与提交. Preserve these confirmed rules:

- `deploy_landtype` is single-select and is shown/required only when `is_specialuse=是`.
- `deploy_height` and `deploy_weight` are always visible and optional.
- `investment`, `pred_ys`, `pred_tax`, `pred_rdex` are required and measured in 万元; `pred_unitenergy` is required and measured in 万元/吨标煤.
- `projectdata` has text semantics with no UI character limit.
- `project_hydm` stores `industryCode` and displays `industryName（industryCode）`; use only the generated 150 groups/515 leaves whose numeric SQL `pid` is 181 through 439.

## Local docs first

- Read the nearest `AGENTS.md`/override, matching Skill, and relevant source-of-truth file under `docs/` before editing.
- For version-sensitive CLI behavior, read `node_modules/weapp-vite/dist/docs/index.md` first, then its README/MCP docs as needed.
- Prefer project scripts: `pnpm dev`, `pnpm dev:open`, `pnpm prepare`, `pnpm build`, `pnpm open`.

## Repository contract

- Keep pages/components on Wevu Vue SFC and import runtime APIs from `wevu`.
- Pages depend on domain Services/Queries/Mutations and typed navigation; do not call Storage, Mock Repository, `fetch`, `wx.request`, or raw navigation APIs.
- Query Core owns persisted server-like records. Wevu Store owns auth and the editable form. Do not duplicate Query records into Store.
- Use TDesign MiniProgram, native mini-program nodes, and compile-time Tailwind via `weapp-tailwindcss`; do not add a browser Tailwind runtime or second UI library.
- Keep `vite.config.ts` as the source of truth. Run `pnpm prepare` after route/generated-config changes; never edit `.weapp-vite/` by hand.
- `pnpm lint` is the zero-warning product gate for maintained TS/Vue, tests, E2E, generators and root configs. Only the SQL-generated industry artifact is excluded; validate it through generator and dictionary tests. Style files remain under `pnpm stylelint`.
- Preserve user-owned `.codex/config.toml`, `.mcp.json`, `CLAUDE.md`, `pnpm-lock.yaml`, `.DS_Store`, planning files and unrelated dirty changes.
- Every implementation stage needs a focused test/build check and its own commit.

## WeChat DevTools

- Before E2E, screenshot, compare, preview or `--open`, ensure DevTools is logged in and the service port is enabled.
- Use `pnpm test:e2e` for the serialized Playwright/Automator suite.
- Use `wv screenshot` for runtime screenshots and `wv compare` for visual diffs; prefer workspace `.tmp/` outputs.
- A build does not prove runtime behavior. Report `re-login` or unavailable service ports as blockers, never as passes. Hosted Linux CI cannot run WeChat DevTools E2E.
- If DevTools reports `./pages/login/index.wxml not found`, first inspect the complete console output. When it also contains `err_code: 41002`, `appid missing`, or `summer-compiler miss js file` for every page, the generated login page is not the root cause: verify that `project.config.json.appid` is present, is the correct AppID for this Mini Program, and is the AppID used by the project currently imported into DevTools. An absent or incorrect AppID can leave DevTools with an incomplete project record and make it resolve `pages/**` from the repository root instead of `dist/`. After correcting the AppID, close the affected DevTools project, run `pnpm build`, reopen the repository root containing `project.config.json`, and compile again. Keep `miniprogramRoot` pointed at `dist/`; do not open an old worktree or import `dist/` as a separate project. Also verify `dist/pages/login/index.wxml` exists before compiling. `pnpm dev` clears `dist/` at startup and emits generated pages asynchronously, so compiling during that window can still produce a transient missing-file error.
- For login troubleshooting, the runtime is configured with the HTTP auth adapter in `src/app.vue`. The `demo` credentials belong only to the Mock repository/tests; a successful demo login usually indicates that DevTools loaded an old Mock worktree. Verify the real flow as `POST /customapi/enterpriseapi/login` followed by `GET /customapi/enterpriseapi/getinfo`, and do not record real credentials in repository guidance.

## Source-of-truth docs

- `docs/architecture.md` — boundaries and data flow.
- `docs/routing.md` — generated routes and guards.
- `docs/http-client.md` — HTTP adapters, local API base URL, and Mock captcha boundary.
- `docs/query-state.md` — Query/Store ownership and private cache.
- `docs/ui-guidelines.md` — TDesign, native nodes, Tailwind and field visibility.
- `docs/testing.md` — static, unit, E2E and runtime prerequisites.
- `docs/agent-workflow.md` — implementation and evidence workflow.

## Completion evidence

Run relevant commands and record their actual results in `reports/verification.md`. Do not reuse legacy example evidence, and do not claim screenshots or DevTools interactions that were not observed.

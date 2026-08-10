# AGENTS Guidelines

## Product

This repository is the enterprise land-demand (企业用地需求) WeChat Mini Program. Runtime authentication and land-demand records use the local HTTP adapters described in `docs/http-client.md`; verification codes and local drafts remain Mock/WeChat Storage only. Never add production credentials, real SMS, publishing, or production data unless the user explicitly changes scope.

The five steps are 基本信息、用地需求、投资项目、融资及联系人、信息确认与提交. Preserve these confirmed rules:

- `deploy_landtype` is single-select and is shown/required only when `is_specialuse=是`.
- `deploy_height` and `deploy_weight` are always visible and optional.
- `is_financing` defaults to 没有; amount/time are shown and required only for 有.
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

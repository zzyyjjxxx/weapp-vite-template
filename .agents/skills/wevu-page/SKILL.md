---
name: wevu-page
description: Create or refactor a Wevu Vue SFC page or component in this repository, including lifecycle, forms and loading/error UI.
---

# Wevu page workflow

Use for page/component changes. Do not use for HTTP transport or Query Core
adapter internals.

## Read first

- Root and nearest `AGENTS.md`/`AGENTS.override.md`
- `docs/architecture.md` and `docs/ui-guidelines.md`
- `$weapp-vite-vue-sfc-best-practices` and `$wevu-best-practices`
- The closest existing page with the same interaction pattern

## Procedure

1. Choose the main package or declared business subpackage.
2. Use `<script setup lang="ts">`, `definePageJson`, and runtime imports from
   `wevu` only.
3. Reuse a domain Query/Mutation/Service; do not call fetch, wpi or wx APIs.
4. Handle loading, empty, initial error, retry, background refresh and auth as
   appropriate for the page.
5. Reuse SCSS tokens and semantic UI primitives.
6. Run `pnpm prepare`, `pnpm typecheck`, `pnpm lint`, `pnpm test` and
   `pnpm build`.
7. If DevTools/MCP is available, verify route, page stack, Console, primary
   interaction and screenshot; otherwise record the exact unavailable step.

## Completion evidence

The page appears in generated routes, invalid/direct-entry input is handled,
static checks pass, and runtime-visible evidence is either captured or marked
unverified.

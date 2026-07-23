# Agent workflow

## Before editing

1. Check `git status --short --branch` and preserve user-owned changes.
2. Read root and nearest override instructions.
3. Load the matching project Skill and the relevant `docs/` source of truth.
4. Inspect local weapp-vite/Wevu docs and generated types for version-sensitive
   behavior.
5. Write a focused failing test before production behavior changes where
   practical.

## During implementation

- Keep changes scoped and commit each completed stage separately.
- Run focused tests after each subsystem change.
- Run `pnpm prepare` after routes/config changes.
- Do not stage `.DS_Store` or `weapp-vite-wevu-codex-development-plan.md`.

Project Skills:

- `wevu-page` — page/component and UI state
- `mini-program-routing` — routes, subpackages, guards and query
- `mini-program-api` — Hono endpoints and domain Services
- `wevu-query-state` — Query Core keys, cache and adapter
- `mini-program-runtime-acceptance` — DevTools/MCP runtime evidence

## MCP

The local source of truth for the client block is:

```bash
pnpm mcp:print
pnpm mcp:doctor
```

The current observed `print` command emits a `weapp-vite-weapp-vite-template`
server using the installed weapp-vite CLI and workspace root. In this
environment `doctor` reported that the generated Codex configuration block was
missing from `/Users/mang/.codex/config.toml`; therefore DevTools runtime
acceptance is not claimed until that client configuration is connected.

`.codex/config.toml` contains the project allowlist and approval policy. It
allows source inspection, CLI, DevTools route/state/Console, node inspection,
and approved screenshot tools. Navigation, tap and input are prompt-gated;
screenshots are approval-gated. No upload or publish tool is enabled.

## Completion report

Report files, commands, commits, actual test/build output, route and runtime
evidence, screenshots/diffs, and remaining unverified assumptions. Never infer
runtime success from static checks.

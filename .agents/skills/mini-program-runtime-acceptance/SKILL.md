---
name: mini-program-runtime-acceptance
description: Verify runtime-visible mini-program routes, interactions, Console output, screenshots and visual diffs through weapp-vite MCP.
---

# Runtime acceptance workflow

## Preflight

1. Run `pnpm verify`.
2. Run `pnpm mcp:doctor`.
3. Confirm build output and an isolated local test account/environment.
4. Never use production credentials, payments or destructive user-data actions.

## Checks

1. Connect to DevTools, route to the target with representative query values,
   and read active page plus page stack.
2. Inspect Console/exception output.
3. Verify loading, success, empty and error states where feasible.
4. Tap/input the primary interaction, then re-read page state and Console.
5. Capture a screenshot and compare against an approved baseline for visual
   changes; inspect diffs before updating baselines.

If DevTools or MCP is unavailable, report runtime verification as incomplete
with the exact failing command or connection step. A build pass is not runtime
acceptance.

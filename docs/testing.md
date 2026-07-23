# Testing

## Static and unit checks

```bash
pnpm install --frozen-lockfile
pnpm prepare
pnpm typecheck
pnpm lint
pnpm stylelint
pnpm test
pnpm test:coverage
pnpm build
pnpm analyze:budget
```

`pnpm verify` runs the project gate in one command. The server has separate
`pnpm typecheck:server` and `pnpm build:server` checks.

## Hono smoke

Start the local-only server with `pnpm dev:api`, then run:

```bash
curl -fsS http://127.0.0.1:8787/api/health
```

Vitest covers login, refresh, profile auth, pagination/filtering, detail 404,
cancel success/conflict, malformed JSON and missing authorization. The only
fixture account is `demo` / `demo123`; never use production credentials.

## Runtime acceptance

Runtime checks are conditional on WeChat DevTools and MCP availability. When
available, connect through the runtime-acceptance Skill, verify route/query,
page stack, Console, loading/success/error behavior and the primary login/order
interaction, then capture and compare screenshots. A passing build is not a
runtime acceptance result. If `pnpm mcp:doctor` fails, record the exact failure
and leave runtime status explicitly incomplete.

For production deployment, configure the WeChat legal request domains; this
local scaffold deliberately uses `urlCheck: false` only for local testing.

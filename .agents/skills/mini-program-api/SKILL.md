---
name: mini-program-api
description: Add or change a typed Hono endpoint, domain Service, request/response model or authentication flow.
---

# API workflow

## Read first

- `docs/http-client.md`
- `src/shared/http/AGENTS.override.md`
- Existing Services and server routes in the same domain

## Procedure

1. Confirm method, auth mode, request/response schema and stable error codes.
2. Put normal JSON calls in `src/features/<domain>/service.ts` and use
   `src/shared/http/client.ts`.
3. Define typed models; do not return raw Response or host API values to pages.
4. Preserve Query Core AbortSignal for GET/query functions.
5. Keep UI feedback out of Services and transport.
6. Add tests for success and applicable business, HTTP, network, timeout,
   cancellation and unauthorized behavior.
7. Run focused tests, `pnpm typecheck`, `pnpm lint`, and `pnpm build`.
8. Report any local Hono-only assumptions; do not add production credentials
   or real write workflows.

# Query state

The project uses `@tanstack/query-core` without a browser framework adapter.
`src/shared/query/use-query.ts` and `use-mutation.ts` bridge one Query Core
observer to Wevu refs and register deterministic cleanup at unmount.

## Ownership

- Query Core: profile and order server data, status, errors, refetch and cache.
- Wevu Store: auth session, app readiness, online state and preferences.
- Page refs: draft filters, form inputs and route IDs.

Authenticated queries set `meta.scope: 'private'`. Logout and refresh failure
remove these queries. The full cache is not persisted.

## Domain conventions

Order keys are produced by `orderKeys`: `all`, `lists`, `list(input)`,
`details`, and `detail(id)`. List inputs are fully represented in the key and
use a 30-second stale time. Detail queries stay disabled until an ID exists.

The cancel mutation updates the exact detail cache and invalidates the smallest
list prefix (`orderKeys.lists()`), leaving user-facing feedback to the page.
Mutations default to zero retries.

## Adapter changes

Changes under `src/shared/query` must preserve one observer per hook instance,
AbortSignal propagation, Wevu lifecycle cleanup, and private cache semantics.
Read `src/shared/query/AGENTS.override.md` and the `wevu-query-state` Skill
before changing the adapter.

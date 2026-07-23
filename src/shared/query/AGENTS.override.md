# Query Core adapter rules

Before changing this subsystem, read `docs/query-state.md` and verify the
installed Query Core/Wevu types.

- Query Core is the only server-state cache; page code consumes `useQuery` and `useMutation`.
- Import reactivity and lifecycle APIs from `wevu`, not from a browser Vue runtime.
- A hook creates exactly one QueryObserver or MutationObserver and disposes it on Wevu unmount.
- Query functions must preserve Query Core's AbortSignal and must not hide cancellation.
- Mark authenticated queries with `meta.scope: 'private'` so logout and refresh failure can clear them without importing domain services.
- Do not import domain models or Services into this adapter directory.
- Query Cache persistence is disabled by default; any future persistence needs
  an explicit allowlist, account scope, buster and migration.
- Mutation retry defaults to zero. Account or tenant changes must clear private
  query data.

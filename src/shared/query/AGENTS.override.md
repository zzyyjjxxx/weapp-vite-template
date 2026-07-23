# Query Core adapter rules

- Query Core is the only server-state cache; page code consumes `useQuery` and `useMutation`.
- Import reactivity and lifecycle APIs from `wevu`, not from a browser Vue runtime.
- A hook creates exactly one QueryObserver or MutationObserver and disposes it on Wevu unmount.
- Query functions must preserve Query Core's AbortSignal and must not hide cancellation.
- Mark authenticated queries with `meta.scope: 'private'` so logout and refresh failure can clear them without importing domain services.

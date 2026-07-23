---
name: wevu-query-state
description: Add or modify Query Core keys, domain queries, mutations, invalidation or the repository Wevu adapter.
---

# Query-state workflow

Classify the change first: domain code belongs under `src/features/<domain>`;
adapter behavior belongs under `src/shared/query` and follows its override.

1. Reuse or add stable serializable key factories with every result-changing
   input included.
2. Use a domain Service as `queryFn` and pass Query Core's AbortSignal.
3. Set an explicit stale time and keep mutation retry at zero unless justified.
4. On mutation success, update the exact detail and invalidate the smallest
   affected list prefix.
5. Mark authenticated data with `meta.scope: 'private'` and clear it on logout.
6. Add tests for key stability, error behavior and cache updates/invalidation.
7. For adapter changes, create one observer per hook, dispose deterministically,
   and test the real page using the adapter.

Never install Pinia Colada, standard Pinia, or a framework-specific TanStack
adapter.

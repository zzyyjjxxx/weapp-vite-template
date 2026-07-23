import type { QueryClient } from '@tanstack/query-core'

import { queryClient as defaultQueryClient } from './client'

export const PRIVATE_QUERY_SCOPE = 'private'

export function clearPrivateQueryCaches(client: QueryClient = defaultQueryClient): void {
  client.removeQueries({
    predicate: query => query.meta?.scope === PRIVATE_QUERY_SCOPE,
  })
}

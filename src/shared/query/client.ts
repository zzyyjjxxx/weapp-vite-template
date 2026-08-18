import {
  MutationCache,
  QueryCache,
  QueryClient,
} from '@tanstack/query-core'

import { logger } from '@/shared/logger'

export function createQueryClient(): QueryClient {
  const queryCache = new QueryCache({
    onError: (error, query) => {
      logger.error('query.failed', {
        queryHash: query.queryHash,
      }, error)
    },
  })
  const mutationCache = new MutationCache({
    onError: (error, _variables, _onMutateResult, mutation) => {
      if (mutation.options.meta?.suppressGlobalErrorLog === true) {
        return
      }
      logger.error('mutation.failed', {
        mutationId: String(mutation.mutationId),
      }, error)
    },
  })

  const client = new QueryClient({
    queryCache,
    mutationCache,
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        gcTime: 300_000,
        refetchOnWindowFocus: false,
        refetchOnReconnect: true,
        retry: 0,
      },
      mutations: {
        retry: 0,
      },
    },
  })
  client.mount()
  return client
}

export const queryClient = createQueryClient()

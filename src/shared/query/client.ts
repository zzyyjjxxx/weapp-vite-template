import {
  MutationCache,
  QueryCache,
  QueryClient,
} from '@tanstack/query-core'

import { ApiError } from '@/shared/http/errors'
import { logger } from '@/shared/logger'

function shouldRetryQuery(failureCount: number, error: unknown): boolean {
  return error instanceof ApiError && error.retryable && failureCount < 2
}

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
        retry: shouldRetryQuery,
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

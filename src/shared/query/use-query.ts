import type { QueryClient, QueryKey } from '@tanstack/query-core'
import type { QueryOptionsResolver, UseQueryResult } from './types'
import {

  QueryObserver,
} from '@tanstack/query-core'

import { computed, shallowRef, watchEffect } from 'wevu'
import { queryClient as defaultQueryClient } from './client'
import { registerQueryCleanup } from './lifecycle'

export function useQuery<
  TQueryFnData,
  TError = unknown,
  TData = TQueryFnData,
  TQueryKey extends QueryKey = QueryKey,
>(
  resolveOptions: QueryOptionsResolver<TQueryFnData, TError, TData, TQueryKey>,
  client: QueryClient = defaultQueryClient,
): UseQueryResult<TData, TError> {
  const observer = new QueryObserver(client, resolveOptions())
  const result = shallowRef(observer.getCurrentResult())
  const unsubscribe = observer.subscribe((next) => {
    result.value = next
  })
  const stop = watchEffect(() => {
    observer.setOptions(resolveOptions())
  })
  let disposed = false
  const dispose = (): void => {
    if (disposed) {
      return
    }
    disposed = true
    stop()
    unsubscribe()
    observer.destroy()
  }
  registerQueryCleanup(dispose)

  return {
    result,
    data: computed(() => result.value.data),
    error: computed(() => result.value.error),
    status: computed(() => result.value.status),
    fetchStatus: computed(() => result.value.fetchStatus),
    isPending: computed(() => result.value.isPending),
    isFetching: computed(() => result.value.isFetching),
    isError: computed(() => result.value.isError),
    isSuccess: computed(() => result.value.isSuccess),
    refetch: options => observer.refetch(options),
  }
}

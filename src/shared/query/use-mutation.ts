import type { MutateOptions, MutationObserverResult, QueryClient } from '@tanstack/query-core'
import type { MutationOptionsResolver, UseMutationResult } from './types'
import {

  MutationObserver,

} from '@tanstack/query-core'

import { computed, shallowRef, watchEffect } from 'wevu'
import { queryClient as defaultQueryClient } from './client'
import { registerQueryCleanup } from './lifecycle'

export function useMutation<
  TData,
  TError = unknown,
  TVariables = void,
  TOnMutateResult = unknown,
>(
  resolveOptions: MutationOptionsResolver<TData, TError, TVariables, TOnMutateResult>,
  client: QueryClient = defaultQueryClient,
): UseMutationResult<TData, TError, TVariables, TOnMutateResult> {
  const observer = new MutationObserver(client, resolveOptions())
  const result = shallowRef<MutationObserverResult<TData, TError, TVariables, TOnMutateResult>>(
    observer.getCurrentResult(),
  )
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
  }
  registerQueryCleanup(dispose)

  const mutateAsync = (
    variables: TVariables,
    options?: MutateOptions<TData, TError, TVariables, TOnMutateResult>,
  ): Promise<TData> => observer.mutate(variables, options)
  const mutate = (
    variables: TVariables,
    options?: MutateOptions<TData, TError, TVariables, TOnMutateResult>,
  ): void => {
    void mutateAsync(variables, options).catch(() => undefined)
  }

  return {
    result,
    data: computed(() => result.value.data),
    error: computed(() => result.value.error),
    status: computed(() => result.value.status),
    isIdle: computed(() => result.value.isIdle),
    isPending: computed(() => result.value.isPending),
    isError: computed(() => result.value.isError),
    isSuccess: computed(() => result.value.isSuccess),
    mutate,
    mutateAsync,
    reset: () => observer.reset(),
  }
}

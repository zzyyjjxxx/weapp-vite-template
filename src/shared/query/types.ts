import type {
  MutateOptions,
  MutationObserverOptions,
  MutationObserverResult,
  QueryKey,
  QueryObserverOptions,
  QueryObserverResult,
  RefetchOptions,
} from '@tanstack/query-core'
import type { ComputedRef, Ref } from 'wevu'

export type QueryOptionsResolver<
  TQueryFnData,
  TError,
  TData,
  TQueryKey extends QueryKey,
> = () => QueryObserverOptions<TQueryFnData, TError, TData, TQueryFnData, TQueryKey>

export interface UseQueryResult<TData, TError> {
  result: Ref<QueryObserverResult<TData, TError>>
  data: ComputedRef<TData | undefined>
  error: ComputedRef<TError | null>
  status: ComputedRef<QueryObserverResult<TData, TError>['status']>
  fetchStatus: ComputedRef<QueryObserverResult<TData, TError>['fetchStatus']>
  isPending: ComputedRef<boolean>
  isFetching: ComputedRef<boolean>
  isError: ComputedRef<boolean>
  isSuccess: ComputedRef<boolean>
  refetch: (options?: RefetchOptions) => Promise<QueryObserverResult<TData, TError>>
}

export type MutationOptionsResolver<TData, TError, TVariables, TOnMutateResult>
  = () => MutationObserverOptions<TData, TError, TVariables, TOnMutateResult>

export interface UseMutationResult<TData, TError, TVariables, TOnMutateResult> {
  result: Ref<MutationObserverResult<TData, TError, TVariables, TOnMutateResult>>
  data: ComputedRef<TData | undefined>
  error: ComputedRef<TError | null>
  status: ComputedRef<MutationObserverResult<TData, TError, TVariables, TOnMutateResult>['status']>
  isIdle: ComputedRef<boolean>
  isPending: ComputedRef<boolean>
  isError: ComputedRef<boolean>
  isSuccess: ComputedRef<boolean>
  mutate: (
    variables: TVariables,
    options?: MutateOptions<TData, TError, TVariables, TOnMutateResult>,
  ) => void
  mutateAsync: (
    variables: TVariables,
    options?: MutateOptions<TData, TError, TVariables, TOnMutateResult>,
  ) => Promise<TData>
  reset: () => void
}

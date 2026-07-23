import type { QueryClient } from '@tanstack/query-core'
import type { Ref } from 'wevu'

import type { Order, OrderListInput, OrderListResult } from './models'
import type { UseMutationResult, UseQueryResult } from '@/shared/query/types'
import { queryClient as defaultQueryClient } from '@/shared/query/client'
import { PRIVATE_QUERY_SCOPE } from '@/shared/query/private-cache'
import { useMutation } from '@/shared/query/use-mutation'
import { useQuery } from '@/shared/query/use-query'
import { orderKeys } from './query-keys'
import { cancelOrder, getOrder, getOrders } from './service'

const orderQueryOptions = {
  staleTime: 30_000,
} as const

export function useOrderListQuery(
  input: Ref<OrderListInput>,
  client: QueryClient = defaultQueryClient,
): UseQueryResult<OrderListResult, Error> {
  return useQuery<OrderListResult, Error, OrderListResult, ReturnType<typeof orderKeys.list>>(
    () => ({
      ...orderQueryOptions,
      queryKey: orderKeys.list(input.value),
      queryFn: ({ signal }) => getOrders(input.value, { signal }),
      meta: {
        scope: PRIVATE_QUERY_SCOPE,
      },
    }),
    client,
  )
}

export function useOrderDetailQuery(
  id: Ref<string>,
  client: QueryClient = defaultQueryClient,
): UseQueryResult<Order, Error> {
  return useQuery<Order, Error, Order, ReturnType<typeof orderKeys.detail>>(
    () => ({
      ...orderQueryOptions,
      queryKey: orderKeys.detail(id.value),
      queryFn: ({ signal }) => getOrder(id.value, { signal }),
      enabled: () => id.value.length > 0,
      meta: {
        scope: PRIVATE_QUERY_SCOPE,
      },
    }),
    client,
  )
}

export function useCancelOrderMutation(
  client: QueryClient = defaultQueryClient,
): UseMutationResult<Order, Error, string, unknown> {
  return useMutation<Order, Error, string, unknown>(() => ({
    mutationKey: [...orderKeys.all, 'cancel'],
    mutationFn: id => cancelOrder(id),
    onSuccess: (order, id) => {
      client.setQueryData(orderKeys.detail(id), order)
      void client.invalidateQueries({ queryKey: orderKeys.lists() })
    },
  }), client)
}

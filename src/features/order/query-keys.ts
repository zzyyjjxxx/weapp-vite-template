import type { OrderListInput } from './models'

export const orderKeys = {
  all: ['orders'] as const,
  lists: () => [...orderKeys.all, 'list'] as const,
  list: (input: OrderListInput) => [
    ...orderKeys.lists(),
    {
      page: input.page,
      pageSize: input.pageSize,
      status: input.status ?? '',
      keyword: input.keyword ?? '',
    },
  ] as const,
  details: () => [...orderKeys.all, 'detail'] as const,
  detail: (id: string) => [...orderKeys.details(), id] as const,
}

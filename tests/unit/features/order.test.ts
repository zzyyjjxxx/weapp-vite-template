import type { Order, OrderListResult } from '@/features/order/models'

import { describe, expect, it, vi } from 'vitest'
import { orderKeys } from '@/features/order/query-keys'
import {
  cancelOrder,
  getOrder,
  getOrders,
} from '@/features/order/service'

const order: Order = {
  id: 'order-1',
  number: 'NO-1',
  status: 'pending',
  statusLabel: '待处理',
  amount: 10,
  createdAt: '2026-07-23T00:00:00.000Z',
  canCancel: true,
}

describe('order service', () => {
  it('includes every list input in the order list key', () => {
    expect(orderKeys.list({ page: 1, pageSize: 10, status: 'pending', keyword: '' }))
      .not
      .toEqual(orderKeys.list({ page: 2, pageSize: 10, status: 'pending', keyword: '' }))
  })

  it('encodes order ids and passes query signals through the service boundary', async () => {
    const request = vi.fn()
      .mockResolvedValueOnce({ items: [order], total: 1, page: 1, pageSize: 10 } satisfies OrderListResult)
      .mockResolvedValueOnce(order)
      .mockResolvedValueOnce(order)
    const signal = new AbortController().signal

    await getOrders({ page: 1, pageSize: 10 }, { request, signal })
    await getOrder('order/1', { request, signal })
    await cancelOrder('order/1', { request })

    expect(request).toHaveBeenNthCalledWith(1, expect.objectContaining({
      path: '/orders',
      method: 'GET',
      signal,
    }))
    expect(request).toHaveBeenNthCalledWith(2, expect.objectContaining({
      path: '/orders/order%2F1',
      method: 'GET',
      signal,
    }))
    expect(request).toHaveBeenNthCalledWith(3, expect.objectContaining({
      path: '/orders/order%2F1/cancel',
      method: 'POST',
    }))
  })
})

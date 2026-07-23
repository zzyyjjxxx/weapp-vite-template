import type { Order, OrderListInput, OrderListResult } from './models'

import type { RequestOptions } from '@/shared/http/types'
import { request as httpRequest } from '@/shared/http/client'

type Request = typeof httpRequest

export interface OrderServiceRequestOptions {
  request?: Request
  signal?: AbortSignal
}

function orderPath(id: string): string {
  return `/orders/${encodeURIComponent(id)}`
}

export function getOrders(
  input: OrderListInput,
  options: OrderServiceRequestOptions = {},
): Promise<OrderListResult> {
  const request = options.request ?? httpRequest
  const requestOptions: RequestOptions = {
    path: '/orders',
    method: 'GET',
    auth: 'required',
    query: {
      page: input.page,
      pageSize: input.pageSize,
      status: input.status,
      keyword: input.keyword,
    },
    signal: options.signal,
  }
  return request<OrderListResult>(requestOptions)
}

export function getOrder(
  id: string,
  options: OrderServiceRequestOptions = {},
): Promise<Order> {
  const request = options.request ?? httpRequest
  const requestOptions: RequestOptions = {
    path: orderPath(id),
    method: 'GET',
    auth: 'required',
    signal: options.signal,
  }
  return request<Order>(requestOptions)
}

export function cancelOrder(
  id: string,
  options: OrderServiceRequestOptions = {},
): Promise<Order> {
  const request = options.request ?? httpRequest
  const requestOptions: RequestOptions = {
    path: `${orderPath(id)}/cancel`,
    method: 'POST',
    auth: 'required',
  }
  return request<Order>(requestOptions)
}

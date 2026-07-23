import type { AppEnv, OrderStatus } from '../types'

import { Hono } from 'hono'
import { failure, success } from '../envelope'
import {
  cancelOrderById,
  getOrderById,
  getOrders,
} from '../fixtures'
import { requireBearerUser } from '../middleware/auth'

const validStatuses = new Set<OrderStatus>([
  'pending',
  'processing',
  'completed',
  'cancelled',
])

function parsePositiveInteger(
  value: string | undefined,
  fallback: number,
  max: number,
): number | undefined {
  if (value === undefined || value === '') {
    return fallback
  }

  const parsed = Number(value)
  if (!Number.isInteger(parsed) || parsed < 1 || parsed > max) {
    return undefined
  }

  return parsed
}

export const orderRoutes = new Hono<AppEnv>()

orderRoutes.get('/orders', requireBearerUser, (c) => {
  const page = parsePositiveInteger(c.req.query('page'), 1, 10_000)
  const pageSize = parsePositiveInteger(c.req.query('pageSize'), 10, 50)
  const statusValue = c.req.query('status')
  const keyword = c.req.query('keyword')?.trim().toLowerCase() ?? ''

  if (page === undefined || pageSize === undefined) {
    return c.json(failure('INVALID_PAGINATION', '分页参数无效'), 400)
  }

  if (statusValue && !validStatuses.has(statusValue as OrderStatus)) {
    return c.json(failure('INVALID_STATUS', '订单状态无效'), 400)
  }

  const filtered = getOrders().filter((order) => {
    const matchesStatus = !statusValue || order.status === statusValue
    const matchesKeyword = keyword.length === 0
      || order.number.toLowerCase().includes(keyword)
      || order.id.toLowerCase().includes(keyword)
    return matchesStatus && matchesKeyword
  })

  const start = (page - 1) * pageSize
  return c.json(success({
    items: filtered.slice(start, start + pageSize),
    total: filtered.length,
    page,
    pageSize,
  }, '订单列表加载成功'))
})

orderRoutes.get('/orders/:id', requireBearerUser, (c) => {
  const order = getOrderById(c.req.param('id') ?? '')
  if (!order) {
    return c.json(failure('ORDER_NOT_FOUND', '订单不存在'), 404)
  }

  return c.json(success(order, '订单详情加载成功'))
})

orderRoutes.post('/orders/:id/cancel', requireBearerUser, (c) => {
  const current = getOrderById(c.req.param('id') ?? '')
  if (!current) {
    return c.json(failure('ORDER_NOT_FOUND', '订单不存在'), 404)
  }

  if (!current.canCancel) {
    return c.json(failure('ORDER_NOT_CANCELLABLE', '当前订单不可取消'), 409)
  }

  const order = cancelOrderById(current.id)
  return c.json(success(order, '订单已取消'))
})

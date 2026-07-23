import type { AuthSession, OrderListResult } from '../../server/types'

import { beforeEach, describe, expect, it } from 'vitest'
import { app } from '../../server/app'
import { resetFixtures } from '../../server/fixtures'

beforeEach(() => {
  resetFixtures()
})

async function login(): Promise<AuthSession> {
  const response = await app.request('/api/auth/login', {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ username: 'demo', password: 'demo123' }),
  })

  const body = await response.json() as { data: AuthSession }
  return body.data
}

describe('order routes', () => {
  it('supports pagination and status filtering', async () => {
    const session = await login()
    const response = await app.request('/api/orders?page=1&pageSize=1&status=pending', {
      headers: { authorization: `Bearer ${session.accessToken}` },
    })
    const body = await response.json() as { data: OrderListResult }

    expect(response.status).toBe(200)
    expect(body.data.total).toBe(1)
    expect(body.data.items[0]?.status).toBe('pending')
  })

  it('returns a detail and cancels a cancellable order', async () => {
    const session = await login()
    const headers = { authorization: `Bearer ${session.accessToken}` }
    const detail = await app.request('/api/orders/order-1001', { headers })
    expect(detail.status).toBe(200)

    const cancelled = await app.request('/api/orders/order-1001/cancel', {
      method: 'POST',
      headers,
    })
    expect(cancelled.status).toBe(200)
    expect(await cancelled.json()).toMatchObject({
      data: { id: 'order-1001', status: 'cancelled', canCancel: false },
    })

    const conflict = await app.request('/api/orders/order-1001/cancel', {
      method: 'POST',
      headers,
    })
    expect(conflict.status).toBe(409)
    expect(await conflict.json()).toMatchObject({ code: 'ORDER_NOT_CANCELLABLE' })
  })

  it('rejects invalid pagination and unknown details', async () => {
    const session = await login()
    const headers = { authorization: `Bearer ${session.accessToken}` }
    const invalid = await app.request('/api/orders?page=0', { headers })
    const missing = await app.request('/api/orders/not-found', { headers })

    expect(invalid.status).toBe(400)
    expect(missing.status).toBe(404)
  })
})

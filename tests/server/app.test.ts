import { describe, expect, it } from 'vitest'

import { app } from '../../server/app'

describe('Hono test API', () => {
  it('returns a healthy success envelope', async () => {
    const response = await app.request('/api/health')

    expect(response.status).toBe(200)
    expect(await response.json()).toMatchObject({
      code: 'SUCCESS',
      data: { status: 'ok' },
    })
  })

  it('rejects invalid credentials', async () => {
    const response = await app.request('/api/auth/login', {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ username: 'demo', password: 'wrong' }),
    })

    expect(response.status).toBe(401)
  })

  it('requires a bearer token for orders', async () => {
    const response = await app.request('/api/orders')

    expect(response.status).toBe(401)
  })
})

import type { AuthSession } from '../../server/types'

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

describe('auth routes', () => {
  it('logs in and returns the current profile', async () => {
    const session = await login()
    const response = await app.request('/api/profile', {
      headers: { authorization: `Bearer ${session.accessToken}` },
    })

    expect(response.status).toBe(200)
    expect(await response.json()).toMatchObject({
      code: 'SUCCESS',
      data: { id: 'user-demo', username: 'demo' },
    })
  })

  it('rotates a refresh token into a new session', async () => {
    const first = await login()
    const response = await app.request('/api/auth/refresh', {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ refreshToken: first.refreshToken }),
    })
    const body = await response.json() as { data: AuthSession }

    expect(response.status).toBe(200)
    expect(body.data.accessToken).not.toBe(first.accessToken)
    expect(body.data.refreshToken).not.toBe(first.refreshToken)
  })

  it('distinguishes malformed JSON from missing fields', async () => {
    const response = await app.request('/api/auth/login', {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: '{bad json',
    })

    expect(response.status).toBe(400)
    expect(await response.json()).toMatchObject({ code: 'INVALID_JSON' })
  })
})

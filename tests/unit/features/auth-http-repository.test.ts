import type { AuthSession } from '@/features/auth/models'
import type { ApiClient } from '@/platform/http-client'

import { describe, expect, it } from 'vitest'
import { createHttpAuthRepository } from '@/features/auth/http-repository'

function createClient(responses: unknown[]): {
  client: ApiClient
  calls: Array<{ method: string, path: string, options?: { body?: unknown, token?: string } }>
} {
  const calls: Array<{ method: string, path: string, options?: { body?: unknown, token?: string } }> = []
  const client: ApiClient = {
    async request<T>(method, path, options) {
      calls.push({ method, path, options })
      return responses.shift() as T
    },
  }
  return { client, calls }
}

describe('HTTP auth repository', () => {
  it('logs in, loads enterprise info, and maps token expiry', async () => {
    const { client, calls } = createClient([
      {
        access_token: 'access-token',
        refresh_token: 'refresh-token',
        token_type: 'Bearer',
        expires_in: 60,
        refresh_expires_in: 600,
      },
      {
        businessname: 'Example Enterprise',
        creditcode: '91330200EXAMPLE001',
        county: 'County',
        region: 'Town',
      },
    ])
    const repository = createHttpAuthRepository({ client, now: () => 1_000 })

    const session = await repository.login({ username: '91330200EXAMPLE001', password: 'secret' })

    expect(session).toEqual({
      token: 'access-token',
      refreshToken: 'refresh-token',
      tokenType: 'Bearer',
      expiresAt: 61_000,
      refreshExpiresAt: 601_000,
      enterprise: {
        id: '91330200EXAMPLE001',
        username: '91330200EXAMPLE001',
        businessname: 'Example Enterprise',
        creditcode: '91330200EXAMPLE001',
        county: 'County',
        region: 'Town',
        contact: '',
        office: '',
        phone: '',
      },
    })
    expect(calls).toEqual([
      {
        method: 'POST',
        path: '/customapi/enterpriseapi/login',
        options: { body: { username: '91330200EXAMPLE001', password: 'secret' } },
      },
      {
        method: 'GET',
        path: '/customapi/enterpriseapi/getinfo',
        options: { token: 'access-token' },
      },
    ])
  })

  it('refreshes tokens and preserves editable contact fields', async () => {
    const { client, calls } = createClient([
      {
        access_token: 'new-access-token',
        refresh_token: 'new-refresh-token',
        expires_in: 120,
        refresh_expires_in: 900,
      },
      {
        businessname: 'Updated Enterprise',
        creditcode: '91330200EXAMPLE001',
        county: 'County',
        region: 'Town',
      },
    ])
    const repository = createHttpAuthRepository({ client, now: () => 2_000 })
    const previous: AuthSession = {
      token: 'old-access-token',
      refreshToken: 'old-refresh-token',
      expiresAt: 2_500,
      enterprise: {
        id: '91330200EXAMPLE001',
        username: '91330200EXAMPLE001',
        businessname: 'Old Enterprise',
        creditcode: '91330200EXAMPLE001',
        county: 'County',
        region: 'Town',
        contact: 'Contact',
        office: 'Office',
        phone: '13800000000',
      },
    }

    const session = await repository.refresh?.(previous)

    expect(session?.token).toBe('new-access-token')
    expect(session?.enterprise).toMatchObject({
      businessname: 'Updated Enterprise',
      contact: 'Contact',
      office: 'Office',
      phone: '13800000000',
    })
    expect(calls[0]).toEqual({
      method: 'POST',
      path: '/customapi/enterpriseapi/refresh',
      options: { body: { refresh_token: 'old-refresh-token' } },
    })
    expect(calls[1]).toEqual({
      method: 'GET',
      path: '/customapi/enterpriseapi/getinfo',
      options: { token: 'new-access-token' },
    })
  })
})

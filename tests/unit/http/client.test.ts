import type { AuthSession, AuthSessionStore } from '@/shared/http/session'

import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { request } from '@/shared/http/client'
import {
  configureAuthSessionStore,
  resetAuthSessionStore,
} from '@/shared/http/session'
import {
  resetFetchImplementation,
  setFetchImplementation,
} from '@/shared/http/transport'

const initialSession: AuthSession = {
  accessToken: 'access-old',
  refreshToken: 'refresh-old',
  expiresAt: Date.now() + 60_000,
  userId: 'user-demo',
  tenantId: 'tenant-demo',
}

function createStore(session: AuthSession | null = initialSession): AuthSessionStore & {
  session: AuthSession | null
} {
  const store: AuthSessionStore & { session: AuthSession | null } = {
    session,
    getAccessToken: () => store.session?.accessToken,
    getRefreshToken: () => store.session?.refreshToken,
    setSession: (nextSession) => { store.session = nextSession },
    clearSession: () => { store.session = null },
  }
  return store
}

function success(data: unknown): Response {
  return new Response(JSON.stringify({
    code: 'SUCCESS',
    message: 'ok',
    data,
    traceId: 'trace-success',
  }), { status: 200 })
}

function unauthorized(): Response {
  return new Response(JSON.stringify({
    code: 'UNAUTHORIZED',
    message: 'expired',
    data: null,
    traceId: 'trace-unauthorized',
  }), { status: 401 })
}

describe('request', () => {
  beforeEach(() => {
    configureAuthSessionStore(createStore())
  })

  afterEach(() => {
    resetAuthSessionStore()
    resetFetchImplementation()
  })

  it('shares one refresh when concurrent requests receive 401', async () => {
    const store = createStore()
    configureAuthSessionStore(store)
    let calls = 0

    setFetchImplementation(async (_input, init) => {
      calls += 1
      const authorization = new Headers(init?.headers).get('authorization')
      if (authorization === 'Bearer access-old') {
        return unauthorized()
      }
      if (init?.body === JSON.stringify({ refreshToken: 'refresh-old' })) {
        return success({
          ...initialSession,
          accessToken: 'access-new',
          refreshToken: 'refresh-new',
        })
      }
      return success({ id: 'profile-demo' })
    })

    const [first, second] = await Promise.all([
      request<{ id: string }>({ path: '/profile' }),
      request<{ id: string }>({ path: '/profile' }),
    ])

    expect(first).toEqual({ id: 'profile-demo' })
    expect(second).toEqual({ id: 'profile-demo' })
    expect(calls).toBe(5)
    expect(store.session?.accessToken).toBe('access-new')
  })

  it('rejects required requests without an access token', async () => {
    configureAuthSessionStore(createStore(null))
    let calls = 0
    setFetchImplementation(async () => {
      calls += 1
      return success(null)
    })

    await expect(request({ path: '/profile' })).rejects.toMatchObject({
      kind: 'unauthorized',
      code: 'MISSING_ACCESS_TOKEN',
    })
    expect(calls).toBe(0)
  })
})

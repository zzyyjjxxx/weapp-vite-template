import type { AuthSession, AuthSessionStore } from '@/shared/http/session'

import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import {

  configureAuthSessionStore,
} from '@/shared/http/session'
import { refreshAccessTokenSingleFlight } from '@/shared/http/token-refresh'
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

function createStore(): AuthSessionStore & { session: AuthSession | null } {
  const store: AuthSessionStore & { session: AuthSession | null } = {
    session: initialSession,
    getAccessToken: () => store.session?.accessToken,
    getRefreshToken: () => store.session?.refreshToken,
    setSession: (session) => { store.session = session },
    clearSession: () => { store.session = null },
  }
  return store
}

describe('refreshAccessTokenSingleFlight', () => {
  let store: ReturnType<typeof createStore>

  beforeEach(() => {
    store = createStore()
    configureAuthSessionStore(store)
  })

  afterEach(() => {
    resetFetchImplementation()
  })

  it('shares one refresh request between concurrent callers', async () => {
    let calls = 0
    const refreshed: AuthSession = {
      ...initialSession,
      accessToken: 'access-new',
      refreshToken: 'refresh-new',
    }

    setFetchImplementation(async () => {
      calls += 1
      await Promise.resolve()
      return new Response(JSON.stringify({
        code: 'SUCCESS',
        message: 'ok',
        data: refreshed,
        traceId: 'trace-refresh',
      }), { status: 200 })
    })

    const results = await Promise.all([
      refreshAccessTokenSingleFlight(),
      refreshAccessTokenSingleFlight(),
    ])

    expect(calls).toBe(1)
    expect(results[0]).toEqual(refreshed)
    expect(results[1]).toEqual(refreshed)
    expect(store.session).toEqual(refreshed)
  })

  it('clears the session when refresh fails', async () => {
    setFetchImplementation(async () => new Response(JSON.stringify({
      code: 'INVALID_REFRESH_TOKEN',
      message: 'invalid',
      data: null,
      traceId: 'trace-fail',
    }), { status: 401 }))

    await expect(refreshAccessTokenSingleFlight()).rejects.toMatchObject({
      kind: 'unauthorized',
    })
    expect(store.session).toBeNull()
  })
})

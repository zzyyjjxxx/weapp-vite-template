import type { AuthSession } from '@/shared/http/session'

import { beforeAll, beforeEach, describe, expect, it } from 'vitest'
import { useAuthStore } from '@/stores/auth'
import { setupStorePlugins } from '@/stores/plugins'
import { createMemoryStorage } from '../../helpers/memory-storage'

const session: AuthSession = {
  accessToken: 'access',
  refreshToken: 'refresh',
  expiresAt: Date.now() + 60_000,
  userId: 'user-demo',
}

describe('auth store', () => {
  const storage = createMemoryStorage()

  beforeAll(() => {
    setupStorePlugins({ storage })
  })

  beforeEach(() => {
    storage.clear()
    useAuthStore().$reset()
  })

  it('is authenticated only while a non-expired session exists', () => {
    const auth = useAuthStore()

    auth.setSession(session)
    expect(auth.isAuthenticated.value).toBe(true)

    auth.clearSession()
    expect(auth.isAuthenticated.value).toBe(false)
  })

  it('tracks initialization separately from authentication', () => {
    const auth = useAuthStore()

    expect(auth.initialized.value).toBe(false)
    auth.markInitialized()
    expect(auth.initialized.value).toBe(true)
    expect(auth.isAuthenticated.value).toBe(false)
  })
})

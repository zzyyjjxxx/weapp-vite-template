import type { AuthSession } from '@/shared/http/session'
import type { PersistedAuthStateV1 } from '@/stores/plugins/persistence'

import { beforeAll, beforeEach, describe, expect, it } from 'vitest'
import { nextTick } from 'wevu'
import { useAuthStore } from '@/stores/auth'
import { setupStorePlugins } from '@/stores/plugins'
import {
  AUTH_STORAGE_KEY,

} from '@/stores/plugins/persistence'
import { createMemoryStorage } from '../../helpers/memory-storage'

const session: AuthSession = {
  accessToken: 'access',
  refreshToken: 'refresh',
  expiresAt: Date.now() + 60_000,
  userId: 'user-demo',
}

describe('auth persistence', () => {
  const storage = createMemoryStorage()

  beforeAll(() => {
    setupStorePlugins({ storage })
  })

  beforeEach(() => {
    storage.clear()
    useAuthStore().$reset()
  })

  it('persists only the versioned auth session whitelist', async () => {
    const auth = useAuthStore()

    auth.setSession(session)
    await nextTick()

    expect(storage.get<PersistedAuthStateV1>(AUTH_STORAGE_KEY)).toEqual({
      version: 1,
      session,
    })
  })

  it('clears persisted state on logout', async () => {
    const auth = useAuthStore()
    auth.setSession(session)
    auth.clearSession()
    await nextTick()

    expect(storage.get(AUTH_STORAGE_KEY)).toEqual({
      version: 1,
      session: null,
    })
  })
})

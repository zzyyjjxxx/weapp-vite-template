import type { AuthSession } from '@/features/auth/models'
import type { PersistedAuthStateV1 } from '@/stores/plugins/persistence'

import { beforeAll, beforeEach, describe, expect, it } from 'vitest'
import { nextTick } from 'wevu'
import { useAuthStore } from '@/stores/auth'
import { setupStorePlugins } from '@/stores/plugins'
import {
  AUTH_STORAGE_KEY,
  createPersistencePlugin,
  readPersistedAuthSession,
} from '@/stores/plugins/persistence'
import { createMemoryStorage } from '../../helpers/memory-storage'

const session: AuthSession = {
  token: 'demo-session-token',
  expiresAt: Date.now() + 60_000,
  enterprise: {
    id: 'enterprise-demo',
    username: 'demo',
    businessname: '宁波示范智造有限公司',
    creditcode: '91330200MA2DEMO001',
    county: '鄞州区',
    region: '首南街道',
    contact: '张示例',
    office: '法定代表人',
    phone: '13800000000',
  },
}

function createPersistenceContext() {
  const store: {
    $id: string
    session: AuthSession | null
    $patch: (patch: { session?: AuthSession | null }) => void
    $subscribe: () => () => void
  } = {
    $id: 'auth',
    session: null,
    $patch: (patch) => {
      store.session = patch.session ?? null
    },
    $subscribe: () => () => undefined,
  }

  return { store }
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

  it('overwrites an existing persisted session after a new login', async () => {
    const auth = useAuthStore()
    const previousSession = {
      ...session,
      token: 'previous-session-token',
      enterprise: {
        ...session.enterprise,
        creditcode: '91330200MA2PREVIOUS01',
      },
    }
    const nextSession = {
      ...session,
      token: 'next-session-token',
      enterprise: {
        ...session.enterprise,
        creditcode: '91330200MA2NEXT0001',
      },
    }

    storage.set(AUTH_STORAGE_KEY, { version: 1, session: previousSession })
    auth.setSession(nextSession)
    await nextTick()

    expect(storage.get<PersistedAuthStateV1>(AUTH_STORAGE_KEY)).toEqual({
      version: 1,
      session: nextSession,
    })
  })

  it('clears persisted state on logout', async () => {
    const auth = useAuthStore()
    auth.setSession(session)
    auth.clearSession()
    await nextTick()

    expect(storage.get(AUTH_STORAGE_KEY)).toBeUndefined()
  })

  it('restores a valid session and clears malformed persisted sessions', () => {
    const restored = createPersistenceContext()
    storage.set('land-demand.auth', { version: 1, session })

    createPersistencePlugin(storage)(restored)

    expect(restored.store.session).toEqual(session)

    const malformed = createPersistenceContext()
    storage.set('land-demand.auth', { version: 1, session: { token: 42 } })

    createPersistencePlugin(storage)(malformed)

    expect(malformed.store.session).toBeNull()
    expect(readPersistedAuthSession(storage)).toBeUndefined()
  })

  it('does not overwrite a persisted session during an app-instance reset', () => {
    let subscriber: ((mutation: unknown, state: unknown) => void) | undefined
    const lifecycleStore = {
      $id: 'auth',
      session: null as AuthSession | null,
      $patch(patch: { session?: AuthSession | null }) {
        lifecycleStore.session = patch.session ?? null
      },
      $subscribe(callback: (mutation: unknown, state: unknown) => void) {
        subscriber = callback
        return () => undefined
      },
    }
    storage.set(AUTH_STORAGE_KEY, { version: 1, session })
    createPersistencePlugin(storage)({ store: lifecycleStore })

    subscriber?.({}, { session: null, sessionClearRevision: 0 })
    expect(storage.get<PersistedAuthStateV1>(AUTH_STORAGE_KEY)?.session).toEqual(session)

    subscriber?.({}, { session: null, sessionClearRevision: 1 })
    expect(storage.get<PersistedAuthStateV1>(AUTH_STORAGE_KEY)).toBeUndefined()
  })
})

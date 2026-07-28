import type { AuthSession } from '@/features/auth/models'

import { beforeAll, beforeEach, describe, expect, it } from 'vitest'
import { queryClient } from '@/shared/query/client'
import { useAuthStore } from '@/stores/auth'
import { setupStorePlugins } from '@/stores/plugins'
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

describe('auth store', () => {
  const storage = createMemoryStorage()

  beforeAll(() => {
    setupStorePlugins({ storage })
  })

  beforeEach(() => {
    storage.clear()
    queryClient.clear()
    useAuthStore().$reset()
  })

  it('exposes the enterprise while an unexpired session exists', () => {
    const auth = useAuthStore()

    auth.setSession(session)

    expect(auth.isAuthenticated.value).toBe(true)
    expect(auth.enterprise.value).toEqual(session.enterprise)
  })

  it('clears authentication and enterprise together', () => {
    const auth = useAuthStore()
    auth.setSession(session)

    auth.clearSession()

    expect(auth.isAuthenticated.value).toBe(false)
    expect(auth.enterprise.value).toBeUndefined()
  })

  it('clears private query data on logout', async () => {
    const auth = useAuthStore()
    auth.setSession(session)
    await seedPrivateQuery()

    auth.clearSession()

    expect(queryClient.getQueryData(['private', 'enterprise-record'])).toBeUndefined()
  })

  it('clears private query data before switching enterprises', async () => {
    const auth = useAuthStore()
    auth.setSession(session)
    await seedPrivateQuery()

    auth.setSession({
      ...session,
      enterprise: {
        ...session.enterprise,
        id: 'enterprise-other',
        creditcode: '91330200MA2OTHER01',
      },
    })

    expect(queryClient.getQueryData(['private', 'enterprise-record'])).toBeUndefined()
  })

  it('tracks initialization separately from authentication', () => {
    const auth = useAuthStore()

    expect(auth.initialized.value).toBe(false)
    auth.markInitialized()
    expect(auth.initialized.value).toBe(true)
    expect(auth.isAuthenticated.value).toBe(false)
  })
})

async function seedPrivateQuery(): Promise<void> {
  await queryClient.fetchQuery({
    queryKey: ['private', 'enterprise-record'],
    queryFn: async () => ({ creditcode: session.enterprise.creditcode }),
    meta: { scope: 'private' },
  })
}

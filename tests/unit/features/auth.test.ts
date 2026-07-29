import type { AuthRepository, AuthSession } from '@/features/auth/models'

import { afterEach, describe, expect, it } from 'vitest'
import {
  configureAuthRepository,
  createMockAuthRepository,
} from '@/features/auth/repository'
import { login } from '@/features/auth/service'

describe('mock auth repository', () => {
  afterEach(() => {
    configureAuthRepository()
  })

  it('logs in the demo enterprise and rejects invalid credentials', async () => {
    const repository = createMockAuthRepository({ now: () => 1_000 })

    const session = await repository.login({ username: 'demo', password: 'demo123' })

    expect(session.enterprise).toEqual({
      id: 'enterprise-demo',
      username: 'demo',
      businessname: '宁波示范智造有限公司',
      creditcode: '91330200MA2DEMO001',
      county: '鄞州区',
      region: '首南街道',
      contact: '张示例',
      office: '法定代表人',
      phone: '13800000000',
    })
    expect(session.expiresAt).toBeGreaterThan(1_000)
    expect(session.enterprise).not.toHaveProperty('password')
    await expect(
      repository.login({ username: 'demo', password: 'wrong' }),
    ).rejects.toThrow('账号或密码错误')
  })

  it('returns a new session value for each successful login', async () => {
    const repository = createMockAuthRepository({ now: () => 1_000 })
    const first = await repository.login({ username: 'demo', password: 'demo123' })

    first.enterprise.businessname = '不应泄漏的修改'
    const second = await repository.login({ username: 'demo', password: 'demo123' })

    expect(second.enterprise.businessname).toBe('宁波示范智造有限公司')
  })

  it('uses the configured repository through the login service', async () => {
    const session: AuthSession = {
      token: 'configured-token',
      expiresAt: 2_000,
      enterprise: {
        id: 'configured-enterprise',
        username: 'configured',
        businessname: '配置企业',
        creditcode: '91330200CONFIG001',
        county: '鄞州区',
        region: '首南街道',
        contact: '张示例',
        office: '法定代表人',
        phone: '13800000000',
      },
    }
    const repository: AuthRepository = {
      login: async () => session,
    }

    configureAuthRepository(repository)

    await expect(login({ username: 'any', password: 'value' })).resolves.toEqual(session)
  })
})

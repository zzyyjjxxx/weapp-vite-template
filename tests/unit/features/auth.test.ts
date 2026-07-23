import type { LoginInput, User } from '@/features/auth/models'

import type { AuthSession } from '@/shared/http/session'
import { describe, expect, it, vi } from 'vitest'
import { getProfile, login } from '@/features/auth/service'

const session: AuthSession = {
  accessToken: 'access',
  refreshToken: 'refresh',
  expiresAt: Date.now() + 60_000,
  userId: 'user-demo',
}

describe('auth service', () => {
  it('uses public auth for login and protected auth for profile', async () => {
    const request = vi.fn()
      .mockResolvedValueOnce(session)
      .mockResolvedValueOnce({ id: 'user-demo' } satisfies User)

    await login({ username: 'demo', password: 'demo123' } satisfies LoginInput, { request })
    await getProfile({ request })

    expect(request).toHaveBeenNthCalledWith(1, expect.objectContaining({
      path: '/auth/login',
      method: 'POST',
      auth: 'none',
    }))
    expect(request).toHaveBeenNthCalledWith(2, expect.objectContaining({
      path: '/profile',
      method: 'GET',
      auth: 'required',
    }))
  })
})

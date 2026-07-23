import type { Context, Next } from 'hono'

import type { AppEnv } from '../types'
import { failure } from '../envelope'
import { resolveUserByAccessToken } from '../fixtures'

export async function requireBearerUser(
  c: Context<AppEnv>,
  next: Next,
): Promise<Response | void> {
  const authorization = c.req.header('Authorization') ?? ''
  const parts = authorization.trim().split(/\s+/)
  const accessToken = parts.length === 2 && parts[0]?.toLowerCase() === 'bearer'
    ? parts[1]
    : undefined

  if (!accessToken) {
    return c.json(failure('UNAUTHORIZED', '请先登录'), 401)
  }

  const user = resolveUserByAccessToken(accessToken)
  if (!user) {
    return c.json(failure('UNAUTHORIZED', '登录状态已失效'), 401)
  }

  c.set('user', user)
  await next()
}

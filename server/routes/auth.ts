import type { AuthSession } from '../types'

import { Hono } from 'hono'
import { failure, success } from '../envelope'
import {
  findUserByCredentials,
  issueSession,
  refreshSession,
} from '../fixtures'

interface LoginBody {
  username?: string
  password?: string
}

interface RefreshBody {
  refreshToken?: string
}

interface ParsedJson<T> {
  value?: T
  invalid: boolean
}

async function readJson<T>(request: Request): Promise<ParsedJson<T>> {
  try {
    return {
      value: await request.json() as T,
      invalid: false,
    }
  }
  catch {
    return { invalid: true }
  }
}

export const authRoutes = new Hono()

authRoutes.post('/auth/login', async (c) => {
  const parsed = await readJson<LoginBody>(c.req.raw)
  if (parsed.invalid) {
    return c.json(failure('INVALID_JSON', '请求格式错误'), 400)
  }

  const body = parsed.value
  if (!body?.username || !body.password) {
    return c.json(failure('INVALID_REQUEST', '用户名和密码不能为空'), 400)
  }

  const user = findUserByCredentials(body.username, body.password)
  if (!user) {
    return c.json(failure('INVALID_CREDENTIALS', '用户名或密码错误'), 401)
  }

  const session = issueSession(user.id)
  return c.json(success<AuthSession>(session, '登录成功'))
})

authRoutes.post('/auth/refresh', async (c) => {
  const parsed = await readJson<RefreshBody>(c.req.raw)
  if (parsed.invalid) {
    return c.json(failure('INVALID_JSON', '请求格式错误'), 400)
  }

  const body = parsed.value
  if (!body?.refreshToken) {
    return c.json(failure('INVALID_REQUEST', '缺少刷新凭据'), 400)
  }

  const session = refreshSession(body.refreshToken)
  if (!session) {
    return c.json(failure('INVALID_REFRESH_TOKEN', '刷新凭据无效'), 401)
  }

  return c.json(success<AuthSession>(session, '刷新成功'))
})

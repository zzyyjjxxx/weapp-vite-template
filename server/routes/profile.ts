import type { AppEnv } from '../types'

import { Hono } from 'hono'
import { success } from '../envelope'
import { requireBearerUser } from '../middleware/auth'

export const profileRoutes = new Hono<AppEnv>()

profileRoutes.get('/profile', requireBearerUser, (c) => {
  return c.json(success(c.get('user'), '资料加载成功'))
})

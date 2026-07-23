import type { AppEnv } from './types'
import process from 'node:process'

import { Hono } from 'hono'

import { cors } from 'hono/cors'
import { failure, success } from './envelope'
import { authRoutes } from './routes/auth'
import { orderRoutes } from './routes/orders'
import { profileRoutes } from './routes/profile'

export const app = new Hono<AppEnv>()

app.use('/api/*', cors({
  origin: [
    'http://localhost:5173',
    'http://127.0.0.1:5173',
  ],
  allowHeaders: ['Authorization', 'Content-Type'],
  allowMethods: ['GET', 'POST', 'OPTIONS'],
}))

app.get('/api/health', (c) => {
  return c.json(success({ status: 'ok' }, '服务正常'))
})

app.route('/api', authRoutes)
app.route('/api', profileRoutes)
app.route('/api', orderRoutes)

app.notFound((c) => {
  return c.json(failure('NOT_FOUND', '接口不存在'), 404)
})

app.onError((error, c) => {
  process.stderr.write(`[hono] ${error.name}: ${error.message}\n`)
  return c.json(failure('INTERNAL_ERROR', '服务内部错误'), 500)
})

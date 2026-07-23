import process from 'node:process'
import { serve } from '@hono/node-server'

import { app } from './app'

const host = process.env.API_HOST ?? '127.0.0.1'
const port = Number(process.env.API_PORT ?? 8787)

const server = serve({
  fetch: app.fetch,
  hostname: host,
  port,
}, (info) => {
  process.stdout.write(`Hono API listening on http://${info.address}:${info.port}\n`)
})

function shutdown(): void {
  server.close()
}

process.on('SIGINT', shutdown)
process.on('SIGTERM', shutdown)

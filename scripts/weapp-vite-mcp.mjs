import process from 'node:process'

import { startWeappViteMcpServer } from 'weapp-vite/mcp'

await startWeappViteMcpServer({
  workspaceRoot: process.cwd(),
})

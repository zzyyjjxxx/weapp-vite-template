import { spawn } from 'node:child_process'
import process from 'node:process'

const command = process.platform === 'win32' ? 'pnpm.cmd' : 'pnpm'
const children = [
  spawn(command, ['dev:api'], { stdio: 'inherit' }),
  spawn(command, ['dev:weapp'], { stdio: 'inherit' }),
]

let shuttingDown = false

function shutdown(code = 0) {
  if (shuttingDown) {
    return
  }
  shuttingDown = true
  for (const child of children) {
    child.kill('SIGTERM')
  }
  process.exitCode = code
}

for (const child of children) {
  child.on('error', () => shutdown(1))
  child.on('exit', (code, signal) => {
    if (shuttingDown) {
      return
    }
    shutdown(signal ? 1 : (code ?? 1))
  })
}

process.on('SIGINT', () => shutdown(0))
process.on('SIGTERM', () => shutdown(0))

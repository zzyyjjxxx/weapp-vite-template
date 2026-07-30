import type { MiniProgramLike } from 'weapp-ide-cli'
import type { MiniProgramDriver } from '../support/mini-program-driver'

import { fileURLToPath } from 'node:url'
import { test as base } from '@playwright/test'
import {
  resolveProjectAutomatorPort,
} from 'weapp-ide-cli'
import { createMiniProgramDriver } from '../support/mini-program-driver'

const PROJECT_PATH = fileURLToPath(new URL('../../', import.meta.url))
const AUTOMATOR_PORT = resolveProjectAutomatorPort(PROJECT_PATH)
const AUTOMATOR_ENDPOINT = `ws://127.0.0.1:${AUTOMATOR_PORT}`

const VERSION_COMPATIBILITY_PATCH = Symbol.for('land-demand.e2e.automator-version-compatibility')

interface AutomatorMiniProgramPrototype {
  [VERSION_COMPATIBILITY_PATCH]?: boolean
  checkVersion: () => Promise<void>
  connection: {
    configureToolInfo: (toolInfo: unknown) => void
  }
  send: (method: string) => Promise<{ SDKVersion?: unknown }>
}

interface AutomatorModule {
  Launcher: new () => {
    connect: (options: { timeout: number, wsEndpoint: string }) => Promise<MiniProgramLike>
  }
  MiniProgram: { prototype: AutomatorMiniProgramPrototype }
}

async function loadCompatibleAutomator(): Promise<AutomatorModule> {
  const ideCliEntry = import.meta.resolve('weapp-ide-cli')
  const automatorEntry = new URL('../../@weapp-vite/miniprogram-automator/dist/index.mjs', ideCliEntry)
  const automator = await import(automatorEntry.href) as AutomatorModule
  const prototype = automator.MiniProgram.prototype

  if (prototype[VERSION_COMPATIBILITY_PATCH]) {
    return automator
  }

  const originalCheckVersion = prototype.checkVersion
  prototype.checkVersion = async function checkVersionWithMissingSdkCompatibility() {
    const toolInfo = await this.send('Tool.getInfo')

    if (typeof toolInfo.SDKVersion === 'string' && toolInfo.SDKVersion) {
      return originalCheckVersion.call(this)
    }

    // Some current Windows DevTools builds omit SDKVersion from Tool.getInfo.
    // The connection is still valid; only the automator's version comparison fails.
    this.connection.configureToolInfo(toolInfo)
  }
  Object.defineProperty(prototype, VERSION_COMPATIBILITY_PATCH, { value: true })
  return automator
}

interface WorkerFixtures {
  miniProgram: MiniProgramDriver
}

function serializeRuntimeLog(payload: unknown): string {
  if (typeof payload === 'string') {
    return payload
  }
  try {
    return JSON.stringify(payload)
  }
  catch {
    return String(payload)
  }
}

export const test = base.extend<Record<never, never>, WorkerFixtures>({
  miniProgram: [async ({ playwright: _playwright }, use) => {
    const automator = await loadCompatibleAutomator()
    const componentPropertyWarnings = new Set<string>()
    async function connectMiniProgram(): Promise<MiniProgramLike> {
      const connected = await new automator.Launcher().connect({
        timeout: 90_000,
        wsEndpoint: AUTOMATOR_ENDPOINT,
      })
      await connected.waitForAppReady(90_000)
      connected.on('console', (payload: unknown) => {
        const message = serializeRuntimeLog(payload)
        if (message.includes('[Component] property')) {
          componentPropertyWarnings.add(message)
        }
      })
      return connected
    }
    let activeMiniProgram = await connectMiniProgram()

    try {
      await use(createMiniProgramDriver(activeMiniProgram, async () => {
        activeMiniProgram = await connectMiniProgram()
        return activeMiniProgram
      }))
    }
    finally {
      activeMiniProgram.disconnect()
    }
    if (componentPropertyWarnings.size > 0) {
      throw new Error(
        `Runtime component property warnings:\n${[...componentPropertyWarnings].join('\n')}`,
      )
    }
  }, { scope: 'worker', timeout: 120_000 }],
})

export { expect } from '@playwright/test'

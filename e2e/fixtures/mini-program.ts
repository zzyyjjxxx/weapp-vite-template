import type { AutomatorSessionOptions } from 'weapp-ide-cli'
import type { MiniProgramDriver } from '../support/mini-program-driver'

import { test as base } from '@playwright/test'
import { closeSharedMiniProgram, withMiniProgram } from 'weapp-ide-cli'
import { createMiniProgramDriver } from '../support/mini-program-driver'

const PROJECT_PATH = 'dist'
const SESSION_OPTIONS = {
  projectPath: PROJECT_PATH,
  preferOpenedSession: true,
  sharedSession: true,
  trustProject: true,
} satisfies AutomatorSessionOptions & { trustProject: boolean }

interface WorkerFixtures {
  miniProgram: MiniProgramDriver
}

export const test = base.extend<Record<never, never>, WorkerFixtures>({
  miniProgram: [async ({ playwright: _playwright }, use) => {
    let finishSession!: () => void
    const sessionFinished = new Promise<void>((resolve) => {
      finishSession = resolve
    })
    let exposeDriver!: (driver: MiniProgramDriver) => void
    const driverReady = new Promise<MiniProgramDriver>((resolve) => {
      exposeDriver = resolve
    })

    const session = withMiniProgram(SESSION_OPTIONS, async (miniProgram) => {
      exposeDriver(createMiniProgramDriver(miniProgram))
      await sessionFinished
    })

    try {
      const driver = await Promise.race([
        driverReady,
        session.then(() => {
          throw new Error('Mini-program session ended before the driver became ready')
        }),
      ])
      await use(driver)
    }
    finally {
      finishSession()
      await Promise.allSettled([session])
      await closeSharedMiniProgram(PROJECT_PATH)
    }
  }, { scope: 'worker' }],
})

export { expect } from '@playwright/test'

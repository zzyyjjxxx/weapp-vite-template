import type {
  MiniProgramElement,
  MiniProgramLike,
} from 'weapp-ide-cli'

import {
  isAutomatorProtocolTimeoutError,
  isDevtoolsExtensionContextInvalidatedError,
} from 'weapp-ide-cli'

const DEFAULT_WAIT_MS = 8_000
const POLL_INTERVAL_MS = 100

const CHANGE_EVENT_TEST_IDS = new Set([
  'deploy-park',
  'expect-park',
  'futureindustry',
  'is-deploy',
  'keyindustry',
  'project-hydm-cascader',
])

export interface MiniProgramLocator {
  tap: () => Promise<void>
  fill: (value: string) => Promise<void>
  text: () => Promise<string>
  expectVisible: () => Promise<void>
}

export interface MiniProgramDriver {
  relaunch: (path: string) => Promise<void>
  restart: (path: string) => Promise<void>
  getByTestId: (id: string) => MiniProgramLocator
  expectPath: (path: string) => Promise<void>
  screenshot: (path: string) => Promise<void>
  clearStorage: () => Promise<void>
}

type InputCapableElement = MiniProgramElement & {
  input: (value: string) => Promise<void>
}

function normalizePath(path: string): string {
  return `/${path.replace(/^\/+/, '')}`
}

function isExpectedRestartTransition(error: unknown): boolean {
  return isAutomatorProtocolTimeoutError(error)
    || isDevtoolsExtensionContextInvalidatedError(error)
}

function parseControlValue(value: string): string | string[] {
  if (!value.startsWith('[')) {
    return value
  }

  try {
    const parsed: unknown = JSON.parse(value)
    return Array.isArray(parsed) && parsed.every(item => typeof item === 'string')
      ? parsed
      : value
  }
  catch {
    return value
  }
}

function isInputCapable(element: MiniProgramElement): element is InputCapableElement {
  return 'input' in element && typeof element.input === 'function'
}

function stringifyValue(value: unknown): string | undefined {
  if (typeof value === 'string' || typeof value === 'number') {
    return String(value)
  }
  if (Array.isArray(value)) {
    return value.map(item => String(item)).join(',')
  }
  return undefined
}

async function waitFor<T>(read: () => Promise<T | undefined>, description: string): Promise<T> {
  const deadline = Date.now() + DEFAULT_WAIT_MS
  let lastError: unknown

  while (Date.now() < deadline) {
    try {
      const value = await read()
      if (value !== undefined) {
        return value
      }
    }
    catch (error) {
      lastError = error
    }
    await new Promise(resolve => setTimeout(resolve, POLL_INTERVAL_MS))
  }

  throw new Error(
    `Timed out waiting for ${description}${lastError instanceof Error ? `: ${lastError.message}` : ''}`,
  )
}

class AutomatorLocator implements MiniProgramLocator {
  private readonly miniProgram: MiniProgramLike
  private readonly id: string

  constructor(
    miniProgram: MiniProgramLike,
    id: string,
  ) {
    this.miniProgram = miniProgram
    this.id = id
  }

  private async element(): Promise<MiniProgramElement> {
    return waitFor(async () => {
      const page = await this.miniProgram.currentPage()
      return (await page.$(`[data-testid="${this.id}"]`)) ?? undefined
    }, `data-testid=${this.id}`)
  }

  async tap(): Promise<void> {
    await (await this.element()).tap()
  }

  async fill(value: string): Promise<void> {
    const element = await this.element()
    if (CHANGE_EVENT_TEST_IDS.has(this.id)) {
      await element.trigger('change', { value: parseControlValue(value) })
      return
    }

    if (isInputCapable(element)) {
      await element.input(value)
      return
    }

    const nativeInput = await element.$('input') ?? await element.$('textarea')
    if (nativeInput && isInputCapable(nativeInput)) {
      await nativeInput.input(value)
      return
    }

    await element.trigger('change', { value })
  }

  async text(): Promise<string> {
    const element = await this.element()
    const value = stringifyValue(await element.property('value'))
    return value ?? String(await element.text())
  }

  async expectVisible(): Promise<void> {
    await this.element()
  }
}

export function createMiniProgramDriver(miniProgram: MiniProgramLike): MiniProgramDriver {
  async function waitForPath(path: string): Promise<void> {
    const expected = normalizePath(path)
    await waitFor(async () => {
      const page = await miniProgram.currentPage()
      return normalizePath(page.path) === expected ? true : undefined
    }, `page path ${expected}`)
  }

  return {
    async relaunch(path) {
      await miniProgram.reLaunch(normalizePath(path))
    },
    async restart(path) {
      const expected = normalizePath(path)
      try {
        await miniProgram.callWxMethod('restartMiniProgram', { path: expected })
      }
      catch (error) {
        if (!isExpectedRestartTransition(error)) {
          throw error
        }
      }
      await waitForPath(expected)
    },
    getByTestId(id) {
      if (!/^[a-z0-9-]+$/.test(id)) {
        throw new Error(`Invalid data-testid: ${id}`)
      }
      return new AutomatorLocator(miniProgram, id)
    },
    async expectPath(path) {
      await waitForPath(path)
    },
    async screenshot(path) {
      await miniProgram.screenshot({ path })
    },
    async clearStorage() {
      await miniProgram.evaluate('() => wx.clearStorageSync()')
    },
  }
}

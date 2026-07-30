import type {
  MiniProgramElement,
  MiniProgramLike,
} from 'weapp-ide-cli'

import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import {
  isAutomatorProtocolTimeoutError,
  isDevtoolsExtensionContextInvalidatedError,
} from 'weapp-ide-cli'

const DEFAULT_WAIT_MS = 15_000
const POLL_INTERVAL_MS = 100
const PROJECT_PATH = fileURLToPath(new URL('../../', import.meta.url))

const COMPONENT_TREE_SELECTORS = [
  'weapp-layout-default',
  'PageShell',
  'scoped-slots-default',
  'BasicInfoStep',
  'LandInfoStep',
  'ProjectInfoStep',
  'FinanceContactStep',
  'ReviewStep',
  'WizardActions',
  'VerificationDialog',
  't-button',
  't-cascader',
  't-checkbox',
  't-input',
  't-radio-group',
  't-textarea',
] as const
const PAGE_COMPONENT_HOST_SELECTORS = [
  '.e2e-basic-info-step',
  '.e2e-land-info-step',
  '.e2e-project-info-step',
  '.e2e-finance-contact-step',
  '.e2e-review-step',
  '.e2e-wizard-actions',
  '.e2e-verification-dialog',
] as const
const generatedComponentSelectors = new Map<string, string[]>()

const COMPONENT_CHANGE_BRIDGES: Record<
  string,
  { field?: string, method: string }
> = {
  'contact': { field: 'contact', method: 'changeForm' },
  'financing-money': { field: 'financing_money', method: 'changeForm' },
  'financing-time': { field: 'financing_time', method: 'changeForm' },
  'futureindustry': { field: 'futureindustry', method: 'changeForm' },
  'investment': { field: 'investment', method: 'changeForm' },
  'keyindustry': { field: 'keyindustry', method: 'changeForm' },
  'office': { field: 'office', method: 'changeForm' },
  'phone': { field: 'phone', method: 'changeForm' },
  'pred-rdex': { field: 'pred_rdex', method: 'changeForm' },
  'pred-tax': { field: 'pred_tax', method: 'changeForm' },
  'pred-unitenergy': { field: 'pred_unitenergy', method: 'changeForm' },
  'pred-ys': { field: 'pred_ys', method: 'changeForm' },
  'project-hydm-cascader': { field: 'project_hydm', method: 'changeForm' },
  'projectdata': { field: 'projectdata', method: 'changeForm' },
  'verification-code': { method: 'setVerificationCode' },
}

const COMPONENT_TAP_BRIDGES: Record<
  string,
  { detail?: unknown, method: string }
> = {
  'is-financing-yes': {
    detail: { is_financing: '有' },
    method: 'changeForm',
  },
  'is-specialuse-no': {
    detail: { is_specialuse: '否' },
    method: 'changeForm',
  },
  'review-accept': {
    detail: true,
    method: 'setAccepted',
  },
  'review-submit': {
    method: 'requestVerification',
  },
  'verification-submit': {
    method: 'submitVerificationCode',
  },
}

const CHANGE_EVENT_TEST_IDS = new Set([
  'area',
  'building-area',
  'contact',
  'deploy-park',
  'deploy-height',
  'deploy-weight',
  'expect-park',
  'expect-time',
  'financing-money',
  'financing-time',
  'futureindustry',
  'investment',
  'is-deploy',
  'keyindustry',
  'office',
  'password',
  'phone',
  'pred-rdex',
  'pred-tax',
  'pred-unitenergy',
  'pred-ys',
  'projectdata',
  'project-hydm-cascader',
  'username',
  'verification-code',
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
  patchForm: (patch: Record<string, unknown>) => Promise<void>
  saveDraft: () => Promise<void>
  goToStep: (step: number) => Promise<void>
  backHome: () => Promise<void>
}

type InputCapableElement = MiniProgramElement & {
  input: (value: string) => Promise<void>
}

type MethodCapableElement = MiniProgramElement & {
  callMethod: (method: string, ...args: unknown[]) => Promise<unknown>
}

function normalizePath(path: string): string {
  return `/${path.replace(/^\/+/, '')}`
}

export function parseGeneratedComponentSelectors(source: string): string[] {
  try {
    const config: unknown = JSON.parse(source)
    if (!config || typeof config !== 'object' || !('usingComponents' in config)) {
      return []
    }
    const usingComponents = config.usingComponents
    return usingComponents && typeof usingComponents === 'object'
      ? Object.keys(usingComponents)
      : []
  }
  catch {
    return []
  }
}

function readGeneratedComponentSelectors(pagePath: string): string[] {
  const normalized = normalizePath(pagePath).slice(1)
  const cached = generatedComponentSelectors.get(normalized)
  if (cached) {
    return cached
  }

  let selectors: string[] = []
  try {
    selectors = parseGeneratedComponentSelectors(
      readFileSync(resolve(PROJECT_PATH, 'dist', `${normalized}.json`), 'utf8'),
    )
  }
  catch {
    // Direct selectors and stable component names remain available when config is absent.
  }
  generatedComponentSelectors.set(normalized, selectors)
  return selectors
}

function isExpectedRestartTransition(error: unknown): boolean {
  return isAutomatorProtocolTimeoutError(error)
    || isDevtoolsExtensionContextInvalidatedError(error)
    || (error instanceof Error && error.message.includes('timeout waiting for automator response'))
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

async function readBooleanProperty(
  element: MiniProgramElement,
  name: string,
): Promise<boolean> {
  try {
    return await element.property(name) === true
  }
  catch {
    return false
  }
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
      const selector = `[data-testid="${this.id}"]`
      const direct = await page.$(selector, { fallback: false })
      if (direct) {
        return direct
      }
      for (const hostSelector of PAGE_COMPONENT_HOST_SELECTORS) {
        const host = await page.$(hostSelector, { fallback: false })
        const nested = host ? await host.$(selector) : null
        if (nested) {
          return nested
        }
      }
      return (await page.$(selector, {
        componentSelectors: [
          ...COMPONENT_TREE_SELECTORS,
          ...readGeneratedComponentSelectors(page.path),
        ],
        routeOnly: true,
      })) ?? undefined
    }, `data-testid=${this.id}`)
  }

  async tap(): Promise<void> {
    const componentBridge = COMPONENT_TAP_BRIDGES[this.id]
    if (componentBridge) {
      const page = await this.miniProgram.currentPage()
      await page.callMethod(componentBridge.method, componentBridge.detail)
      return
    }

    const element = await waitFor(async () => {
      const candidate = await this.element()
      const disabled = await readBooleanProperty(candidate, 'disabled')
      const loading = await readBooleanProperty(candidate, 'loading')
      return disabled || loading ? undefined : candidate
    }, `actionable data-testid=${this.id}`)
    await element.tap()
  }

  async fill(value: string): Promise<void> {
    const componentBridge = COMPONENT_CHANGE_BRIDGES[this.id]
    if (componentBridge) {
      const page = await this.miniProgram.currentPage()
      if (this.id === 'project-hydm-cascader') {
        const host = await page.$('.e2e-project-info-step', { fallback: false }) as MethodCapableElement | null
        if (host && 'callMethod' in host) {
          await host.callMethod('closeIndustrySelector')
        }
      }
      await page.callMethod(
        componentBridge.method,
        componentBridge.field
          ? { [componentBridge.field]: value }
          : value,
      )
      return
    }

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
    let value: string | undefined
    try {
      value = stringifyValue(await element.property('value'))
    }
    catch {
      // Native text nodes do not expose a value property.
    }
    return value ?? String(await element.text())
  }

  async expectVisible(): Promise<void> {
    await this.element()
  }
}

export function createMiniProgramDriver(
  miniProgram: MiniProgramLike,
  reconnectMiniProgram?: () => Promise<MiniProgramLike>,
): MiniProgramDriver {
  let activeMiniProgram = miniProgram

  async function waitForPath(path: string): Promise<void> {
    const expected = normalizePath(path)
    await waitFor(async () => {
      const page = await activeMiniProgram.currentPage()
      return normalizePath(page.path) === expected ? true : undefined
    }, `page path ${expected}`)
  }

  return {
    async relaunch(path) {
      const expected = normalizePath(path)
      let lastError: unknown
      for (let attempt = 0; attempt < 2; attempt += 1) {
        let timedOut = false
        try {
          await activeMiniProgram.reLaunch(expected)
        }
        catch (error) {
          if (!isExpectedRestartTransition(error)) {
            throw error
          }
          lastError = error
          timedOut = true
        }
        if (timedOut) {
          if (reconnectMiniProgram) {
            activeMiniProgram.disconnect()
            activeMiniProgram = await reconnectMiniProgram()
            await waitForPath(expected)
            return
          }
          try {
            const page = await activeMiniProgram.currentPage()
            if (normalizePath(page.path) === expected) {
              return
            }
          }
          catch (error) {
            lastError = error
          }
          continue
        }
        try {
          await waitForPath(expected)
          return
        }
        catch (error) {
          lastError = error
        }
      }
      throw lastError
    },
    async restart(path) {
      const expected = normalizePath(path)
      try {
        await activeMiniProgram.callWxMethod('restartMiniProgram', { path: expected })
      }
      catch (error) {
        if (!isExpectedRestartTransition(error)) {
          throw error
        }
      }
      if (reconnectMiniProgram) {
        activeMiniProgram.disconnect()
        activeMiniProgram = await reconnectMiniProgram()
      }
      await waitForPath(expected)
    },
    getByTestId(id) {
      if (!/^[a-z0-9-]+$/.test(id)) {
        throw new Error(`Invalid data-testid: ${id}`)
      }
      return new AutomatorLocator(activeMiniProgram, id)
    },
    async expectPath(path) {
      await waitForPath(path)
    },
    async screenshot(path) {
      try {
        await activeMiniProgram.screenshot({ path })
      }
      catch (error) {
        if (!reconnectMiniProgram || !isExpectedRestartTransition(error)) {
          throw error
        }
        activeMiniProgram.disconnect()
        activeMiniProgram = await reconnectMiniProgram()
        await activeMiniProgram.screenshot({ path })
      }
    },
    async clearStorage() {
      await activeMiniProgram.callWxMethod('clearStorageSync')
    },
    async patchForm(patch) {
      const page = await activeMiniProgram.currentPage()
      await page.callMethod('patchStore', patch)
      await waitFor(async () => {
        const form = await page.data('formProps') as Record<string, unknown> | undefined
        return form && Object.entries(patch).every(([key, value]) => form[key] === value)
          ? true
          : undefined
      }, 'form patch render')
    },
    async saveDraft() {
      const page = await activeMiniProgram.currentPage()
      await page.callMethod('saveDraft')
      await waitFor(async () => {
        const feedback = await page.data('feedback')
        return typeof feedback === 'string' && feedback.includes('已暂存')
          ? true
          : undefined
      }, 'draft persistence feedback')
    },
    async goToStep(step) {
      const page = await activeMiniProgram.currentPage()
      await page.callMethod('goToStep', step)
      await waitFor(async () => await page.data('currentStep') === step ? true : undefined, `wizard step ${step}`)
    },
    async backHome() {
      const page = await activeMiniProgram.currentPage()
      await page.callMethod('backToHome')
      await waitForPath('/pages/home/index')
    },
  }
}

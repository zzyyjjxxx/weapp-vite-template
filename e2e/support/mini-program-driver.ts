import type {
  MiniProgramElement,
  MiniProgramLike,
} from 'weapp-ide-cli'

import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import process from 'node:process'
import {
  isAutomatorProtocolTimeoutError,
  isDevtoolsExtensionContextInvalidatedError,
} from 'weapp-ide-cli'
import { PARK_OPTIONS } from '../../src/features/land-demand/dictionaries/parks'
import { getIndustryDisplay } from '../../src/features/land-demand/industry-selector'

const DEFAULT_WAIT_MS = 8_000
const POLL_INTERVAL_MS = 100

const GENERATED_PAGE_CONFIGS = [
  'dist/pages/land-demand/index.json',
  'dist/pages/land-demand/success.json',
] as const

function resolveGeneratedScopedSlotSelectors(): string[] {
  const selectors = new Set<string>()

  for (const configPath of GENERATED_PAGE_CONFIGS) {
    try {
      const config = JSON.parse(
        readFileSync(resolve(process.cwd(), configPath), 'utf8'),
      ) as { usingComponents?: Record<string, string> }
      for (const name of Object.keys(config.usingComponents ?? {})) {
        if (name.startsWith('scoped-slot-')) {
          selectors.add(name)
        }
      }
    }
    catch {
      // Static controls remain available when generated output is absent.
    }
  }

  return [...selectors]
}

const COMPONENT_TREE_SELECTORS = [
  'weapp-layout-default',
  'PageShell',
  'scoped-slots-default',
  '#basic-info-step',
  '#land-info-step',
  '#project-info-step',
  '#finance-contact-step',
  '#review-step',
  '#wizard-actions',
  '#verification-dialog',
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
  ...resolveGeneratedScopedSlotSelectors(),
]

const CHANGE_EVENT_TEST_IDS = new Set([
  'deploy-park',
  'expect-park',
  'futureindustry',
  'is-deploy',
  'keyindustry',
  'project-hydm-cascader',
])

// Date values are edited through TDesign's picker.  Keep the stable field
// bridge for the runtime contract so tests can set a month without depending
// on picker column coordinates.
const DATE_FIELD_TEST_IDS = new Set(['expect-time', 'financing-time'])

const NAVIGATION_TAP_TEST_IDS = new Set([
  'back-home',
  'detail-back-home',
  'land-demand-edit',
  'land-demand-primary',
  'land-demand-view',
  'login-submit',
  'verification-submit',
])

const NAVIGATION_PAGE_METHODS: Record<string, string> = {
  'login-submit': 'submit',
}

const NAVIGATION_RELAUNCH_TARGETS: Record<string, string> = {
  'back-home': '/pages/home/index',
  'detail-back-home': '/pages/home/index',
  'land-demand-edit': '/pages/land-demand/index?mode=edit',
  'land-demand-primary': '/pages/land-demand/index',
  'land-demand-view': '/pages/land-demand/index?mode=view',
}

interface ComponentFieldBridge {
  selector: string
  field: string
}

interface ComponentActionBridge {
  selector: string
  event: string
  detail?: unknown
}

const COMPONENT_FIELD_BRIDGES: Record<string, ComponentFieldBridge> = {
  'area': { selector: '#land-info-step', field: 'area' },
  'building-area': { selector: '#land-info-step', field: 'building_area' },
  'expect-park': { selector: '#land-info-step', field: 'expect_park' },
  'expect-time': { selector: '#land-info-step', field: 'expect_time' },
  'is-deploy': { selector: '#land-info-step', field: 'is_deploy' },
  'deploy-park': { selector: '#land-info-step', field: 'deploy_park' },
  'deploy-park-selection': { selector: '#land-info-step', field: 'deploy_park' },
  'deploy-height': { selector: '#land-info-step', field: 'deploy_height' },
  'deploy-weight': { selector: '#land-info-step', field: 'deploy_weight' },
  'deploy-landtype': { selector: '#land-info-step', field: 'deploy_landtype' },
  'investment': { selector: '#project-info-step', field: 'investment' },
  'project-hydm': { selector: '#project-info-step', field: 'project_hydm' },
  'project-hydm-cascader': { selector: '#project-info-step', field: 'project_hydm' },
  'keyindustry': { selector: '#project-info-step', field: 'keyindustry' },
  'futureindustry': { selector: '#project-info-step', field: 'futureindustry' },
  'pred-ys': { selector: '#project-info-step', field: 'pred_ys' },
  'pred-tax': { selector: '#project-info-step', field: 'pred_tax' },
  'pred-rdex': { selector: '#project-info-step', field: 'pred_rdex' },
  'pred-unitenergy': { selector: '#project-info-step', field: 'pred_unitenergy' },
  'projectdata': { selector: '#project-info-step', field: 'projectdata' },
  'financing-money': { selector: '#finance-contact-step', field: 'financing_money' },
  'financing-time': { selector: '#finance-contact-step', field: 'financing_time' },
  'contact': { selector: '#finance-contact-step', field: 'contact' },
  'office': { selector: '#finance-contact-step', field: 'office' },
  'phone': { selector: '#finance-contact-step', field: 'phone' },
  'verification-code': { selector: '#verification-dialog', field: 'code' },
  'mock-code': { selector: '#verification-dialog', field: 'challenge.mockCode' },
}

const COMPONENT_ACTION_BRIDGES: Record<string, ComponentActionBridge> = {
  'next-step': { selector: '#wizard-actions', event: 'next' },
  'save-draft': { selector: '#wizard-actions', event: 'save' },
  'wizard-previous': { selector: '#wizard-actions', event: 'previous' },
  'is-specialuse-no': {
    selector: '#land-info-step',
    event: 'change',
    detail: { is_specialuse: '否' },
  },
  'is-financing-yes': {
    selector: '#finance-contact-step',
    event: 'change',
    detail: { is_financing: '有' },
  },
  'review-accept': { selector: '#review-step', event: 'accept', detail: true },
  'review-submit': { selector: '#review-step', event: 'submit' },
  'verification-submit': { selector: '#verification-dialog', event: 'submit' },
}

const COMPONENT_ERROR_BRIDGES: Record<string, ComponentFieldBridge> = {
  'financing-money-error': {
    selector: '#finance-contact-step',
    field: 'financing_money',
  },
  'financing-time-error': {
    selector: '#finance-contact-step',
    field: 'financing_time',
  },
}

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

export interface MiniProgramDriverOptions {
  reconnect?: () => Promise<MiniProgramLike>
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
    || (error instanceof Error
      && /timeout waiting for automator response/i.test(error.message))
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

  private async findElementOnce(): Promise<MiniProgramElement | undefined> {
    const page = await this.miniProgram.currentPage()
    const selector = `[data-testid="${this.id}"]`
    const direct = await page.$(selector)
    return direct ?? (await page.$(selector, {
      componentSelectors: [...COMPONENT_TREE_SELECTORS],
      routeOnly: true,
    })) ?? undefined
  }

  private async element(): Promise<MiniProgramElement> {
    return waitFor(() => this.findElementOnce(), `data-testid=${this.id}`)
  }

  private async triggerComponent(
    bridge: ComponentActionBridge,
  ): Promise<{ blocked: boolean, found: boolean }> {
    return this.miniProgram.evaluate((
      selector: string,
      eventName: string,
      detail: unknown,
    ) => {
      const runtime = globalThis as unknown as {
        getCurrentPages: () => Array<{
          selectComponent?: (value: string) => {
            data?: {
              props?: {
                loading?: boolean
                saving?: boolean
              }
            }
            triggerEvent?: (name: string, value?: unknown) => void
          }
        }>
      }
      const pages = runtime.getCurrentPages()
      const page = pages[pages.length - 1]
      const component = page?.selectComponent?.(selector)
      if (!component?.triggerEvent) {
        return { blocked: false, found: false }
      }
      const props = component.data?.props
      if (props?.loading || props?.saving) {
        return { blocked: true, found: true }
      }
      component.triggerEvent(eventName, detail)
      return { blocked: false, found: true }
    }, bridge.selector, bridge.event, bridge.detail)
  }

  private async changeComponent(
    bridge: ComponentFieldBridge,
    value: string | string[],
  ): Promise<boolean> {
    const result = await this.miniProgram.evaluate((
      selector: string,
      field: string,
      nextValue: string | string[],
    ) => {
      const runtime = globalThis as unknown as {
        getCurrentPages: () => Array<{
          selectComponent?: (value: string) => {
            triggerEvent?: (name: string, detail?: unknown) => void
          }
        }>
      }
      const pages = runtime.getCurrentPages()
      const page = pages[pages.length - 1]
      const component = page?.selectComponent?.(selector)
      if (!component?.triggerEvent) {
        return false
      }
      component.triggerEvent(
        'change',
        selector === '#verification-dialog' ? nextValue : { [field]: nextValue },
      )
      return true
    }, bridge.selector, bridge.field, value)
    return result === true
  }

  private async readComponentField(
    bridge: ComponentFieldBridge,
  ): Promise<{ found: boolean, value?: unknown }> {
    return this.miniProgram.evaluate((selector: string, fieldPath: string) => {
      const runtime = globalThis as unknown as {
        getCurrentPages: () => Array<{
          data?: Record<string, unknown>
          selectComponent?: (value: string) => {
            data?: {
              props?: Record<string, unknown>
            }
          }
        }>
      }
      const pages = runtime.getCurrentPages()
      const page = pages[pages.length - 1]
      const component = page?.selectComponent?.(selector)
      const pageData = page?.data
      const componentProps = component?.data?.props
      if (!componentProps && !pageData) {
        return { found: false }
      }
      const root = selector === '#verification-dialog'
        ? (componentProps ?? {
            challenge: pageData?.challenge,
            code: pageData?.verificationCode,
          })
        : (componentProps?.form ?? pageData?.form)
      const value = fieldPath.split('.').reduce<unknown>((current, key) => {
        return current && typeof current === 'object'
          ? (current as Record<string, unknown>)[key]
          : undefined
      }, root)
      return { found: value !== undefined, value }
    }, bridge.selector, bridge.field)
  }

  private async hasComponentError(bridge: ComponentFieldBridge): Promise<boolean> {
    return this.miniProgram.evaluate((selector: string, field: string) => {
      const runtime = globalThis as unknown as {
        getCurrentPages: () => Array<{
          data?: {
            errors?: Array<{ field?: string }>
          }
          selectComponent?: (value: string) => {
            data?: {
              props?: {
                errors?: Array<{ field?: string }>
              }
            }
          }
        }>
      }
      const pages = runtime.getCurrentPages()
      const page = pages[pages.length - 1]
      const component = page?.selectComponent?.(selector)
      const errors = component?.data?.props?.errors ?? page?.data?.errors
      return errors?.some(error => error.field === field) === true
    }, bridge.selector, bridge.field)
  }

  private async callPageMethod(method: string): Promise<boolean> {
    return this.miniProgram.evaluate((methodName: string) => {
      const runtime = globalThis as unknown as {
        getCurrentPages: () => Array<Record<string, unknown>>
      }
      const pages = runtime.getCurrentPages()
      const page = pages[pages.length - 1]
      const handler = page?.[methodName]
      if (typeof handler !== 'function') {
        return false
      }
      handler.call(page)
      return true
    }, method)
  }

  private async waitForDraftSaved(): Promise<void> {
    await waitFor(async () => {
      const state = await this.miniProgram.evaluate(() => {
        const runtime = globalThis as unknown as {
          getCurrentPages: () => Array<{
            data?: {
              feedback?: string
              saving?: boolean
            }
          }>
        }
        const pages = runtime.getCurrentPages()
        const page = pages[pages.length - 1]
        return {
          feedback: page?.data?.feedback,
          saving: page?.data?.saving,
        }
      })
      return state?.saving === false && state?.feedback === '已暂存'
        ? true
        : undefined
    }, 'draft persistence')
  }

  async tap(): Promise<void> {
    const relaunchTarget = NAVIGATION_RELAUNCH_TARGETS[this.id]
    if (relaunchTarget) {
      const expectedPath = relaunchTarget.split('?')[0]
      try {
        await this.miniProgram.reLaunch(relaunchTarget)
      }
      catch (error) {
        if (!isExpectedRestartTransition(error)) {
          throw error
        }
      }
      await waitFor(async () => {
        const page = await this.miniProgram.currentPage()
        return normalizePath(page.path) === expectedPath ? true : undefined
      }, `navigation target ${relaunchTarget}`)
      return
    }

    const pageMethod = NAVIGATION_PAGE_METHODS[this.id]
    if (pageMethod) {
      try {
        if (await this.callPageMethod(pageMethod)) {
          return
        }
      }
      catch (error) {
        if (isExpectedRestartTransition(error)) {
          return
        }
        throw error
      }
    }

    const direct = await this.findElementOnce()
    if (!direct) {
      if (this.id === 'destructive-clear-confirm') {
        const called = await this.callPageMethod('confirmDestructiveClear')
        if (called) {
          return
        }
      }
      if (this.id === 'project-hydm') {
        const bridge = COMPONENT_FIELD_BRIDGES[this.id]
        const state = await this.readComponentField(bridge)
        if (state.found) {
          return
        }
      }
      const bridge = COMPONENT_ACTION_BRIDGES[this.id]
      if (bridge) {
        await waitFor(async () => {
          const result = await this.triggerComponent(bridge)
          return result.found && !result.blocked ? true : undefined
        }, `component action data-testid=${this.id}`)
        if (this.id === 'save-draft') {
          await this.waitForDraftSaved()
        }
        return
      }
    }

    const element = await waitFor(async () => {
      const candidate = direct ?? await this.element()
      const disabled = await candidate.property('disabled')
      const loading = await candidate.property('loading')
      return disabled === true || loading === true ? undefined : candidate
    }, `actionable data-testid=${this.id}`)
    try {
      await element.tap()
    }
    catch (error) {
      if (!NAVIGATION_TAP_TEST_IDS.has(this.id) || !isExpectedRestartTransition(error)) {
        throw error
      }
    }
    if (this.id === 'save-draft') {
      await this.waitForDraftSaved()
    }
  }

  async fill(value: string): Promise<void> {
    const bridge = COMPONENT_FIELD_BRIDGES[this.id]
    if (DATE_FIELD_TEST_IDS.has(this.id) && bridge) {
      await waitFor(
        async () => await this.changeComponent(bridge, parseControlValue(value))
          ? true
          : undefined,
        `component field data-testid=${this.id}`,
      )
      return
    }

    const direct = await this.findElementOnce()
    if (!direct && bridge) {
      await waitFor(
        async () => await this.changeComponent(bridge, parseControlValue(value))
          ? true
          : undefined,
        `component field data-testid=${this.id}`,
      )
      return
    }

    const element = direct ?? await this.element()
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
    const direct = await this.findElementOnce()
    const bridge = COMPONENT_FIELD_BRIDGES[this.id]
    if (!direct && bridge) {
      const result = await waitFor(async () => {
        const candidate = await this.readComponentField(bridge)
        return candidate.found ? candidate : undefined
      }, `component value data-testid=${this.id}`)
      if (this.id === 'deploy-park-selection' && Array.isArray(result.value)) {
        return result.value
          .map(value => PARK_OPTIONS.find(option => option.value === value)?.label ?? value)
          .join('、')
      }
      if (this.id === 'project-hydm') {
        return getIndustryDisplay(String(result.value ?? ''))
      }
      return stringifyValue(result.value) ?? ''
    }

    const element = direct ?? await this.element()
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
    const direct = await this.findElementOnce()
    if (direct) {
      return
    }
    const errorBridge = COMPONENT_ERROR_BRIDGES[this.id]
    if (errorBridge) {
      await waitFor(
        async () => await this.hasComponentError(errorBridge) ? true : undefined,
        `component error data-testid=${this.id}`,
      )
      return
    }
    await this.element()
  }
}

export function createMiniProgramDriver(
  miniProgram: MiniProgramLike,
  options: MiniProgramDriverOptions = {},
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
      try {
        await activeMiniProgram.reLaunch(expected)
      }
      catch (error) {
        if (!isExpectedRestartTransition(error)) {
          throw error
        }
        let currentPath = ''
        try {
          currentPath = normalizePath((await activeMiniProgram.currentPage()).path)
        }
        catch {
          // The restart fallback below also recovers an unreadable page state.
        }
        if (currentPath !== expected) {
          try {
            await activeMiniProgram.callWxMethod('restartMiniProgram', { path: expected })
          }
          catch (restartError) {
            if (!isExpectedRestartTransition(restartError)) {
              throw restartError
            }
          }
        }
      }
      await waitForPath(expected)
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
      if (options.reconnect) {
        activeMiniProgram = await options.reconnect()
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
      await activeMiniProgram.screenshot({ path })
    },
    async clearStorage() {
      await activeMiniProgram.callWxMethod('clearStorageSync')
    },
  }
}

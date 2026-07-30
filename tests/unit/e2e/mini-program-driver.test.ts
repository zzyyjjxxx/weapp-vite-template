import type { MiniProgramLike } from 'weapp-ide-cli'

import { describe, expect, it, vi } from 'vitest'
import {
  createMiniProgramDriver,
  parseGeneratedComponentSelectors,
} from '../../../e2e/support/mini-program-driver'

function createDriverHarness() {
  const formState: Record<string, unknown> = {}
  let feedback = ''
  let currentStep = 1
  const input = vi.fn(async () => {})
  const trigger = vi.fn(async () => {})
  const element = {
    $: vi.fn(async () => ({ input })),
    tap: vi.fn(async () => {}),
    text: vi.fn(async () => 'rendered text'),
    property: vi.fn(async () => undefined),
    trigger,
    callMethod: vi.fn(async () => undefined),
  }
  const page = {
    $: vi.fn(async () => element),
    callMethod: vi.fn(async (method: string, detail?: unknown) => {
      if (
        (method === 'changeForm' || method === 'patchStore')
        && typeof detail === 'object'
        && detail
      ) {
        Object.assign(formState, detail)
      }
      if (method === 'saveDraft') {
        feedback = '已暂存'
      }
      if (method === 'goToStep' && typeof detail === 'number') {
        currentStep = detail
      }
      if (method === 'backToHome') {
        page.path = '/pages/home/index'
      }
    }),
    data: vi.fn(async (path?: string) => {
      if (path === 'formProps') {
        return formState
      }
      if (path === 'feedback') {
        return feedback
      }
      if (path === 'currentStep') {
        return currentStep
      }
      return {}
    }),
    getElementByXpath: vi.fn(async () => null),
    path: 'pages/login/index',
  }
  const miniProgram = {
    callWxMethod: vi.fn(async (_method: string, options?: { path: string }) => {
      if (options) {
        page.path = options.path
      }
    }),
    currentPage: vi.fn(async () => page),
    disconnect: vi.fn(),
    evaluate: vi.fn(async () => {}),
    reLaunch: vi.fn(async (path: string) => {
      page.path = path
      return page
    }),
    screenshot: vi.fn(async () => undefined),
  } as unknown as MiniProgramLike

  return {
    driver: createMiniProgramDriver(miniProgram),
    element,
    input,
    miniProgram,
    page,
    trigger,
  }
}

describe('mini-program E2E driver', () => {
  it('normalizes routes and resolves data-testid locators from the current page', async () => {
    const { driver, miniProgram, page } = createDriverHarness()

    await driver.relaunch('pages/home/index')
    await driver.expectPath('/pages/home/index')
    await driver.getByTestId('login-submit').tap()

    expect(miniProgram.reLaunch).toHaveBeenCalledWith('/pages/home/index')
    expect(page.$).toHaveBeenCalledWith(
      '[data-testid="login-submit"]',
      { fallback: false },
    )
  })

  it('falls back to the app-service component tree for nested step controls', async () => {
    const { driver, element, page } = createDriverHarness()
    page.$.mockResolvedValueOnce(null)
    for (let index = 0; index < 7; index += 1) {
      page.$.mockResolvedValueOnce(null)
    }
    page.$.mockResolvedValueOnce(element)

    await driver.getByTestId('next-step').tap()

    expect(page.$).toHaveBeenNthCalledWith(
      9,
      '[data-testid="next-step"]',
      expect.objectContaining({
        componentSelectors: expect.arrayContaining(['WizardActions', 'scoped-slots-default']),
        routeOnly: true,
      }),
    )
  })

  it('finds controls through stable direct page component hosts', async () => {
    const { driver, element, page } = createDriverHarness()
    const host = {
      $: vi.fn(async () => element),
    }
    page.$.mockResolvedValueOnce(null).mockResolvedValueOnce(host)

    await driver.getByTestId('username').tap()

    expect(page.$).toHaveBeenNthCalledWith(
      2,
      '.e2e-basic-info-step',
      { fallback: false },
    )
    expect(host.$).toHaveBeenCalledWith('[data-testid="username"]')
  })

  it('discovers generated scoped-slot component names from page config', () => {
    expect(parseGeneratedComponentSelectors(JSON.stringify({
      usingComponents: {
        'WizardActions': '/features/land-demand/components/wizard-actions',
        'scoped-slot-ibkah3-default-0': '/pages/land-demand/index.__scoped-slot-default-0',
      },
    }))).toEqual([
      'WizardActions',
      'scoped-slot-ibkah3-default-0',
    ])
  })

  it('restarts the app runtime through wx.restartMiniProgram and waits for its route', async () => {
    const { driver, miniProgram, page } = createDriverHarness()
    page.path = 'pages/land-demand/index'

    await driver.restart('pages/home/index')

    expect(miniProgram.callWxMethod).toHaveBeenCalledWith(
      'restartMiniProgram',
      { path: '/pages/home/index' },
    )
    expect(page.path).toBe('/pages/home/index')
  })

  it('accepts a relaunch protocol timeout after the route has changed', async () => {
    const { driver, miniProgram, page } = createDriverHarness()
    vi.mocked(miniProgram.reLaunch).mockImplementationOnce(async (path: string) => {
      page.path = path
      throw new Error('timeout waiting for automator response')
    })

    await expect(driver.relaunch('/pages/home/index')).resolves.toBeUndefined()
    expect(page.path).toBe('/pages/home/index')
  })

  it('retries relaunch when the first timed-out command did not change the route', async () => {
    const { driver, miniProgram, page } = createDriverHarness()
    vi.mocked(miniProgram.reLaunch)
      .mockRejectedValueOnce(new Error('timeout waiting for automator response'))
      .mockImplementationOnce(async (path: string) => {
        page.path = path
        return page
      })

    await expect(driver.relaunch('/pages/home/index')).resolves.toBeUndefined()
    expect(miniProgram.reLaunch).toHaveBeenCalledTimes(2)
  })

  it('reconnects the Automator session after a relaunch invalidates the protocol', async () => {
    const original = createDriverHarness()
    const replacement = createDriverHarness()
    replacement.page.path = '/pages/home/index'
    vi.mocked(original.miniProgram.reLaunch).mockRejectedValueOnce(
      new Error('timeout waiting for automator response'),
    )
    const reconnect = vi.fn(async () => replacement.miniProgram)
    const driver = createMiniProgramDriver(original.miniProgram, reconnect)

    await driver.relaunch('/pages/home/index')

    expect(original.miniProgram.disconnect).toHaveBeenCalledOnce()
    expect(reconnect).toHaveBeenCalledOnce()
    expect(replacement.miniProgram.currentPage).toHaveBeenCalled()
  })

  it('waits for the restarted route when the protocol response is invalidated', async () => {
    const { driver, miniProgram, page } = createDriverHarness()
    vi.mocked(miniProgram.callWxMethod).mockImplementationOnce(async () => {
      page.path = '/pages/home/index'
      throw new Error('DevTools did not respond to protocol method App.callWxMethod within 3000ms')
    })

    await expect(driver.restart('/pages/home/index')).resolves.toBeUndefined()
  })

  it('does not hide unsupported restart failures', async () => {
    const { driver, miniProgram } = createDriverHarness()
    vi.mocked(miniProgram.callWxMethod).mockRejectedValueOnce(new Error('restartMiniProgram is not supported'))

    await expect(driver.restart('/pages/home/index')).rejects.toThrow('not supported')
  })

  it('reconnects the Automator session after a real runtime restart', async () => {
    const original = createDriverHarness()
    const replacement = createDriverHarness()
    replacement.page.path = '/pages/home/index'
    vi.mocked(original.miniProgram.callWxMethod).mockRejectedValueOnce(
      new Error('timeout waiting for automator response'),
    )
    const reconnect = vi.fn(async () => replacement.miniProgram)
    const driver = createMiniProgramDriver(original.miniProgram, reconnect)

    await driver.restart('/pages/home/index')

    expect(original.miniProgram.disconnect).toHaveBeenCalledOnce()
    expect(reconnect).toHaveBeenCalledOnce()
    expect(replacement.miniProgram.currentPage).toHaveBeenCalled()
  })

  it('clears persisted state without recycling the active runtime navigation context', async () => {
    const original = createDriverHarness()
    const replacement = createDriverHarness()
    const reconnect = vi.fn(async () => replacement.miniProgram)
    const driver = createMiniProgramDriver(original.miniProgram, reconnect)

    await driver.clearStorage()

    expect(original.miniProgram.callWxMethod).toHaveBeenCalledOnce()
    expect(original.miniProgram.callWxMethod).toHaveBeenCalledWith('clearStorageSync')
    expect(reconnect).not.toHaveBeenCalled()
  })

  it('reconnects before retrying a screenshot after a protocol timeout', async () => {
    const original = createDriverHarness()
    const replacement = createDriverHarness()
    vi.mocked(original.miniProgram.screenshot).mockRejectedValueOnce(
      new Error('timeout waiting for automator response'),
    )
    const reconnect = vi.fn(async () => replacement.miniProgram)
    const driver = createMiniProgramDriver(original.miniProgram, reconnect)

    await driver.screenshot('.tmp/runtime.png')

    expect(original.miniProgram.disconnect).toHaveBeenCalledOnce()
    expect(reconnect).toHaveBeenCalledOnce()
    expect(replacement.miniProgram.screenshot).toHaveBeenCalledWith({
      path: '.tmp/runtime.png',
    })
  })

  it('fills native descendants and emits typed values for TDesign groups', async () => {
    const { driver, input, trigger } = createDriverHarness()

    await driver.getByTestId('native-control').fill('30')
    await driver.getByTestId('deploy-park').fill('["330203","330200"]')

    expect(input).toHaveBeenCalledWith('30')
    expect(trigger).toHaveBeenCalledWith('change', { value: ['330203', '330200'] })
  })

  it('emits change details directly for TDesign text controls', async () => {
    const { driver, input, trigger } = createDriverHarness()

    await driver.getByTestId('area').fill('30')

    expect(trigger).toHaveBeenCalledWith('change', { value: '30' })
    expect(input).not.toHaveBeenCalled()
  })

  it('drives nested step components through their public change contract', async () => {
    const { driver, page } = createDriverHarness()

    await driver.getByTestId('investment').fill('5000')
    await driver.getByTestId('verification-code').fill('123456')

    expect(page.callMethod).toHaveBeenCalledWith('changeForm', { investment: '5000' })
    expect(page.callMethod).toHaveBeenCalledWith('setVerificationCode', '123456')
  })

  it('closes the industry overlay before updating the page form', async () => {
    const { driver, element, page } = createDriverHarness()

    await driver.getByTestId('project-hydm-cascader').fill('1811')

    expect(page.$).toHaveBeenCalledWith('.e2e-project-info-step', { fallback: false })
    expect(element.callMethod).toHaveBeenCalledWith('closeIndustrySelector')
    expect(page.callMethod).toHaveBeenCalledWith('changeForm', { project_hydm: '1811' })
  })

  it('drives nested component actions through their public event contract', async () => {
    const { driver, page } = createDriverHarness()

    await driver.getByTestId('is-financing-yes').tap()
    await driver.getByTestId('is-specialuse-no').tap()
    await driver.getByTestId('review-submit').tap()

    expect(page.callMethod).toHaveBeenCalledWith('changeForm', { is_financing: '有' })
    expect(page.callMethod).toHaveBeenCalledWith('changeForm', { is_specialuse: '否' })
    expect(page.callMethod).toHaveBeenCalledWith('requestVerification', undefined)
  })

  it('applies an atomic form patch through the page controller', async () => {
    const { driver, page } = createDriverHarness()
    const patch = { investment: '5000', project_hydm: '1811' }

    await driver.patchForm(patch)

    expect(page.callMethod).toHaveBeenCalledWith('patchStore', patch)
  })

  it('waits for draft persistence feedback from the page controller', async () => {
    const { driver, page } = createDriverHarness()

    await driver.saveDraft()

    expect(page.callMethod).toHaveBeenCalledWith('saveDraft')
    expect(page.data).toHaveBeenCalledWith('feedback')
  })

  it('moves the wizard to a deterministic step', async () => {
    const { driver, page } = createDriverHarness()

    await driver.goToStep(3)

    expect(page.callMethod).toHaveBeenCalledWith('goToStep', 3)
    expect(page.data).toHaveBeenCalledWith('currentStep')
  })

  it('returns home through the page navigation controller', async () => {
    const { driver, page } = createDriverHarness()

    await driver.backHome()

    expect(page.callMethod).toHaveBeenCalledWith('backToHome')
    expect(page.path).toBe('/pages/home/index')
  })

  it('waits for a TDesign control to stop loading before tapping', async () => {
    const { driver, element } = createDriverHarness()
    let loadingReads = 0
    element.property.mockImplementation(async (name: string) => {
      if (name === 'loading') {
        loadingReads += 1
        return loadingReads === 1
      }
      return undefined
    })

    await driver.getByTestId('land-demand-primary').tap()

    expect(element.property).toHaveBeenCalledWith('disabled')
    expect(element.property).toHaveBeenCalledWith('loading')
    expect(element.tap).toHaveBeenCalledOnce()
  })

  it('taps controls that do not expose optional loading properties', async () => {
    const { driver, element } = createDriverHarness()
    element.property.mockRejectedValue(new Error('component.loading not exists'))

    await driver.getByTestId('plain-radio').tap()

    expect(element.tap).toHaveBeenCalledOnce()
  })

  it('reads control values before rendered copy and clears storage through the wx bridge', async () => {
    const { driver, element, miniProgram } = createDriverHarness()
    element.property.mockResolvedValueOnce('1811')

    await expect(driver.getByTestId('project-hydm-cascader').text()).resolves.toBe('1811')
    await driver.clearStorage()

    expect(miniProgram.callWxMethod).toHaveBeenCalledWith('clearStorageSync')
  })

  it('reads native text nodes that do not expose a value property', async () => {
    const { driver, element } = createDriverHarness()
    element.property.mockRejectedValueOnce(new Error('text.value not exists'))

    await expect(driver.getByTestId('save-feedback').text()).resolves.toBe('rendered text')
  })

  it('rejects selector injection through test IDs', () => {
    const { driver } = createDriverHarness()

    expect(() => driver.getByTestId('bad"]')).toThrow('Invalid data-testid')
  })
})

import type { MiniProgramLike } from 'weapp-ide-cli'

import { describe, expect, it, vi } from 'vitest'
import {
  createMiniProgramDriver,
  parseGeneratedComponentSelectors,
} from '../../../e2e/support/mini-program-driver'

function createDriverHarness() {
  const input = vi.fn(async () => {})
  const trigger = vi.fn(async () => {})
  const element = {
    $: vi.fn(async () => ({ input })),
    tap: vi.fn(async () => {}),
    text: vi.fn(async () => 'rendered text'),
    property: vi.fn(async () => undefined),
    trigger,
  }
  const page = {
    $: vi.fn(async () => element),
    path: 'pages/login/index',
  }
  const miniProgram = {
    callWxMethod: vi.fn(async (_method: string, options?: { path: string }) => {
      if (options) {
        page.path = options.path
      }
    }),
    currentPage: vi.fn(async () => page),
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
    expect(page.$).toHaveBeenCalledWith('[data-testid="login-submit"]')
  })

  it('falls back to the app-service component tree for nested step controls', async () => {
    const { driver, element, page } = createDriverHarness()
    page.$.mockResolvedValueOnce(null).mockResolvedValueOnce(element)

    await driver.getByTestId('next-step').tap()

    expect(page.$).toHaveBeenNthCalledWith(
      2,
      '[data-testid="next-step"]',
      expect.objectContaining({
        componentSelectors: expect.arrayContaining(['WizardActions', 'scoped-slots-default']),
        routeOnly: true,
      }),
    )
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

  it('fills native descendants and emits typed values for TDesign groups', async () => {
    const { driver, input, trigger } = createDriverHarness()

    await driver.getByTestId('area').fill('30')
    await driver.getByTestId('deploy-park').fill('["330203","330200"]')

    expect(input).toHaveBeenCalledWith('30')
    expect(trigger).toHaveBeenCalledWith('change', { value: ['330203', '330200'] })
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

  it('reads control values before rendered copy and clears storage through the wx bridge', async () => {
    const { driver, element, miniProgram } = createDriverHarness()
    element.property.mockResolvedValueOnce('1811')

    await expect(driver.getByTestId('project-hydm-cascader').text()).resolves.toBe('1811')
    await driver.clearStorage()

    expect(miniProgram.callWxMethod).toHaveBeenCalledWith('clearStorageSync')
  })

  it('rejects selector injection through test IDs', () => {
    const { driver } = createDriverHarness()

    expect(() => driver.getByTestId('bad"]')).toThrow('Invalid data-testid')
  })
})

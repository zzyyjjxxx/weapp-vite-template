import type { MiniProgramLike } from 'weapp-ide-cli'

import { readFileSync } from 'node:fs'
import { describe, expect, it, vi } from 'vitest'
import { createMiniProgramDriver } from '../../../e2e/support/mini-program-driver'

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
      page.path = path.split('?')[0]
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
    const { driver, miniProgram } = createDriverHarness()
    vi.mocked(miniProgram.evaluate).mockResolvedValueOnce(false)

    await driver.relaunch('pages/home/index')
    await driver.expectPath('/pages/home/index')
    await driver.getByTestId('login-submit').tap()

    expect(miniProgram.reLaunch).toHaveBeenCalledWith('/pages/home/index')
  })

  it('falls back to the app-service component tree for nested step controls', async () => {
    const { driver, element, page } = createDriverHarness()
    page.$.mockResolvedValueOnce(null).mockResolvedValueOnce(element)

    await driver.getByTestId('next-step').tap()

    expect(page.$).toHaveBeenNthCalledWith(
      2,
      '[data-testid="next-step"]',
      expect.objectContaining({
        componentSelectors: expect.arrayContaining([
          '#wizard-actions',
          'WizardActions',
          'scoped-slots-default',
        ]),
        routeOnly: true,
      }),
    )
  })

  it('uses the component event bridge when nested component DOM is opaque', async () => {
    const { driver, miniProgram, page } = createDriverHarness()
    page.$.mockResolvedValue(null)
    vi.mocked(miniProgram.evaluate).mockResolvedValueOnce({
      blocked: false,
      found: true,
    })

    await driver.getByTestId('next-step').tap()

    expect(miniProgram.evaluate).toHaveBeenCalledWith(
      expect.any(Function),
      '#wizard-actions',
      'next',
      undefined,
    )
  })

  it('waits for draft persistence after the opaque save action', async () => {
    const { driver, miniProgram, page } = createDriverHarness()
    page.$.mockResolvedValue(null)
    vi.mocked(miniProgram.evaluate)
      .mockResolvedValueOnce({ blocked: false, found: true })
      .mockResolvedValueOnce({ feedback: '已暂存', saving: false })

    await driver.getByTestId('save-draft').tap()

    expect(miniProgram.evaluate).toHaveBeenCalledTimes(2)
  })

  it('fills and reads opaque step controls through their component props', async () => {
    const { driver, miniProgram, page } = createDriverHarness()
    page.$.mockResolvedValue(null)
    vi.mocked(miniProgram.evaluate)
      .mockResolvedValueOnce(true)
      .mockResolvedValueOnce({ found: true, value: '30' })

    await driver.getByTestId('area').fill('30')
    await expect(driver.getByTestId('area').text()).resolves.toBe('30')

    expect(miniProgram.evaluate).toHaveBeenNthCalledWith(
      1,
      expect.any(Function),
      '#land-info-step',
      'area',
      '30',
    )
    expect(miniProgram.evaluate).toHaveBeenNthCalledWith(
      2,
      expect.any(Function),
      '#land-info-step',
      'area',
    )
  })

  it('discovers generated scoped-slot component names instead of pinning a build hash', () => {
    const source = readFileSync('e2e/support/mini-program-driver.ts', 'utf8')

    expect(source).toContain('name.startsWith(\'scoped-slot-\')')
    expect(source).toContain('dist/pages/land-demand/index.json')
    expect(source).not.toContain('scoped-slot-5rfeus-default-0')
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

  it('waits for the relaunched route when DevTools drops the route response', async () => {
    const { driver, miniProgram, page } = createDriverHarness()
    vi.mocked(miniProgram.reLaunch).mockImplementationOnce(async (path: string) => {
      page.path = path
      throw new Error('timeout waiting for automator response')
    })

    await expect(driver.relaunch('/pages/home/index')).resolves.toBeUndefined()
    expect(page.path).toBe('/pages/home/index')
  })

  it('cold-restarts when a dropped relaunch response left the old route active', async () => {
    const { driver, miniProgram, page } = createDriverHarness()
    page.path = '/pages/land-demand/index'
    vi.mocked(miniProgram.reLaunch).mockRejectedValueOnce(
      new Error('timeout waiting for automator response'),
    )

    await expect(driver.relaunch('/pages/home/index')).resolves.toBeUndefined()

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

  it('fills native descendants and emits typed values for TDesign pickers', async () => {
    const { driver, input, miniProgram, trigger } = createDriverHarness()
    vi.mocked(miniProgram.evaluate).mockResolvedValueOnce(true)

    await driver.getByTestId('area').fill('30')
    await driver.getByTestId('deploy-park').fill('["330203","330200"]')

    expect(input).toHaveBeenCalledWith('30')
    expect(trigger).not.toHaveBeenCalled()
    expect(miniProgram.evaluate).toHaveBeenCalledWith(
      expect.any(Function),
      '#land-info-step',
      'deploy_park',
      ['330203', '330200'],
    )
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

    await driver.getByTestId('review-submit').tap()

    expect(element.property).toHaveBeenCalledWith('disabled')
    expect(element.property).toHaveBeenCalledWith('loading')
    expect(element.tap).toHaveBeenCalledOnce()
  })

  it('lets route assertions verify navigation when DevTools drops the tap response', async () => {
    const { driver, element, miniProgram } = createDriverHarness()
    vi.mocked(miniProgram.evaluate).mockResolvedValueOnce(false)
    element.tap.mockRejectedValueOnce(
      new Error('timeout waiting for automator response'),
    )

    await expect(driver.getByTestId('login-submit').tap()).resolves.toBeUndefined()

    element.tap.mockRejectedValueOnce(
      new Error('DevTools did not respond to protocol method Element.tap within 3000ms'),
    )
    await expect(driver.getByTestId('save-draft').tap()).rejects.toThrow('Element.tap')
  })

  it('lets route assertions verify navigation when a page-method response is dropped', async () => {
    const { driver, element, miniProgram } = createDriverHarness()
    vi.mocked(miniProgram.evaluate).mockRejectedValueOnce(
      new Error('timeout waiting for automator response'),
    )

    await expect(driver.getByTestId('login-submit').tap()).resolves.toBeUndefined()
    expect(element.tap).not.toHaveBeenCalled()
  })

  it('uses relaunch for pure page switches that trigger broken navigateTo responses', async () => {
    const { driver, miniProgram, page } = createDriverHarness()

    await driver.getByTestId('land-demand-edit').tap()

    expect(miniProgram.reLaunch).toHaveBeenCalledWith('/pages/land-demand/index?mode=edit')
    expect(page.path).toBe('/pages/land-demand/index')
  })

  it('reads control values before rendered copy and clears storage through the wx bridge', async () => {
    const { driver, element, miniProgram } = createDriverHarness()
    element.property.mockResolvedValueOnce('1811')

    await expect(driver.getByTestId('project-hydm-cascader').text()).resolves.toBe('1811')
    await driver.clearStorage()

    expect(miniProgram.callWxMethod).toHaveBeenCalledWith('clearStorageSync')
  })

  it('falls back to rendered text when a native text node has no value property', async () => {
    const { driver, element } = createDriverHarness()
    element.property.mockRejectedValueOnce(new Error('text.value not exists'))

    await expect(driver.getByTestId('land-demand-status').text()).resolves.toBe('rendered text')
  })

  it('rejects selector injection through test IDs', () => {
    const { driver } = createDriverHarness()

    expect(() => driver.getByTestId('bad"]')).toThrow('Invalid data-testid')
  })
})

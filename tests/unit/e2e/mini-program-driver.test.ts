import type { MiniProgramLike } from 'weapp-ide-cli'

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

  it('fills native descendants and emits typed values for TDesign groups', async () => {
    const { driver, input, trigger } = createDriverHarness()

    await driver.getByTestId('area').fill('30')
    await driver.getByTestId('deploy-park').fill('["330203","330200"]')

    expect(input).toHaveBeenCalledWith('30')
    expect(trigger).toHaveBeenCalledWith('change', { value: ['330203', '330200'] })
  })

  it('reads control values before rendered copy and clears storage through app evaluate', async () => {
    const { driver, element, miniProgram } = createDriverHarness()
    element.property.mockResolvedValueOnce('1811')

    await expect(driver.getByTestId('project-hydm-cascader').text()).resolves.toBe('1811')
    await driver.clearStorage()

    expect(miniProgram.evaluate).toHaveBeenCalledWith('() => wx.clearStorageSync()')
  })

  it('rejects selector injection through test IDs', () => {
    const { driver } = createDriverHarness()

    expect(() => driver.getByTestId('bad"]')).toThrow('Invalid data-testid')
  })
})

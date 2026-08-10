import { afterEach, describe, expect, it, vi } from 'vitest'

import { scrollPageToTop } from '@/platform/page-scroll'
import {
  buildLoginRedirect,
  configureNavigationAdapter,
  navigate,
  replace,
} from '@/router/navigation'

vi.mock('@/platform/page-scroll', () => ({
  scrollPageToTop: vi.fn(),
}))

describe('typed navigation', () => {
  afterEach(() => {
    configureNavigationAdapter(undefined)
    vi.clearAllMocks()
  })

  it('uses ordinary push and replace navigation because the app has no tab bar', async () => {
    const calls: string[] = []
    configureNavigationAdapter({
      switchTab: async (path) => { calls.push(`tab:${path}`) },
      push: async (url) => { calls.push(`push:${url}`) },
      replace: async (url) => { calls.push(`replace:${url}`) },
    })

    await navigate('/pages/home/index')
    await replace('/pages/home/index', { source: 'login' })
    expect(calls).toEqual([
      'push:/pages/home/index',
      'replace:/pages/home/index?source=login',
    ])
    expect(vi.mocked(scrollPageToTop)).toHaveBeenCalledTimes(2)
  })

  it('encodes login returnTo once and avoids a login loop', () => {
    expect(buildLoginRedirect('/pages/error/index?reason=unavailable'))
      .toBe('/pages/login/index?returnTo=%2Fpages%2Ferror%2Findex%3Freason%3Dunavailable')
    expect(buildLoginRedirect('/pages/login/index')).toBe('/pages/login/index')
  })
})

import { afterEach, describe, expect, it } from 'vitest'

import {
  buildLoginRedirect,
  configureNavigationAdapter,
  navigate,
} from '@/router/navigation'

describe('typed navigation', () => {
  afterEach(() => {
    configureNavigationAdapter(undefined)
  })

  it('uses switchTab for tab routes and rejects tab query parameters', async () => {
    const calls: string[] = []
    configureNavigationAdapter({
      switchTab: async (path) => { calls.push(`tab:${path}`) },
      push: async (url) => { calls.push(`push:${url}`) },
      replace: async (url) => { calls.push(`replace:${url}`) },
    })

    await navigate('/pages/home/index')
    expect(calls).toEqual(['tab:/pages/home/index'])
    await expect(navigate('/pages/home/index', { id: 'order-1' }))
      .rejects
      .toThrow('Tab')
  })

  it('encodes login returnTo once and avoids a login loop', () => {
    expect(buildLoginRedirect('/subpackages/order/pages/detail/index?id=order-1'))
      .toBe('/pages/login/index?returnTo=%2Fsubpackages%2Forder%2Fpages%2Fdetail%2Findex%3Fid%3Dorder-1')
    expect(buildLoginRedirect('/pages/login/index')).toBe('/pages/login/index')
  })
})

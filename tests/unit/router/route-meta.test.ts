import { describe, expect, it } from 'vitest'

import { resolveRouteMeta, routeMeta } from '@/router/route-meta'

describe('route metadata lookup', () => {
  it('protects every product page except login', () => {
    expect(resolveRouteMeta('/pages/login/index')?.auth).not.toBe(true)
    expect(resolveRouteMeta('/pages/home/index')?.auth).toBe(true)
    expect(resolveRouteMeta('/pages/land-demand/index')?.auth).toBe(true)
    expect(resolveRouteMeta('/pages/land-demand/success')?.auth).toBe(true)
  })

  it('does not describe any route as a native tab', () => {
    expect(Object.values(routeMeta).every(meta => meta.tab !== true)).toBe(true)
  })
})

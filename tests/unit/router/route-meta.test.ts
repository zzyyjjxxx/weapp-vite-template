import { describe, expect, it } from 'vitest'

import * as routeMetaModule from '@/router/route-meta'

describe('route metadata lookup', () => {
  it('resolves the path shape emitted by wevu router', () => {
    const resolveRouteMeta = (routeMetaModule as Record<string, unknown>).resolveRouteMeta

    expect(resolveRouteMeta).toEqual(expect.any(Function))
    expect((resolveRouteMeta as (path: string) => unknown)('subpackages/order/pages/list/index'))
      .toMatchObject({ auth: true })
  })
})

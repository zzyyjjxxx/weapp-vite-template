import { describe, expect, it } from 'vitest'

import { buildUrl } from '@/shared/http/url'

describe('buildUrl', () => {
  it('normalizes slashes and encodes repeated query values', () => {
    expect(buildUrl('http://api.test/', '/orders', {
      keyword: 'a b',
      status: ['pending', 'processing'],
      empty: undefined,
      nullable: null,
    })).toBe('http://api.test/orders?keyword=a%20b&status=pending&status=processing')
  })

  it('supports a path without a leading slash', () => {
    expect(buildUrl('http://api.test', 'health')).toBe('http://api.test/health')
  })
})

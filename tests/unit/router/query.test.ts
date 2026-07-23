import { describe, expect, it } from 'vitest'

import {
  parseEnum,
  parseOptionalNumber,
  parseRequiredString,
} from '@/router/query'

describe('route query parsing', () => {
  it('parses valid values and rejects malformed input', () => {
    expect(parseRequiredString('order-1', 'id')).toBe('order-1')
    expect(parseOptionalNumber('2', 'page')).toBe(2)
    expect(parseEnum('pending', ['pending', 'completed'] as const, 'status'))
      .toBe('pending')

    expect(() => parseRequiredString(undefined, 'id')).toThrow('id')
    expect(() => parseOptionalNumber('not-a-number', 'page')).toThrow('page')
    expect(() => parseEnum('unknown', ['pending', 'completed'] as const, 'status'))
      .toThrow('status')
  })
})

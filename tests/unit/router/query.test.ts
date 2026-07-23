import { describe, expect, it } from 'vitest'

import * as queryModule from '@/router/query'
import {
  parseEnum,
  parseOptionalNumber,
  parseRequiredString,
} from '@/router/query'

describe('route query parsing', () => {
  it('decodes and validates an encoded login return path', () => {
    const parseReturnTo = (queryModule as Record<string, unknown>).parseReturnTo

    expect(parseReturnTo).toEqual(expect.any(Function))
    expect((parseReturnTo as (value: unknown) => string)(
      '%2Fsubpackages%2Forder%2Fpages%2Flist%2Findex',
    )).toBe('/subpackages/order/pages/list/index')
    expect((parseReturnTo as (value: unknown) => string)('https%3A%2F%2Fevil.example'))
      .toBe('/pages/home/index')
  })

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

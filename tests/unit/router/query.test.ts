import { describe, expect, it } from 'vitest'

import * as queryModule from '@/router/query'
import {
  parseEnum,
  parseLandDemandMode,
  parseLandDemandStep,
  parseOptionalNumber,
  parseRequiredString,
} from '@/router/query'

describe('route query parsing', () => {
  it('parses land-demand mode and defaults untrusted values to edit', () => {
    expect(parseLandDemandMode('view')).toBe('view')
    expect(parseLandDemandMode('edit')).toBe('edit')
    expect(parseLandDemandMode('other')).toBe('edit')
    expect(parseLandDemandMode(undefined)).toBe('edit')
  })

  it('accepts only the five land-demand steps', () => {
    expect(parseLandDemandStep('1')).toBe(1)
    expect(parseLandDemandStep(5)).toBe(5)
    expect(parseLandDemandStep(0)).toBeUndefined()
    expect(parseLandDemandStep(6)).toBeUndefined()
    expect(parseLandDemandStep('step-2')).toBeUndefined()
  })

  it('decodes and validates an encoded login return path', () => {
    const parseReturnTo = (queryModule as Record<string, unknown>).parseReturnTo

    expect(parseReturnTo).toEqual(expect.any(Function))
    expect((parseReturnTo as (value: unknown) => string)(
      '%2Fpages%2Ferror%2Findex',
    )).toBe('/pages/error/index')
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

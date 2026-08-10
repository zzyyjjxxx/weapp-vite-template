import { describe, expect, it } from 'vitest'

import {
  readBooleanDetail,
  readPatchDetail,
  readPickerValueDetail,
  readStringArrayDetail,
  readStringDetail,
} from '@/platform/event-detail'

describe('Wevu event detail helpers', () => {
  it('reads a TDesign value from the already-unwrapped detail object', () => {
    expect(readStringDetail({ value: '30' })).toBe('30')
    expect(readStringArrayDetail({ value: ['330203', 42, '330205'] }))
      .toEqual(['330203', '330205'])
    expect(readPickerValueDetail({ value: ['330203'], label: ['海曙区'] }))
      .toBe('330203')
  })

  it('reads checkbox values without expecting a native event wrapper', () => {
    expect(readBooleanDetail({ checked: true })).toBe(true)
    expect(readBooleanDetail({ value: false })).toBe(false)
  })

  it('accepts an already-unwrapped child component patch', () => {
    expect(readPatchDetail({ area: '30' })).toEqual({ area: '30' })
    expect(readPatchDetail(null)).toEqual({})
  })
})

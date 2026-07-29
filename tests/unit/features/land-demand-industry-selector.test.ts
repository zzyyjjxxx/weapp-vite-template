import { describe, expect, it } from 'vitest'
import {
  getIndustryDisplay,
  NATIONAL_INDUSTRY_OPTIONS,
} from '@/features/land-demand/industry-selector'

describe('national industry selector', () => {
  it('preserves generated parent and leaf hierarchy for the cascader', () => {
    const parent = NATIONAL_INDUSTRY_OPTIONS.find(option => option.value === '181')

    expect(parent?.label).toBe('机织服装制造')
    expect(parent?.children).toContainEqual({
      label: '运动机织服装制造（1811）',
      value: '1811',
    })
  })

  it('displays a selected leaf as industry name followed by its code', () => {
    expect(getIndustryDisplay('1811')).toBe('运动机织服装制造（1811）')
    expect(getIndustryDisplay('unknown')).toBe('unknown')
  })
})

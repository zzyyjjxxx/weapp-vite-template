import { describe, expect, it } from 'vitest'

import { getDirections, INDUSTRY_TRACK_DIRECTIONS } from '@/features/land-demand/dictionaries/industry-tracks'
import { LAND_TYPE_OPTIONS } from '@/features/land-demand/dictionaries/land-types'
import {
  EXPECT_PARK_OPTIONS,
  PARK_OPTIONS,
} from '@/features/land-demand/dictionaries/parks'

describe('regional and land dictionaries', () => {
  it('keeps Ningbo mutually exclusive metadata and land type single values', () => {
    expect(PARK_OPTIONS[0]).toEqual({ value: '330200', label: '宁波市' })
    expect(new Set(PARK_OPTIONS.map(item => item.value)).size).toBe(13)
    expect(EXPECT_PARK_OPTIONS).toBe(PARK_OPTIONS)
    expect(LAND_TYPE_OPTIONS).toEqual(['小微园', '租售型闲置空间', '租售型标准厂房', '以上皆可'])
  })
})

describe('industry track directions', () => {
  it('returns configured directions and an empty readonly array for unknown tracks', () => {
    expect(INDUSTRY_TRACK_DIRECTIONS.化工新材料).toEqual([
      '高端合成树脂（高端聚烯烃、工程塑料及特种工程塑料）',
      '高性能纤维及复合材料',
      '特种橡胶和弹性体',
      '功能化学品（电子化学品）',
      '其他',
    ])
    expect(getDirections('化工新材料')).toBe(INDUSTRY_TRACK_DIRECTIONS.化工新材料)
    expect(getDirections('未知赛道')).toEqual([])
  })
})

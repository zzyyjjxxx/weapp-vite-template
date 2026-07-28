import { describe, expect, it } from 'vitest'

import {
  getIndustryLabel,
  INDUSTRY_GROUPS,
} from '@/features/land-demand/dictionaries/industries.generated'

describe('national industry dictionary', () => {
  it('contains exactly the selected national industries', () => {
    const leaves = INDUSTRY_GROUPS.flatMap(group => group.children)

    expect(INDUSTRY_GROUPS).toHaveLength(150)
    expect(leaves).toHaveLength(515)
    expect(leaves.every(item => Number(item.pid) >= 181 && Number(item.pid) <= 439)).toBe(true)
    expect(leaves.every(item => item.label === `${item.industryName}（${item.industryCode}）`)).toBe(true)
  })

  it('looks up a generated leaf label by its industry code', () => {
    const leaf = INDUSTRY_GROUPS.flatMap(group => group.children)[0]

    expect(getIndustryLabel(leaf.industryCode)).toBe(leaf.label)
    expect(getIndustryLabel('unknown')).toBeUndefined()
  })
})

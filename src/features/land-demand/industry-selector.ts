import { getIndustryLabel, INDUSTRY_GROUPS } from './dictionaries/industries.generated'

export interface NationalIndustryOption {
  label: string
  value: string
  children: readonly NationalIndustryLeafOption[]
}

export interface NationalIndustryLeafOption {
  label: string
  value: string
}

export const NATIONAL_INDUSTRY_OPTIONS: readonly NationalIndustryOption[] = INDUSTRY_GROUPS.map(
  group => ({
    label: group.label,
    value: group.value,
    children: group.children.map(industry => ({
      label: industry.label,
      value: industry.industryCode,
    })),
  }),
)

export function getIndustryDisplay(code: string): string {
  return getIndustryLabel(code) ?? code
}

import type { FinancingChoice, LandDemandForm, YesNo } from './models'

const CITY_PARK = '330200'

export function selectDeployPark(current: readonly string[], next: string): string[] {
  if (next === CITY_PARK) {
    return current.includes(CITY_PARK) ? [] : [CITY_PARK]
  }

  const withoutCity = current.filter(value => value !== CITY_PARK)
  if (withoutCity.includes(next)) {
    return withoutCity.filter(value => value !== next)
  }

  return [...withoutCity, next]
}

export function applySpecialUseChoice(form: LandDemandForm, value: YesNo): LandDemandForm {
  return {
    ...form,
    is_specialuse: value,
    deploy_landtype: value === '否' ? '' : form.deploy_landtype,
  }
}

export function applyFinancingChoice(form: LandDemandForm, value: FinancingChoice): LandDemandForm {
  return {
    ...form,
    is_financing: value,
    financing_money: value === '没有' ? '' : form.financing_money,
    financing_time: value === '没有' ? '' : form.financing_time,
  }
}

export function applyTrackChoice(form: LandDemandForm, value: string): LandDemandForm {
  return {
    ...form,
    keyindustry: value,
    futureindustry: value === '其他' ? '其他' : '',
  }
}

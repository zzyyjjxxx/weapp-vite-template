import type { LandDemandForm, LandDemandRecordInput } from './models'

import type { EnterpriseProfile } from '@/features/auth/models'

function splitParks(value: string | undefined): string[] {
  return value?.split(',').map(item => item.trim()).filter(Boolean) ?? []
}

export function createLandDemandForm(
  enterprise: EnterpriseProfile,
  record?: Partial<LandDemandRecordInput>,
): LandDemandForm {
  const {
    deploy_park,
    landusedemand: _landusedemand,
    updatetime: _updatetime,
    lastSubmittedAt: _lastSubmittedAt,
    updateuser: _updateuser,
    newproject: _newproject,
    industryCode: _industryCode,
    is_energy: _isEnergy,
    energy: _energy,
    energy_time: _energyTime,
    qyhydm: _qyhydm,
    registrationType: _registrationType,
    ...savedForm
  } = record ?? {}
  const normalizedSavedForm = { ...savedForm } as Record<string, unknown>
  delete normalizedSavedForm.is_financing
  delete normalizedSavedForm.financing_money
  delete normalizedSavedForm.financing_time

  const form: LandDemandForm = {
    county: enterprise.county,
    region: enterprise.region,
    businessname: enterprise.businessname,
    creditcode: enterprise.creditcode,
    area: '',
    building_area: '',
    expect_park: '',
    expect_time: '',
    is_deploy: '',
    deploy_park: [],
    is_specialuse: '',
    deploy_landtype: '',
    deploy_height: '',
    deploy_weight: '',
    investment: '',
    project_hydm: '',
    keyindustry: '',
    futureindustry: '',
    pred_ys: '',
    pred_tax: '',
    pred_rdex: '',
    pred_unitenergy: '',
    projectdata: '',
    contact: enterprise.contact,
    office: enterprise.office,
    phone: enterprise.phone,
    ...normalizedSavedForm,
  }

  return {
    ...form,
    deploy_park: splitParks(deploy_park),
  }
}

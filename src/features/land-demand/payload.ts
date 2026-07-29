import type {
  LandDemandForm,
  LandDemandRecord,
  LandDemandStatus,
  SaveLandDemandPayload,
  UpdateLandDemandPayload,
} from './models'

function serializeForm(form: LandDemandForm, status: LandDemandStatus) {
  return {
    ...form,
    deploy_park: form.deploy_park.join(','),
    landusedemand: status,
  }
}

export function buildSavePayload(
  form: LandDemandForm,
  status: LandDemandStatus,
): SaveLandDemandPayload {
  return serializeForm(form, status)
}

export function buildUpdatePayload(
  form: LandDemandForm,
  original: LandDemandRecord,
  status: LandDemandStatus,
): UpdateLandDemandPayload {
  const { county: _county, region: _region, businessname: _businessname, ...mutableForm } = form

  return {
    ...mutableForm,
    deploy_park: form.deploy_park.join(','),
    landusedemand: status,
    newproject: '1',
    industryCode: original.industryCode,
    is_energy: original.is_energy,
    energy: original.energy,
    energy_time: original.energy_time,
    qyhydm: original.qyhydm,
    registrationType: original.registrationType,
  }
}

import type { EnterpriseProfile } from '@/features/auth/models'
import type { LandDemandDraft, LandDemandForm, LandDemandRecord } from '@/features/land-demand/models'

import { defineStore, ref } from 'wevu'
import { createLandDemandForm } from '@/features/land-demand/defaults'
import { getLandDemandRepository } from '@/features/land-demand/repository'

import './manager'

type LandDemandStep = 1 | 2 | 3 | 4 | 5

function cloneForm(form: LandDemandForm): LandDemandForm {
  return {
    ...form,
    deploy_park: [...form.deploy_park],
  }
}

function withAuthenticatedIdentity(
  form: LandDemandForm,
  enterprise: EnterpriseProfile,
): LandDemandForm {
  return {
    ...form,
    businessname: enterprise.businessname,
    creditcode: enterprise.creditcode,
    county: enterprise.county,
    region: enterprise.region,
  }
}

export const useLandDemandStore = defineStore('land-demand', () => {
  const form = ref<LandDemandForm>({} as LandDemandForm)
  const currentStep = ref<LandDemandStep>(1)
  const hasRecord = ref(false)
  const isDirty = ref(false)
  let enterprise: EnterpriseProfile | undefined

  function initialize(
    nextEnterprise: EnterpriseProfile,
    record?: LandDemandRecord,
    draft?: LandDemandDraft,
  ): void {
    enterprise = { ...nextEnterprise }
    form.value = withAuthenticatedIdentity(
      cloneForm(draft?.form ?? createLandDemandForm(nextEnterprise, record)),
      nextEnterprise,
    )
    currentStep.value = draft?.currentStep ?? 1
    hasRecord.value = Boolean(record)
    isDirty.value = false
  }

  function initializeFromLocalDraft(
    nextEnterprise: EnterpriseProfile,
    record?: LandDemandRecord,
  ): void {
    initialize(
      nextEnterprise,
      record,
      getLandDemandRepository().getDraft(nextEnterprise.creditcode),
    )
  }

  function patch(nextPatch: Partial<LandDemandForm>): void {
    const nextForm = {
      ...form.value,
      ...nextPatch,
      deploy_park: nextPatch.deploy_park
        ? [...nextPatch.deploy_park]
        : [...form.value.deploy_park],
    }
    form.value = enterprise
      ? withAuthenticatedIdentity(nextForm, enterprise)
      : nextForm
    isDirty.value = true
  }

  function goToStep(step: LandDemandStep): void {
    currentStep.value = step
  }

  function saveLocalDraft(): void {
    getLandDemandRepository().setDraft(form.value.creditcode, {
      form: cloneForm(form.value),
      currentStep: currentStep.value,
      savedAt: Date.now(),
    })
    isDirty.value = false
  }

  function discardLocalDraft(): void {
    getLandDemandRepository().removeDraft(form.value.creditcode)
    isDirty.value = false
  }

  function markPersisted(record: LandDemandRecord): void {
    hasRecord.value = true
    isDirty.value = false
    getLandDemandRepository().removeDraft(record.creditcode)
    if (enterprise) {
      form.value = createLandDemandForm(enterprise, record)
    }
  }

  return {
    form,
    currentStep,
    hasRecord,
    isDirty,
    initialize,
    initializeFromLocalDraft,
    patch,
    goToStep,
    saveLocalDraft,
    discardLocalDraft,
    markPersisted,
  }
})

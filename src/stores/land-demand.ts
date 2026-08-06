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

function formSignature(value: LandDemandForm): string {
  return JSON.stringify({
    ...value,
    deploy_park: [...value.deploy_park],
  })
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
  const progressStep = ref<LandDemandStep>(1)
  const hasRecord = ref(false)
  const hasLocalDraft = ref(false)
  const isDirty = ref(false)
  let enterprise: EnterpriseProfile | undefined
  let baselineSignature = ''
  let submittedBaselineSignature: string | undefined

  function initialize(
    nextEnterprise: EnterpriseProfile,
    record?: LandDemandRecord,
    draft?: LandDemandDraft,
  ): void {
    enterprise = { ...nextEnterprise }
    const initializedForm = withAuthenticatedIdentity(
      cloneForm(draft?.form ?? createLandDemandForm(nextEnterprise, record)),
      nextEnterprise,
    )
    form.value = initializedForm
    const submittedForm = record?.landusedemand === '1'
      ? withAuthenticatedIdentity(createLandDemandForm(nextEnterprise, record), nextEnterprise)
      : undefined
    submittedBaselineSignature = submittedForm ? formSignature(submittedForm) : undefined
    baselineSignature = submittedBaselineSignature ?? formSignature(initializedForm)
    const submitted = record?.landusedemand === '1'
    const initializedStep = draft?.currentStep ?? (submitted ? 5 : 1)
    currentStep.value = initializedStep
    progressStep.value = Math.max(
      initializedStep,
      submitted ? 5 : (draft?.progressStep ?? initializedStep),
    ) as LandDemandStep
    hasRecord.value = Boolean(record)
    hasLocalDraft.value = Boolean(draft)
    isDirty.value = formSignature(form.value) !== baselineSignature
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
    isDirty.value = formSignature(form.value) !== baselineSignature
  }

  function goToStep(step: LandDemandStep): void {
    currentStep.value = step
    progressStep.value = Math.max(progressStep.value, step) as LandDemandStep
  }

  function saveLocalDraft(): void {
    getLandDemandRepository().setDraft(form.value.creditcode, {
      form: cloneForm(form.value),
      currentStep: currentStep.value,
      progressStep: progressStep.value,
      savedAt: Date.now(),
    })
    hasLocalDraft.value = true
    if (!submittedBaselineSignature) {
      baselineSignature = formSignature(form.value)
    }
    isDirty.value = formSignature(form.value) !== baselineSignature
  }

  function discardLocalDraft(): void {
    getLandDemandRepository().removeDraft(form.value.creditcode)
    hasLocalDraft.value = false
    isDirty.value = formSignature(form.value) !== baselineSignature
  }

  function markPersisted(record: LandDemandRecord): void {
    const isDraftRecord = record.landusedemand === '2'
    hasRecord.value = true
    if (isDraftRecord) {
      submittedBaselineSignature = undefined
      // A temporary save still needs the local step metadata so the workbench
      // can show the latest completed step after the page is replaced.
      saveLocalDraft()
    }
    else {
      hasLocalDraft.value = false
      progressStep.value = 5
      getLandDemandRepository().removeDraft(record.creditcode)
    }
    if (enterprise) {
      form.value = createLandDemandForm(enterprise, record)
    }
    baselineSignature = formSignature(form.value)
    submittedBaselineSignature = isDraftRecord ? undefined : baselineSignature
    isDirty.value = false
  }

  return {
    form,
    currentStep,
    progressStep,
    hasRecord,
    hasLocalDraft,
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

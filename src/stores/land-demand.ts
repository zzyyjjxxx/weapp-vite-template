import type { EnterpriseProfile } from '@/features/auth/models'
import type { LandDemandDraft, LandDemandForm, LandDemandRecord } from '@/features/land-demand/models'

import { defineStore, ref } from 'wevu'
import { createLandDemandForm } from '@/features/land-demand/defaults'
import { getLandDemandRepository } from '@/features/land-demand/repository'
import { resolveProgressStep } from '@/features/land-demand/validation'

import './manager'

type LandDemandStep = 1 | 2 | 3 | 4 | 5

interface InitializeFromLocalDraftOptions {
  refreshFromServer?: boolean
}

function cloneForm(form: LandDemandForm): LandDemandForm {
  const cloned = {
    ...form,
    deploy_park: [...form.deploy_park],
  } as LandDemandForm & Record<string, unknown>

  // Drafts written by older builds may still contain the removed financing
  // fields. Strip them at every store boundary so they cannot reappear in
  // the UI or leak into a save/update payload.
  delete cloned.is_financing
  delete cloned.financing_money
  delete cloned.financing_time

  return cloned
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

function resolveServerDraftStep(
  enterprise: EnterpriseProfile,
  record: LandDemandRecord,
): LandDemandStep {
  const serverForm = createLandDemandForm(enterprise, record)
  return resolveProgressStep(serverForm)
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
    const initializedStep = draft?.currentStep
      ?? (submitted
        ? 5
        : record
          ? resolveServerDraftStep(nextEnterprise, record)
          : 1)
    const formProgressStep = resolveProgressStep(initializedForm, initializedStep)
    currentStep.value = initializedStep
    progressStep.value = Math.max(
      formProgressStep,
      submitted
        ? 5
        : (draft?.progressStep ?? initializedStep),
    ) as LandDemandStep
    hasRecord.value = Boolean(record)
    hasLocalDraft.value = Boolean(draft)
    isDirty.value = formSignature(form.value) !== baselineSignature
  }

  function initializeFromLocalDraft(
    nextEnterprise: EnterpriseProfile,
    record?: LandDemandRecord,
    options: InitializeFromLocalDraftOptions = {},
  ): void {
    const repository = getLandDemandRepository()
    let draft = repository.getDraft(nextEnterprise.creditcode)
    if (options.refreshFromServer && draft) {
      if (!record || record.landusedemand === '1') {
        repository.removeDraft(nextEnterprise.creditcode)
        draft = undefined
      }
      else {
        draft = {
          ...draft,
          form: cloneForm(createLandDemandForm(nextEnterprise, record)),
          savedAt: Date.now(),
        }
        repository.setDraft(nextEnterprise.creditcode, draft)
      }
    }
    initialize(
      nextEnterprise,
      record,
      draft,
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

  function clearForLogout(creditcode = form.value.creditcode): void {
    getLandDemandRepository().clearDrafts(creditcode)
    enterprise = undefined
    form.value = {} as LandDemandForm
    currentStep.value = 1
    progressStep.value = 1
    hasRecord.value = false
    hasLocalDraft.value = false
    isDirty.value = false
    baselineSignature = ''
    submittedBaselineSignature = undefined
  }

  function markPersisted(record: LandDemandRecord): void {
    const isDraftRecord = record.landusedemand === '2'
    const persistedForm = enterprise
      ? createLandDemandForm(enterprise, record)
      : form.value
    const persistedProgressStep = resolveProgressStep(persistedForm, currentStep.value)
    hasRecord.value = true
    if (isDraftRecord) {
      submittedBaselineSignature = undefined
      // Server data can contain later-step values even when local navigation
      // metadata still says that the user last viewed step 1.
      progressStep.value = Math.max(progressStep.value, persistedProgressStep) as LandDemandStep
      hasLocalDraft.value = true
    }
    else {
      hasLocalDraft.value = false
      progressStep.value = 5
      getLandDemandRepository().removeDraft(record.creditcode)
    }
    if (enterprise) {
      form.value = persistedForm
    }
    baselineSignature = formSignature(form.value)
    submittedBaselineSignature = isDraftRecord ? undefined : baselineSignature
    isDirty.value = false
    if (isDraftRecord) {
      // Persist the canonical server response and the corrected progress
      // metadata, rather than the pre-save local snapshot.
      saveLocalDraft()
    }
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
    clearForLogout,
    markPersisted,
  }
})

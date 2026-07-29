<script setup lang="ts">
import type {
  FieldError,
  FinancingChoice,
  LandDemandForm,
  YesNo,
} from '@/features/land-demand/models'

import { computed, ref, watchEffect } from 'wevu'
import AppError from '@/components/ui/app-error/index.vue'
import AppLoading from '@/components/ui/app-loading/index.vue'
import PageShell from '@/components/ui/page-shell/index.vue'
import BasicInfoStep from '@/features/land-demand/components/basic-info-step.vue'
import FinanceContactStep from '@/features/land-demand/components/finance-contact-step.vue'
import LandInfoStep from '@/features/land-demand/components/land-info-step.vue'
import ProjectInfoStep from '@/features/land-demand/components/project-info-step.vue'
import ReviewStep from '@/features/land-demand/components/review-step.vue'
import VerificationDialog from '@/features/land-demand/components/verification-dialog.vue'
import WizardActions from '@/features/land-demand/components/wizard-actions.vue'
import WizardProgress from '@/features/land-demand/components/wizard-progress.vue'
import {
  useLandDemandQuery,
  useSaveLandDemandMutation,
  useSendVerificationCodeMutation,
  useUpdateLandDemandMutation,
  useVerifyVerificationCodeMutation,
} from '@/features/land-demand/queries'
import {
  nextStep,
  previousStep,
  resolveSubmissionTarget,
} from '@/features/land-demand/step-controller'
import { createSubmitController } from '@/features/land-demand/submit'
import { validateDraft } from '@/features/land-demand/validation'
import {
  applyFinancingChoice,
  applySpecialUseChoice,
  applyTrackChoice,
  selectDeployPark,
} from '@/features/land-demand/visibility'
import { replace } from '@/router/navigation'
import { useAuthStore } from '@/stores/auth'
import { useLandDemandStore } from '@/stores/land-demand'

definePageJson({
  navigationBarTitleText: '用地需求填报',
})

type PendingClear
  = | { kind: 'deploy', value: YesNo }
    | { kind: 'special', value: YesNo }
    | { kind: 'track', value: string }
    | { kind: 'financing', value: FinancingChoice }

const auth = useAuthStore()
const enterprise = auth.enterprise
const creditcode = enterprise.value?.creditcode ?? ''
const query = useLandDemandQuery(creditcode)
const saveMutation = useSaveLandDemandMutation()
const updateMutation = useUpdateLandDemandMutation()
const sendCodeMutation = useSendVerificationCodeMutation()
const verifyCodeMutation = useVerifyVerificationCodeMutation()
const store = useLandDemandStore()
const form = store.form
const currentStep = store.currentStep
const errors = ref<FieldError[]>([])
const ready = ref(false)
const pendingClear = ref<PendingClear | null>(null)
const feedback = ref('')
const accepted = ref(false)
const acceptanceError = ref('')
const challenge = ref<NonNullable<typeof sendCodeMutation.data.value>>()
const verificationCode = ref('')
const verificationError = ref('')
let initialized = false

const saving = computed(() => saveMutation.isPending.value || updateMutation.isPending.value)
const submitting = computed(() => (
  sendCodeMutation.isPending.value
  || verifyCodeMutation.isPending.value
  || saving.value
))
const mutationError = computed(() => (
  saveMutation.error.value?.message ?? updateMutation.error.value?.message ?? ''
))
const queryErrorMessage = computed(() => query.error.value?.message ?? '请稍后重试')
const clearDialogVisible = computed(() => pendingClear.value !== null)
const verificationVisible = computed(() => challenge.value !== undefined)
const clearDialogContent = computed(() => {
  switch (pendingClear.value?.kind) {
    case 'deploy':
      return '选择“不接受”将清空已选的可调剂园区，是否继续？'
    case 'special':
      return '选择“否”将清空特殊用地类型，是否继续？'
    case 'track':
      return '切换重点产业赛道将清空已选的细分方向，是否继续？'
    case 'financing':
      return '选择“没有”将清空融资金额和融资时间，是否继续？'
    default:
      return ''
  }
})

watchEffect(() => {
  const profile = enterprise.value
  if (initialized || !profile || query.isPending.value) {
    return
  }

  store.initializeFromLocalDraft(profile, query.data.value)
  initialized = true
  ready.value = true
})

function readPatchDetail(event: unknown): Partial<LandDemandForm> {
  if (typeof event !== 'object' || event === null || !('detail' in event)) {
    return {}
  }
  const detail = event.detail
  return typeof detail === 'object' && detail !== null
    ? detail as Partial<LandDemandForm>
    : {}
}

function patchStore(patch: Partial<LandDemandForm>): void {
  store.patch(patch)
  const fields = new Set(Object.keys(patch) as (keyof LandDemandForm)[])
  errors.value = errors.value.filter(error => !fields.has(error.field))
  feedback.value = ''
}

function setAccepted(value: boolean): void {
  accepted.value = value
  if (value) {
    acceptanceError.value = ''
  }
}

function applyDeployParkSnapshot(next: readonly string[]): void {
  const current = form.value.deploy_park
  const changed = next.find(value => !current.includes(value))
    ?? current.find(value => !next.includes(value))
  patchStore({
    deploy_park: changed ? selectDeployPark(current, changed) : [...next],
  })
}

function applySpecialChoice(value: YesNo): void {
  const next = applySpecialUseChoice(form.value, value)
  patchStore({
    is_specialuse: next.is_specialuse,
    deploy_landtype: next.deploy_landtype,
  })
}

function applyTrack(value: string): void {
  if (value === form.value.keyindustry) {
    return
  }
  const next = applyTrackChoice(form.value, value)
  patchStore({
    keyindustry: next.keyindustry,
    futureindustry: next.futureindustry,
  })
}

function applyFinancing(value: FinancingChoice): void {
  const next = applyFinancingChoice(form.value, value)
  patchStore({
    is_financing: next.is_financing,
    financing_money: next.financing_money,
    financing_time: next.financing_time,
  })
}

function changeForm(event: unknown): void {
  const patch = readPatchDetail(event)

  if (patch.deploy_park) {
    applyDeployParkSnapshot(patch.deploy_park)
    return
  }
  if (patch.is_deploy) {
    if (patch.is_deploy === '否' && form.value.deploy_park.length > 0) {
      pendingClear.value = { kind: 'deploy', value: patch.is_deploy }
      return
    }
    patchStore({ is_deploy: patch.is_deploy })
    return
  }
  if (patch.is_specialuse) {
    if (patch.is_specialuse === '否' && Boolean(form.value.deploy_landtype)) {
      pendingClear.value = { kind: 'special', value: patch.is_specialuse }
      return
    }
    applySpecialChoice(patch.is_specialuse)
    return
  }
  if (patch.keyindustry !== undefined) {
    if (patch.keyindustry !== form.value.keyindustry && Boolean(form.value.futureindustry)) {
      pendingClear.value = { kind: 'track', value: patch.keyindustry }
      return
    }
    applyTrack(patch.keyindustry)
    return
  }
  if (patch.is_financing) {
    if (patch.is_financing === '没有' && Boolean(
      form.value.financing_money || form.value.financing_time,
    )) {
      pendingClear.value = { kind: 'financing', value: patch.is_financing }
      return
    }
    applyFinancing(patch.is_financing)
    return
  }

  patchStore(patch)
}

function cancelDestructiveClear(): void {
  pendingClear.value = null
}

function confirmDestructiveClear(): void {
  const pending = pendingClear.value
  pendingClear.value = null
  if (!pending) {
    return
  }

  switch (pending.kind) {
    case 'deploy':
      patchStore({ is_deploy: pending.value, deploy_park: [] })
      break
    case 'special':
      applySpecialChoice(pending.value)
      break
    case 'track':
      applyTrack(pending.value)
      break
    case 'financing':
      applyFinancing(pending.value)
      break
  }
}

function goToStep(step: 1 | 2 | 3 | 4 | 5): void {
  store.goToStep(step)
  store.saveLocalDraft()
}

function goPrevious(): void {
  goToStep(previousStep(currentStep.value))
}

function goNext(): void {
  goToStep(nextStep(currentStep.value))
}

async function saveDraft(): Promise<void> {
  feedback.value = ''
  saveMutation.reset()
  updateMutation.reset()
  const draftErrors = validateDraft(form.value)
  errors.value = draftErrors
  const target = resolveSubmissionTarget(draftErrors)
  if (target) {
    goToStep(target)
    return
  }

  try {
    const variables = {
      form: form.value,
      status: '2' as const,
      updateuser: enterprise.value?.username,
    }
    const original = query.data.value
    const record = original
      ? await updateMutation.mutateAsync({ ...variables, original })
      : await saveMutation.mutateAsync(variables)
    store.markPersisted(record)
    feedback.value = '已暂存'
  }
  catch {
    // The mutation exposes its sanitized error through mutationError.
  }
}

async function persistSubmission(status: '1'): Promise<Awaited<ReturnType<typeof saveMutation.mutateAsync>>> {
  const variables = {
    form: form.value,
    status,
    updateuser: enterprise.value?.username,
  }
  const original = query.data.value
  return original
    ? updateMutation.mutateAsync({ ...variables, original })
    : saveMutation.mutateAsync(variables)
}

const submitController = createSubmitController({
  sendCode: phone => sendCodeMutation.mutateAsync(phone),
  verifyCode: (phone, code) => verifyCodeMutation.mutateAsync({ phone, code }),
  persist: persistSubmission,
})

async function requestVerification(): Promise<void> {
  feedback.value = ''
  acceptanceError.value = ''
  sendCodeMutation.reset()
  try {
    const result = await submitController.requestCode(form.value, accepted.value)
    errors.value = result.errors
    acceptanceError.value = result.acceptanceError ?? ''
    const target = resolveSubmissionTarget(result.errors)
    if (target) {
      goToStep(target)
      return
    }
    if (!result.challenge) {
      return
    }
    challenge.value = result.challenge
    verificationCode.value = ''
    verificationError.value = ''
  }
  catch {
    feedback.value = sendCodeMutation.error.value?.message ?? '验证码发送失败，请稍后重试'
  }
}

function closeVerification(): void {
  if (submitting.value) {
    return
  }
  challenge.value = undefined
  verificationCode.value = ''
  verificationError.value = ''
}

async function submitVerificationCode(): Promise<void> {
  const currentChallenge = challenge.value
  if (!currentChallenge || submitting.value) {
    return
  }
  verificationError.value = ''
  verifyCodeMutation.reset()
  saveMutation.reset()
  updateMutation.reset()
  try {
    const record = await submitController.submitCode(
      currentChallenge.phone,
      verificationCode.value,
    )
    store.markPersisted(record)
    challenge.value = undefined
    await replace('/pages/land-demand/success')
  }
  catch (error) {
    verificationError.value = error instanceof Error
      ? error.message
      : '提交失败，请稍后重试'
  }
}
</script>

<template>
  <PageShell title="用地需求填报" :subtitle="enterprise?.businessname" icon="list-check">
    <AppLoading v-if="query.isPending || !ready" />
    <AppError
      v-else-if="query.isError"
      title="填报信息加载失败"
      :message="queryErrorMessage"
    />
    <view v-else class="land-demand-page">
      <WizardProgress :current-step="currentStep" />
      <scroll-view class="land-demand-page__form" scroll-y>
        <BasicInfoStep
          v-if="currentStep === 1"
          :form="form"
          :errors="errors"
          @change="changeForm"
        />
        <LandInfoStep
          v-else-if="currentStep === 2"
          :form="form"
          :errors="errors"
          @change="changeForm"
        />
        <ProjectInfoStep
          v-else-if="currentStep === 3"
          :form="form"
          :errors="errors"
          @change="changeForm"
        />
        <FinanceContactStep
          v-else-if="currentStep === 4"
          :form="form"
          :errors="errors"
          @change="changeForm"
        />
        <ReviewStep
          v-else
          :form="form"
          :accepted="accepted"
          :acceptance-error="acceptanceError"
          :submitting="submitting"
          @edit="goToStep"
          @accept="setAccepted"
          @submit="requestVerification"
        />
      </scroll-view>

      <text v-if="feedback" class="land-demand-page__feedback">{{ feedback }}</text>
      <text v-if="mutationError" class="land-demand-page__error">{{ mutationError }}</text>
      <WizardActions
        :current-step="currentStep"
        :saving="saving"
        @previous="goPrevious"
        @save="saveDraft"
        @next="goNext"
      />
    </view>

    <t-dialog
      data-testid="destructive-clear-dialog"
      :visible="clearDialogVisible"
      title="确认清空已有内容"
      :content="clearDialogContent"
      cancel-btn="取消"
      :confirm-btn="false"
      :close-on-overlay-click="false"
      @cancel="cancelDestructiveClear"
      @close="cancelDestructiveClear"
    >
      <template #confirm-btn>
        <t-button
          data-testid="destructive-clear-confirm"
          theme="primary"
          @tap="confirmDestructiveClear"
        >
          继续
        </t-button>
      </template>
    </t-dialog>
    <VerificationDialog
      :visible="verificationVisible"
      :challenge="challenge"
      :code="verificationCode"
      :loading="submitting"
      :error="verificationError"
      @change="verificationCode = $event"
      @close="closeVerification"
      @submit="submitVerificationCode"
    />
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.land-demand-page__form {
  max-height: calc(100vh - 420rpx);
}

.land-demand-page__feedback,
.land-demand-page__error {
  display: block;
}

.land-demand-page__feedback,
.land-demand-page__error {
  margin-top: $space-2;
  font-size: 24rpx;
  text-align: center;
}

.land-demand-page__feedback {
  color: $color-success;
}

.land-demand-page__error {
  color: $color-error;
}
</style>

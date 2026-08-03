<script setup lang="ts">
import type {
  FieldError,
  FinancingChoice,
  LandDemandForm,
  YesNo,
} from '@/features/land-demand/models'

import { computed, onLoad, ref, watchEffect } from 'wevu'
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
import { validateDraft, validateStep } from '@/features/land-demand/validation'
import {
  applyFinancingChoice,
  applySpecialUseChoice,
  applyTrackChoice,
  selectDeployPark,
} from '@/features/land-demand/visibility'
import { readPatchDetail } from '@/platform/event-detail'
import { replace } from '@/router/navigation'
import { runProtectedAction, useProtectedPage } from '@/router/protected-page'
import { parseLandDemandMode } from '@/router/query'
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
const { authorized } = useProtectedPage('/pages/land-demand/index')
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
const mode = ref<'edit' | 'view'>('edit')
const routeReady = ref(false)
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
const enterpriseName = computed(() => enterprise.value?.businessname ?? '')
const clearDialogVisible = computed(() => pendingClear.value !== null)
const verificationVisible = computed(() => challenge.value !== undefined)
const viewOnly = computed(() => mode.value === 'view')
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

onLoad((query) => {
  mode.value = parseLandDemandMode(query?.mode)
  routeReady.value = true
})

watchEffect(() => {
  const profile = enterprise.value
  if (initialized || !routeReady.value || !profile || query.isPending.value) {
    return
  }

  if (viewOnly.value && !query.data.value) {
    initialized = true
    void replace('/pages/home/index')
    return
  }

  if (viewOnly.value) {
    store.initialize(profile, query.data.value ?? undefined)
    store.goToStep(5)
  }
  else {
    store.initializeFromLocalDraft(profile, query.data.value ?? undefined)
  }
  initialized = true
  ready.value = true
})

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

function changeForm(detail: unknown): void {
  const patch = readPatchDetail<LandDemandForm>(detail)

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
  const stepErrors = validateStep(form.value, currentStep.value)
  errors.value = errors.value
    .filter(error => error.step !== currentStep.value)
    .concat(stepErrors)
  if (stepErrors.length > 0) {
    feedback.value = `请先完成第 ${currentStep.value} 步的必填项`
    return
  }
  feedback.value = ''
  goToStep(nextStep(currentStep.value))
}

async function saveDraftAuthorized(): Promise<void> {
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

async function saveDraft(): Promise<void> {
  await runProtectedAction(
    auth,
    '/pages/land-demand/index',
    saveDraftAuthorized,
  )
}

async function persistSubmissionAuthorized(status: '1'): Promise<Awaited<ReturnType<typeof saveMutation.mutateAsync>>> {
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

async function persistSubmission(status: '1'): Promise<Awaited<ReturnType<typeof saveMutation.mutateAsync>>> {
  const record = await runProtectedAction(
    auth,
    '/pages/land-demand/index',
    () => persistSubmissionAuthorized(status),
  )
  if (!record) {
    throw new Error('登录状态已失效，请重新登录')
  }
  return record
}

const submitController = createSubmitController({
  sendCode: phone => sendCodeMutation.mutateAsync(phone),
  verifyCode: (phone, code) => verifyCodeMutation.mutateAsync({ phone, code }),
  persist: persistSubmission,
})

async function requestVerificationAuthorized(): Promise<void> {
  feedback.value = '正在发送验证码，请稍候…'
  acceptanceError.value = ''
  sendCodeMutation.reset()
  try {
    const result = await submitController.requestCode(form.value, accepted.value)
    errors.value = result.errors
    acceptanceError.value = result.acceptanceError ?? ''
    const target = resolveSubmissionTarget(result.errors)
    if (target) {
      feedback.value = `请先完成第 ${target} 步的必填项`
      goToStep(target)
      return
    }
    if (!result.challenge) {
      feedback.value = ''
      return
    }
    challenge.value = result.challenge
    verificationCode.value = ''
    verificationError.value = ''
    feedback.value = '验证码已发送，请在弹窗中完成验证'
  }
  catch {
    feedback.value = sendCodeMutation.error.value?.message ?? '验证码发送失败，请稍后重试'
  }
}

async function requestVerification(): Promise<void> {
  await runProtectedAction(
    auth,
    '/pages/land-demand/index',
    requestVerificationAuthorized,
  )
}

function closeVerification(): void {
  if (submitting.value) {
    return
  }
  challenge.value = undefined
  verificationCode.value = ''
  verificationError.value = ''
}

async function submitVerificationCodeAuthorized(): Promise<void> {
  const currentChallenge = challenge.value
  if (!currentChallenge || submitting.value) {
    return
  }
  verificationError.value = ''
  feedback.value = '正在核验并提交，请稍候…'
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
    feedback.value = ''
    await replace('/pages/land-demand/success')
  }
  catch (error) {
    feedback.value = ''
    verificationError.value = error instanceof Error
      ? error.message
      : '提交失败，请稍后重试'
  }
}

async function submitVerificationCode(): Promise<void> {
  await runProtectedAction(
    auth,
    '/pages/land-demand/index',
    submitVerificationCodeAuthorized,
  )
}

async function backToHome(): Promise<void> {
  await replace('/pages/home/index')
}

async function editDetail(): Promise<void> {
  await replace('/pages/land-demand/index', { mode: 'edit' })
}
</script>

<template>
  <PageShell
    v-if="authorized"
    :title="viewOnly ? '填报详情' : '用地需求填报'"
    :subtitle="viewOnly ? `${enterpriseName} · 已提交信息` : `${enterpriseName} · 请按实际情况填写`"
    icon="list-check"
    compact
  >
    <view class="land-demand-page__content">
      <AppLoading v-if="query.isPending || !ready" />
      <AppError
        v-else-if="query.isError"
        title="填报信息加载失败"
        :message="queryErrorMessage"
      />
      <view v-else class="land-demand-page">
        <WizardProgress v-if="!viewOnly" :current-step="currentStep || 1" />
        <view v-if="!viewOnly" class="land-demand-page__guide">
          <view class="land-demand-page__guide-dot" />
          <text>当前第 {{ currentStep }} 步，共 5 步；切换步骤时会保留本地编辑内容</text>
        </view>
        <view class="land-demand-page__form">
          <BasicInfoStep
            v-if="currentStep === 1"
            id="basic-info-step"
            :form="form"
            :errors="errors"
            @change="changeForm"
          />
          <LandInfoStep
            v-else-if="currentStep === 2"
            id="land-info-step"
            :form="form"
            :errors="errors"
            @change="changeForm"
          />
          <ProjectInfoStep
            v-else-if="currentStep === 3"
            id="project-info-step"
            :form="form"
            :errors="errors"
            @change="changeForm"
          />
          <FinanceContactStep
            v-else-if="currentStep === 4"
            id="finance-contact-step"
            :form="form"
            :errors="errors"
            @change="changeForm"
          />
          <ReviewStep
            v-else
            id="review-step"
            :form="form"
            :accepted="accepted"
            :acceptance-error="acceptanceError || ''"
            :submitting="submitting"
            :readonly="viewOnly"
            @edit="goToStep"
            @accept="setAccepted"
            @submit="requestVerification"
          />
          <view v-if="viewOnly" class="land-demand-page__detail-actions">
            <t-button
              data-testid="detail-back-home"
              theme="default"
              block
              @tap="backToHome"
            >
              返回首页
            </t-button>
            <t-button
              data-testid="detail-edit"
              theme="primary"
              block
              @tap="editDetail"
            >
              修改填报
            </t-button>
          </view>
        </view>

        <text v-if="feedback" class="land-demand-page__feedback">{{ feedback }}</text>
        <text v-if="mutationError" class="land-demand-page__error">{{ mutationError }}</text>
        <WizardActions
          v-if="!viewOnly"
          id="wizard-actions"
          :current-step="currentStep || 1"
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
        :content="clearDialogContent || ''"
        :cancel-btn="false"
        :confirm-btn="false"
        :close-on-overlay-click="false"
        @cancel="cancelDestructiveClear"
        @close="cancelDestructiveClear"
      >
        <template #cancel-btn>
          <t-button
            data-testid="destructive-clear-cancel"
            class="land-demand-dialog__button"
            theme="default"
            variant="text"
            @tap="cancelDestructiveClear"
          >
            取消
          </t-button>
        </template>
        <template #confirm-btn>
          <t-button
            data-testid="destructive-clear-confirm"
            class="land-demand-dialog__button"
            theme="primary"
            @tap="confirmDestructiveClear"
          >
            继续
          </t-button>
        </template>
      </t-dialog>
      <VerificationDialog
        id="verification-dialog"
        :visible="verificationVisible"
        :challenge="challenge"
        :code="verificationCode || ''"
        :loading="submitting"
        :error="verificationError || ''"
        @change="verificationCode = $event"
        @close="closeVerification"
        @submit="submitVerificationCode"
      />
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.land-demand-page__guide {
  display: flex;
  align-items: flex-start;
  padding: $space-2 $space-3;
  margin: -$space-2 0 $space-3;
  font-size: 22rpx;
  line-height: 1.55;
  color: $color-text-secondary;
  background: rgb(255 255 255 / 68%);
  border: 1rpx solid rgb(217 229 246 / 76%);
  border-radius: $radius-md;
}

.land-demand-page__content {
  padding-bottom: 0;
}

.land-demand-page {
  padding-bottom: 220rpx;
}

.land-demand-page__guide-dot {
  flex: 0 0 auto;
  width: 10rpx;
  height: 10rpx;
  margin: 12rpx 12rpx 0 0;
  background: $color-primary;
  border-radius: 50%;
}

.land-demand-page__feedback,
.land-demand-page__error {
  display: block;
}

.land-demand-page__detail-actions {
  display: flex;
  gap: $space-2;
  padding: $space-3;
  margin-top: $space-4;
  background: $color-card;
  border-radius: $radius-lg;
  box-shadow: $shadow-card;
}

.land-demand-page__feedback,
.land-demand-page__error {
  padding: $space-2;
  margin-top: $space-3;
  font-size: 24rpx;
  text-align: center;
  border-radius: $radius-md;
}

.land-demand-page__feedback {
  color: $color-success;
  background: $color-success-soft;
}

.land-demand-page__error {
  color: $color-error;
  background: $color-error-soft;
}

.land-demand-dialog__button {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  border-radius: $radius-md;
}
</style>

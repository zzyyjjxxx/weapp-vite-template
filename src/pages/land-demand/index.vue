<script setup lang="ts">
import type {
  FieldError,
  LandDemandForm,
  YesNo,
} from '@/features/land-demand/models'
import type { LandDemandStep } from '@/router/query'

import { computed, nextTick, onLoad, ref, watchEffect } from 'wevu'
import AppError from '@/components/ui/app-error/index.vue'
import AppLoading from '@/components/ui/app-loading/index.vue'
import PageShell from '@/components/ui/page-shell/index.vue'
import PageTransitionLoading from '@/components/ui/page-transition-loading/index.vue'
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
import {
  validateDraft,
  validateStep,
} from '@/features/land-demand/validation'
import {
  applySpecialUseChoice,
  applyTrackChoice,
  selectDeployPark,
} from '@/features/land-demand/visibility'
import { readPatchDetail } from '@/platform/event-detail'
import { scrollPageToTop } from '@/platform/page-scroll'
import { usePageTransitionLoading } from '@/platform/page-transition'
import { replace } from '@/router/navigation'
import { runProtectedAction, useProtectedPage } from '@/router/protected-page'
import { parseLandDemandMode, parseLandDemandStep } from '@/router/query'
import { useAuthStore } from '@/stores/auth'
import { useLandDemandStore } from '@/stores/land-demand'

definePageJson({
  navigationBarTitleText: '用地需求填报',
})

type PendingClear
  = | { kind: 'deploy', value: YesNo }
    | { kind: 'special', value: YesNo }
    | { kind: 'track', value: string }

const auth = useAuthStore()
const { authorized } = useProtectedPage('/pages/land-demand/index')
const enterprise = auth.enterprise
const creditcode = () => enterprise.value?.creditcode ?? ''
const query = useLandDemandQuery(creditcode)
const queryPending = query.isPending
const queryFailed = query.isError
const saveMutation = useSaveLandDemandMutation()
const updateMutation = useUpdateLandDemandMutation()
const sendCodeMutation = useSendVerificationCodeMutation()
const sendCodePending = sendCodeMutation.isPending
const verifyCodeMutation = useVerifyVerificationCodeMutation()
const store = useLandDemandStore()
const form = store.form
const currentStep = store.currentStep
const progressStep = store.progressStep
const errors = ref<FieldError[]>([])
const scrollRequest = ref(0)
const ready = ref(false)
const pendingClear = ref<PendingClear | null>(null)
const feedback = ref('')
const saveNotice = ref('')
const saveErrorNotice = ref('')
const returningToHome = ref(false)
const requiredReturnDialogVisible = ref(false)
const { pending: transitioning, run: runTransition } = usePageTransitionLoading()
const submitted = computed(() => query.data.value?.landusedemand === '1')
const unchangedSubmittedRecord = computed(() => submitted.value && !store.isDirty.value)
const accepted = ref(false)
const acceptanceError = ref('')
const challenge = ref<NonNullable<typeof sendCodeMutation.data.value>>()
const verificationCode = ref('')
const verificationError = ref('')
const verificationSubmitting = ref(false)
const mode = ref<'edit' | 'view'>('edit')
const requestedStep = ref<LandDemandStep | undefined>(undefined)
const routeReady = ref(false)
const refreshFromServer = ref(false)
let initializedCreditcode = ''
let initializedSource: 'local' | 'server' | undefined

const saving = computed(() => saveMutation.isPending.value || updateMutation.isPending.value)
const submitting = computed(() => (
  verificationSubmitting.value
  || sendCodeMutation.isPending.value
  || verifyCodeMutation.isPending.value
  || saving.value
))
const mutationError = computed(() => (
  saveMutation.error.value?.message ?? updateMutation.error.value?.message ?? ''
))
const queryErrorMessage = computed(() => query.error.value?.message ?? '请稍后重试')
const clearDialogVisible = computed(() => pendingClear.value !== null)
const verificationVisible = ref(false)
const viewOnly = computed(() => mode.value === 'view')
const clearDialogContent = computed(() => {
  switch (pendingClear.value?.kind) {
    case 'deploy':
      return '选择“不接受”将清空已选的可调剂园区，是否继续？'
    case 'special':
      return '选择“否”将清空特殊用地类型，是否继续？'
    case 'track':
      return '切换重点产业赛道将清空已选的细分方向，是否继续？'
    default:
      return ''
  }
})
const wizardSteps: readonly LandDemandStep[] = [1, 2, 3, 4, 5]
const incompleteSteps = computed<LandDemandStep[]>(() => {
  if (!ready.value) {
    return []
  }

  return wizardSteps.filter(step => validateStep(form.value, step).length > 0)
})
const progressIncompleteSteps = computed<LandDemandStep[]>(() => {
  return incompleteSteps.value.filter(step => step <= progressStep.value)
})
onLoad((query) => {
  mode.value = parseLandDemandMode(query?.mode)
  requestedStep.value = parseLandDemandStep(query?.step)
  refreshFromServer.value = query?.freshLogin === '1'
  routeReady.value = true
})

watchEffect(() => {
  const profile = enterprise.value
  if (!profile) {
    initializedCreditcode = ''
    initializedSource = undefined
    ready.value = false
    return
  }
  const source = refreshFromServer.value ? 'server' : 'local'
  if (
    (initializedCreditcode === profile.creditcode && initializedSource === source)
    || !routeReady.value
    || queryPending.value
    || queryFailed.value
  ) {
    return
  }

  if (viewOnly.value && !query.data.value) {
    initializedCreditcode = profile.creditcode
    void runTransition(() => replace('/pages/home/index'))
    return
  }

  if (viewOnly.value) {
    store.initialize(profile, query.data.value ?? undefined)
    store.goToStep(5)
  }
  else {
    store.initializeFromLocalDraft(profile, query.data.value ?? undefined, {
      refreshFromServer: refreshFromServer.value,
    })
    if (requestedStep.value) {
      store.goToStep(requestedStep.value)
    }
  }
  initializedCreditcode = profile.creditcode
  initializedSource = source
  ready.value = true
})

function patchStore(patch: Partial<LandDemandForm>): void {
  store.patch(patch)
  const fields = new Set(Object.keys(patch) as (keyof LandDemandForm)[])
  errors.value = errors.value.filter(error => !fields.has(error.field))
  feedback.value = ''
  saveErrorNotice.value = ''
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
  }
}

async function goToStep(step: 1 | 2 | 3 | 4 | 5): Promise<void> {
  await runTransition(async () => {
    saveNotice.value = ''
    store.goToStep(step)
    store.saveLocalDraft()
    await nextTick()
    await new Promise<void>(resolve => setTimeout(resolve, 0))
    scrollPageToTop()
  })
}

function goPrevious(): void {
  void goToStep(previousStep(currentStep.value))
}

function goNext(): void {
  const stepErrors = validateStep(form.value, currentStep.value)
  errors.value = errors.value
    .filter(error => error.step !== currentStep.value)
    .concat(stepErrors)
  feedback.value = ''
  if (stepErrors.length > 0) {
    scrollRequest.value += 1
    return
  }
  void goToStep(nextStep(currentStep.value))
}

async function saveDraftAuthorized(showNotice = true): Promise<boolean> {
  feedback.value = ''
  saveNotice.value = ''
  saveErrorNotice.value = ''
  saveMutation.reset()
  updateMutation.reset()
  const draftErrors = validateDraft(form.value)
  errors.value = draftErrors
  const target = resolveSubmissionTarget(draftErrors)
  if (target) {
    goToStep(target)
    return false
  }
  if (unchangedSubmittedRecord.value) {
    return true
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
    if (showNotice) {
      saveNotice.value = '暂存成功'
    }
    return true
  }
  catch {
    // The mutation exposes its sanitized error through mutationError.
    return false
  }
}

async function saveDraftWithNotice(showNotice: boolean): Promise<boolean> {
  return await runProtectedAction(
    auth,
    '/pages/land-demand/index',
    () => saveDraftAuthorized(showNotice),
  ) ?? false
}

async function saveDraft(): Promise<boolean> {
  return await saveDraftWithNotice(true)
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

async function requestVerificationAuthorized(forceResend = false): Promise<void> {
  feedback.value = forceResend ? '正在重新发送验证码，请稍候…' : '正在发送验证码，请稍候…'
  acceptanceError.value = ''
  sendCodeMutation.reset()
  try {
    const result = await submitController.requestCode(form.value, accepted.value, {
      existingChallenge: challenge.value,
      forceResend,
    })
    errors.value = result.errors
    acceptanceError.value = result.acceptanceError ?? ''
    const target = resolveSubmissionTarget(result.errors)
    if (target) {
      feedback.value = ''
      goToStep(target)
      return
    }
    if (!result.challenge) {
      feedback.value = ''
      return
    }
    challenge.value = result.challenge
    verificationVisible.value = true
    verificationCode.value = ''
    verificationError.value = ''
    feedback.value = ''
  }
  catch {
    verificationVisible.value = true
    verificationCode.value = ''
    verificationError.value = sendCodeMutation.error.value?.message ?? '验证码发送失败，请稍后重试'
    feedback.value = ''
  }
}

async function requestVerification(): Promise<void> {
  await runProtectedAction(
    auth,
    '/pages/land-demand/index',
    () => requestVerificationAuthorized(false),
  )
}

async function resendVerification(): Promise<void> {
  if (verificationSubmitting.value) {
    return
  }
  await runProtectedAction(
    auth,
    '/pages/land-demand/index',
    () => requestVerificationAuthorized(true),
  )
}

function closeVerification(): void {
  if (verificationSubmitting.value) {
    return
  }
  verificationVisible.value = false
  verificationCode.value = ''
  verificationError.value = ''
}

async function submitVerificationCodeAuthorized(): Promise<void> {
  const currentChallenge = challenge.value
  if (!currentChallenge || verificationSubmitting.value) {
    return
  }
  if (!/^\d{6}$/.test(verificationCode.value)) {
    verificationError.value = '请输入6位验证码'
    return
  }
  verificationSubmitting.value = true
  verificationError.value = ''
  verifyCodeMutation.reset()
  saveMutation.reset()
  updateMutation.reset()
  try {
    await nextTick()
    const record = await submitController.submitCode(
      currentChallenge.phone,
      verificationCode.value,
    )
    store.markPersisted(record)
    verificationVisible.value = false
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
  finally {
    verificationSubmitting.value = false
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
  await runTransition(() => replace('/pages/home/index'))
}

async function completeReturnToHome(): Promise<void> {
  if (saving.value || returningToHome.value) {
    return
  }

  returningToHome.value = true
  const shouldPersist = !unchangedSubmittedRecord.value
  try {
    // Yield once so the TDesign loading state is rendered before the fast Mock mutation finishes.
    await nextTick()
    await new Promise<void>(resolve => setTimeout(resolve, 0))
    if (shouldPersist) {
      const saved = await saveDraftWithNotice(false)
      if (!saved) {
        returningToHome.value = false
        if (errors.value.length > 0) {
          saveErrorNotice.value = '当前填报内容存在问题，请先修正后再返回工作台'
        }
        return
      }
    }
    await replace('/pages/home/index', shouldPersist ? { notice: 'saved' } : undefined)
  }
  catch {
    returningToHome.value = false
    feedback.value = '返回工作台失败，请稍后重试'
  }
}

async function saveAndBackToHome(): Promise<void> {
  if (saving.value || returningToHome.value) {
    return
  }

  saveErrorNotice.value = ''
  const currentStepErrors = validateStep(form.value, currentStep.value)
  errors.value = errors.value
    .filter(error => error.step !== currentStep.value)
    .concat(currentStepErrors)
  if (currentStepErrors.some(error => error.message === '此项必填')) {
    requiredReturnDialogVisible.value = true
    return
  }

  await completeReturnToHome()
}

async function handleReturnToHome(): Promise<void> {
  await saveAndBackToHome()
}

function cancelRequiredReturn(): void {
  requiredReturnDialogVisible.value = false
}

async function confirmRequiredReturn(): Promise<void> {
  requiredReturnDialogVisible.value = false
  await completeReturnToHome()
}

async function editDetail(): Promise<void> {
  await runTransition(() => replace('/pages/land-demand/index', { mode: 'edit' }))
}
</script>

<template>
  <PageShell
    v-if="authorized"
    :title="viewOnly ? '填报详情' : '用地需求填报'"
    icon="list-check"
    compact
  >
    <template #actions>
      <t-button
        v-if="!viewOnly"
        t-class="land-demand-page__back-home-button"
        data-testid="land-demand-back-home"
        theme="default"
        variant="outline"
        size="extra-small"
        shape="round"
        :disabled="saving || returningToHome"
        @tap="handleReturnToHome"
      >
        返回工作台
      </t-button>
    </template>

    <view class="land-demand-page__content">
      <t-message
        v-if="saveNotice"
        data-testid="save-success-message"
        theme="success"
        :content="saveNotice"
        :visible="true"
        :duration="2000"
        :offset="[16, 16]"
        single
        @duration-end="saveNotice = ''"
        @close-btn-click="saveNotice = ''"
      />
      <t-message
        v-if="saveErrorNotice"
        data-testid="return-save-error-message"
        theme="error"
        :content="saveErrorNotice"
        :visible="true"
        :duration="4000"
        :offset="[16, 16]"
        single
        @duration-end="saveErrorNotice = ''"
        @close-btn-click="saveErrorNotice = ''"
      />
      <AppLoading v-if="queryPending || !ready" />
      <AppError
        v-else-if="queryFailed"
        title="填报信息加载失败"
        :message="queryErrorMessage"
      />
      <view v-else class="land-demand-page" :class="{ 'land-demand-page--view': viewOnly }">
        <WizardProgress
          v-if="!viewOnly"
          :current-step="currentStep || 1"
          :progress-step="progressStep || 1"
          :incomplete-steps="progressIncompleteSteps"
        />
        <view v-if="!viewOnly" class="land-demand-page__guide">
          <view class="land-demand-page__guide-dot" />
          <text>当前第 {{ currentStep }} 步，共 5 步；切换步骤时会保留本地编辑内容</text>
        </view>
        <view class="land-demand-page__form">
          <view :hidden="viewOnly || currentStep !== 1">
            <BasicInfoStep
              id="basic-info-step"
              :form="form"
              :errors="errors"
              :scroll-request="scrollRequest"
              :active="!viewOnly && currentStep === 1"
              @change="changeForm"
            />
          </view>
          <view :hidden="viewOnly || currentStep !== 2">
            <LandInfoStep
              id="land-info-step"
              :form="form"
              :errors="errors"
              :scroll-request="scrollRequest"
              :active="!viewOnly && currentStep === 2"
              @change="changeForm"
            />
          </view>
          <view :hidden="viewOnly || currentStep !== 3">
            <ProjectInfoStep
              id="project-info-step"
              :form="form"
              :errors="errors"
              :scroll-request="scrollRequest"
              :active="!viewOnly && currentStep === 3"
              @change="changeForm"
            />
          </view>
          <view :hidden="viewOnly || currentStep !== 4">
            <FinanceContactStep
              id="finance-contact-step"
              :form="form"
              :errors="errors"
              :scroll-request="scrollRequest"
              :active="!viewOnly && currentStep === 4"
              @change="changeForm"
            />
          </view>
          <view :hidden="!viewOnly && currentStep !== 5">
            <ReviewStep
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
          </view>
          <view v-if="viewOnly" class="land-demand-page__detail-actions">
            <t-button
              data-testid="detail-back-home"
              theme="default"
              block
              :disabled="transitioning"
              @tap="backToHome"
            >
              返回首页
            </t-button>
            <t-button
              data-testid="detail-edit"
              theme="primary"
              block
              :disabled="transitioning"
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
          :transitioning="transitioning"
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
        cancel-btn="取消"
        confirm-btn="继续"
        button-layout="horizontal"
        :close-on-overlay-click="false"
        @cancel="cancelDestructiveClear"
        @close="cancelDestructiveClear"
        @confirm="confirmDestructiveClear"
      />
      <t-dialog
        data-testid="required-return-dialog"
        :visible="requiredReturnDialogVisible"
        title="确认返回工作台"
        content="当前还有必填项未填写，是否确认返回？"
        cancel-btn="继续填写"
        confirm-btn="确认返回"
        button-layout="horizontal"
        :close-on-overlay-click="false"
        @cancel="cancelRequiredReturn"
        @close="cancelRequiredReturn"
        @confirm="confirmRequiredReturn"
      />
      <VerificationDialog
        id="verification-dialog"
        :visible="verificationVisible"
        :challenge="challenge"
        :code="verificationCode || ''"
        :loading="verificationSubmitting"
        :error="verificationError || ''"
        @change="verificationCode = $event"
        @close="closeVerification"
        @resend="resendVerification"
        @submit="submitVerificationCode"
      />
      <t-loading
        v-if="returningToHome"
        data-testid="return-home-loading"
        fullscreen
        size="56rpx"
        text="正在返回工作台"
      />
      <PageTransitionLoading
        :visible="transitioning"
        text="正在加载"
      />
      <PageTransitionLoading
        :visible="saving && !returningToHome && !verificationSubmitting"
        text="正在暂存"
      />
      <PageTransitionLoading
        :visible="sendCodePending && !returningToHome && !verificationSubmitting"
        text="正在发送验证码"
      />
      <PageTransitionLoading
        :visible="verificationSubmitting"
        text="正在提交"
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

.land-demand-page__back-home-button {
  box-sizing: border-box;
  width: 220rpx;
  min-width: 0;
  max-width: 220rpx;
  padding-right: 20rpx;
  padding-left: 20rpx;

  --td-button-border-radius: 999rpx;
  --td-button-default-outline-border-color: rgb(158 190 235 / 82%);
  --td-button-default-outline-active-bg-color: rgb(235 243 255 / 92%);
  --td-button-default-outline-active-border-color: rgb(74 126 224 / 72%);

  box-shadow: 0 6rpx 16rpx rgb(65 116 193 / 12%);
}

.land-demand-page {
  padding-bottom: calc(128rpx + env(safe-area-inset-bottom));
}

.land-demand-page--view {
  padding-bottom: 0;
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
</style>

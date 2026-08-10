<script setup lang="ts">
import type { LandDemandStep } from '@/router/query'

import { computed, onLoad, ref, watchEffect } from 'wevu'
import AppError from '@/components/ui/app-error/index.vue'
import AppIcon from '@/components/ui/app-icon/index.vue'
import AppLoading from '@/components/ui/app-loading/index.vue'
import PageTransitionLoading from '@/components/ui/page-transition-loading/index.vue'
import { useLandDemandQuery } from '@/features/land-demand/queries'
import { validateStep } from '@/features/land-demand/validation'
import { formatDateTime } from '@/platform/date-time'
import { usePageTransitionLoading } from '@/platform/page-transition'
import { navigate, replace } from '@/router/navigation'
import { useProtectedPage } from '@/router/protected-page'
import { useAuthStore } from '@/stores/auth'
import { useLandDemandStore } from '@/stores/land-demand'

definePageJson({
  navigationBarTitleText: '用地需求',
})

const auth = useAuthStore()
const { authorized } = useProtectedPage('/pages/home/index')
const enterprise = auth.enterprise
const creditcode = enterprise.value?.creditcode ?? ''
const landDemandQuery = useLandDemandQuery(creditcode)
const landDemandStore = useLandDemandStore()
const { pending: transitioning, run: runTransition } = usePageTransitionLoading()
const record = landDemandQuery.data
const submitted = computed(() => record.value?.landusedemand === '1')
const stepNumbers = [1, 2, 3, 4, 5] as const
const selectedStep = ref<LandDemandStep | undefined>(undefined)
const saveNotice = ref('')
const draftReady = computed(() => Boolean(
  record.value
  || landDemandStore.hasLocalDraft.value
  || landDemandStore.form.value.creditcode,
))
const currentProgressStep = computed(() => submitted.value
  ? 5
  : Math.min(5, Math.max(1, landDemandStore.progressStep.value)))
const queryErrorMessage = computed(() => landDemandQuery.error.value?.message ?? '请稍后重试')
const incompleteSteps = computed<LandDemandStep[]>(() => {
  if (!draftReady.value) {
    return []
  }

  return stepNumbers.filter(step => validateStep(landDemandStore.form.value, step).length > 0)
})
const progressIncompleteSteps = computed<LandDemandStep[]>(() => {
  return incompleteSteps.value
})
const resumeStep = computed<LandDemandStep>(() => (
  progressIncompleteSteps.value.find(step => step <= currentProgressStep.value)
  ?? currentProgressStep.value as LandDemandStep
))
const resumeStepIncomplete = computed(() => progressIncompleteSteps.value.includes(resumeStep.value))
const progressLabel = computed(() => submitted.value
  ? '已完成全部填报'
  : `已填写至第 ${resumeStep.value} 步 / 共 5 步`)
let draftInitialized = false

watchEffect(() => {
  const profile = enterprise.value
  if (draftInitialized || !profile || landDemandQuery.isPending.value) {
    return
  }
  landDemandStore.initializeFromLocalDraft(profile, record.value ?? undefined)
  draftInitialized = true
})
const enterpriseName = computed(() => enterprise.value?.businessname ?? '企业信息加载中')
const enterpriseCreditcode = computed(() => enterprise.value?.creditcode ?? '--')
const primaryLabel = computed(() => {
  if (!record.value) {
    return '开始填报'
  }
  if (record.value.landusedemand === '1') {
    return '查看填报'
  }
  return selectedStep.value ? `进入第 ${selectedStep.value} 步` : `进入第 ${resumeStep.value} 步`
})
const statusLabel = computed(() => {
  if (!record.value) {
    return '尚未填报'
  }
  return record.value.landusedemand === '1' ? '已提交' : '草稿待完善'
})
const lastSubmittedAt = computed(() => {
  const currentRecord = record.value
  if (!currentRecord) {
    return ''
  }
  const submittedAt = currentRecord.lastSubmittedAt
    ?? (currentRecord.landusedemand === '1' ? currentRecord.updatetime : undefined)
  return submittedAt ? formatDateTime(submittedAt) : ''
})

onLoad((query) => {
  saveNotice.value = query?.notice === 'saved' ? '暂存成功' : ''
})

async function openLandDemand(): Promise<void> {
  await runTransition(() => navigate('/pages/land-demand/index', {
    step: selectedStep.value ?? (record.value ? resumeStep.value : undefined),
  }))
}

async function viewLandDemand(): Promise<void> {
  await runTransition(() => replace('/pages/land-demand/index', { mode: 'view' }))
}

async function editLandDemand(): Promise<void> {
  await runTransition(() => replace('/pages/land-demand/index', {
    mode: 'edit',
    step: selectedStep.value ?? 5,
  }))
}

function selectStep(step: LandDemandStep): void {
  if (step <= currentProgressStep.value && !transitioning.value) {
    selectedStep.value = step
  }
}

async function logout(): Promise<void> {
  await runTransition(async () => {
    auth.clearSession()
    await replace('/pages/login/index')
  })
}
</script>

<template>
  <view
    v-if="authorized"
    class="page-shell"
  >
    <view class="page-shell__glow page-shell__glow--left" />
    <view class="page-shell__glow page-shell__glow--right" />
    <view class="page-shell__content">
      <view class="page-shell__header">
        <view class="page-shell__heading">
          <view class="page-shell__icon-wrap">
            <AppIcon
              class="page-shell__icon"
              name="home"
              :size="40"
              weight="Filled"
            />
          </view>
          <view class="page-shell__heading-copy">
            <text class="page-shell__eyebrow">企业用地需求服务</text>
            <text class="page-shell__title">企业服务工作台</text>
          </view>
        </view>
        <view class="page-shell__actions">
          <view
            data-testid="logout"
            class="home__logout"
            @tap="logout"
          >
            退出登录
          </view>
        </view>
      </view>

      <view class="page-shell__body">
        <view class="home__page-content">
          <AppLoading v-if="landDemandQuery.isPending" />
          <AppError
            v-else-if="landDemandQuery.isError"
            title="工作台信息加载失败"
            :message="queryErrorMessage"
          />
          <view v-else>
            <t-message
              v-if="saveNotice"
              data-testid="home-save-success-message"
              theme="success"
              :content="saveNotice"
              :visible="true"
              :duration="2000"
              :offset="[16, 16]"
              single
              @duration-end="saveNotice = ''"
              @close-btn-click="saveNotice = ''"
            />

            <view class="home__enterprise u-card">
              <view class="home__enterprise-mark">
                <text>企</text>
              </view>
              <view class="home__enterprise-copy">
                <text class="home__enterprise-name">
                  {{ enterpriseName }}
                </text>
                <text class="home__enterprise-creditcode">
                  统一社会信用代码：{{ enterpriseCreditcode }}
                </text>
              </view>
            </view>

            <view class="home__section-heading">
              <text class="u-section-heading">用地需求填报</text>
            </view>

            <view class="home__product u-card">
              <view class="home__product-heading">
                <view>
                  <text class="home__product-title">企业项目用地需求</text>
                </view>
                <text data-testid="land-demand-status" class="home__product-status">
                  {{ statusLabel }}
                </text>
              </view>
              <view class="home__steps">
                <view
                  v-for="number in stepNumbers"
                  :key="number"
                  :data-testid="`home-step-${number}`"
                  class="home__step"
                  :class="{
                    'home__step--active': number <= currentProgressStep,
                    'home__step--completed': number < currentProgressStep,
                    'home__step--current': number === resumeStep,
                    'home__step--complete': number === resumeStep && !resumeStepIncomplete,
                    'home__step--incomplete': progressIncompleteSteps.includes(number),
                    'home__step--selected': number === (selectedStep || resumeStep),
                    'home__step--pending': number > currentProgressStep,
                  }"
                  @tap="selectStep(number)"
                >
                  <text class="home__step-number">{{ number }}</text>
                </view>
              </view>
              <text class="home__steps-progress">{{ progressLabel }}</text>
              <text
                v-if="lastSubmittedAt"
                data-testid="land-demand-last-submitted-at"
                class="home__submitted-at"
              >
                上次提交：{{ lastSubmittedAt }}
              </text>
              <view v-if="submitted" class="home__product-actions">
                <t-button
                  data-testid="land-demand-view"
                  theme="default"
                  block
                  :disabled="landDemandQuery.isPending || transitioning"
                  @tap="viewLandDemand"
                >
                  查看详情
                </t-button>
                <t-button
                  data-testid="land-demand-edit"
                  theme="primary"
                  block
                  :disabled="landDemandQuery.isPending || transitioning"
                  @tap="editLandDemand"
                >
                  修改填报
                </t-button>
              </view>
              <t-button
                v-else
                data-testid="land-demand-primary"
                class="home__product-action"
                theme="primary"
                block
                :disabled="landDemandQuery.isPending || transitioning"
                @tap="openLandDemand"
              >
                {{ primaryLabel }}
              </t-button>
            </view>
          </view>
        </view>
        <PageTransitionLoading :visible="transitioning" text="正在加载" />
      </view>
    </view>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.page-shell {
  position: relative;
  min-height: 100vh;
  overflow: hidden;
  background: $gradient-page;
}

.page-shell__content {
  position: relative;
  z-index: 1;
  padding: $space-4 $space-4 $space-6;
}

.page-shell__glow {
  position: absolute;
  width: 420rpx;
  height: 420rpx;
  pointer-events: none;
  background: rgb(96 159 255 / 14%);
  border-radius: 50%;
  filter: blur(12rpx);
}

.page-shell__glow--left {
  top: -240rpx;
  left: -230rpx;
}

.page-shell__glow--right {
  top: 170rpx;
  right: -300rpx;
}

.page-shell__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  padding: $space-2 0 $space-4;
}

.page-shell__heading {
  display: flex;
  flex: 1;
  align-items: center;
  min-width: 0;
}

.page-shell__actions {
  flex: 0 0 auto;
  margin-left: $space-2;
}

.page-shell__icon {
  filter: brightness(0) invert(1);
}

.page-shell__icon-wrap {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 72rpx;
  height: 72rpx;
  margin-right: $space-2;
  background: $gradient-primary;
  border: 6rpx solid rgb(255 255 255 / 72%);
  border-radius: 22rpx;
  box-shadow: $shadow-button;
}

.page-shell__heading-copy {
  flex: 1;
  min-width: 0;
}

.page-shell__eyebrow {
  display: block;
  margin-bottom: 4rpx;
  font-size: 20rpx;
  font-weight: 600;
  color: $color-primary;
  letter-spacing: 2rpx;
}

.page-shell__title {
  display: block;
  font-size: 40rpx;
  font-weight: 700;
  line-height: 1.25;
  color: $color-text;
}

.page-shell__body {
  min-height: 480rpx;
}

.home__enterprise,
.home__product {
  padding: $space-4;
}

.home__logout {
  flex: 0 0 auto;
  min-width: 128rpx;
  padding: 14rpx 18rpx;
  font-size: 24rpx;
  font-weight: 600;
  line-height: 1.3;
  color: $color-text;
  text-align: center;
  white-space: nowrap;
  background: $color-card;
  border: 1rpx solid $color-border;
  border-radius: 999rpx;
}

.home__enterprise {
  display: flex;
  align-items: center;
  margin-top: $space-3;
}

.home__enterprise-mark {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 76rpx;
  height: 76rpx;
  margin-right: $space-3;
  font-size: 28rpx;
  font-weight: 700;
  color: #fff;
  background: $gradient-primary;
  border-radius: 24rpx;
  box-shadow: $shadow-button;
}

.home__enterprise-copy {
  flex: 1;
  min-width: 0;
}

.home__product {
  margin-top: $space-3;
}

.home__enterprise-name,
.home__enterprise-creditcode,
.home__product-title,
.home__product-status,
.home__product-copy,
.home__submitted-at {
  display: block;
}

.home__enterprise-name,
.home__product-title {
  font-size: 30rpx;
  font-weight: 700;
  color: $color-text;
}

.home__enterprise-creditcode,
.home__product-copy {
  margin-top: $space-1;
  font-size: 24rpx;
  line-height: 1.6;
  color: $color-text-secondary;
}

.home__section-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: $space-5;
}

.home__product-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.home__product-status {
  padding: 8rpx $space-2;
  font-size: 22rpx;
  color: $color-primary;
  background: $color-primary-soft;
  border-radius: 999rpx;
}

.home__steps {
  position: relative;
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  align-items: center;
  width: 100%;
  margin-top: $space-3;
}

.home__step {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 0;
}

.home__step:not(:last-child)::after {
  position: absolute;
  top: 50%;
  left: 50%;
  z-index: 0;
  width: 100%;
  height: 2rpx;
  content: '';
  background: #d9e6fa;
  transform: translateY(-50%);
}

.home__step-number {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 34rpx;
  height: 34rpx;
  font-size: 19rpx;
  color: $color-primary;
  background: $color-primary-soft;
  border: 3rpx solid transparent;
  border-radius: 50%;
}

.home__step--active .home__step-number {
  color: #fff;
  background: $gradient-primary;
  box-shadow: $shadow-button;
}

.home__step--current .home__step-number {
  border-color: #cfe0ff;
}

.home__step--selected .home__step-number {
  border-color: #7aa9f8;
  box-shadow: 0 0 0 5rpx rgb(56 113 224 / 14%);
}

.home__step--complete .home__step-number {
  color: #fff;
  background: $color-success;
  border-color: $color-success-soft;
  box-shadow: 0 6rpx 14rpx rgb(10 168 117 / 24%);
}

.home__step--incomplete .home__step-number {
  color: #fff;
  background: $color-error;
  border-color: $color-error-soft;
  box-shadow: 0 6rpx 14rpx rgb(213 73 65 / 24%);
}

.home__step--pending {
  opacity: 0.5;
}

.home__step--active:not(:last-child)::after {
  background: #83affb;
}

.home__step--complete:not(:last-child)::after {
  background: $color-success;
}

.home__step--incomplete:not(:last-child)::after {
  background: $color-error;
}

.home__steps-progress {
  display: block;
  margin-top: 8rpx;
  font-size: 20rpx;
  color: $color-text-secondary;
  text-align: center;
}

.home__submitted-at {
  margin-top: 6rpx;
  font-size: 20rpx;
  color: $color-text-secondary;
  text-align: center;
}

.home__product-action {
  margin-top: $space-4;
  overflow: hidden;
  border-radius: $radius-md;
  box-shadow: $shadow-button;
}

.home__product-actions {
  display: flex;
  gap: $space-2;
  margin-top: $space-4;
}
</style>

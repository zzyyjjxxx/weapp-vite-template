<script setup lang="ts">
import type { LandDemandStep } from '@/router/query'

import { computed, ref, watchEffect } from 'wevu'
import PageShell from '@/components/ui/page-shell/index.vue'
import { useLandDemandQuery } from '@/features/land-demand/queries'
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
const record = landDemandQuery.data
const submitted = computed(() => record.value?.landusedemand === '1')
const stepNumbers = [1, 2, 3, 4, 5] as const
const selectedStep = ref<LandDemandStep | undefined>(undefined)
const currentProgressStep = computed(() => submitted.value
  ? 5
  : Math.min(5, Math.max(1, landDemandStore.progressStep.value)))
const progressLabel = computed(() => submitted.value
  ? '已完成全部填报'
  : `已填写至第 ${currentProgressStep.value} 步 / 共 5 步`)
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
const fillingDateRange = computed(() => {
  const timestamp = record.value?.updatetime
  const base = timestamp ? new Date(timestamp) : new Date()
  const start = new Date(base.getFullYear(), base.getMonth(), 1)
  const end = new Date(base.getFullYear(), base.getMonth() + 1, 0)
  const twoDigits = (value: number) => value < 10 ? `0${value}` : String(value)
  const format = (date: Date) => `${date.getFullYear()}.${twoDigits(date.getMonth() + 1)}.${twoDigits(date.getDate())}`
  return `${format(start)} — ${format(end)}`
})
const primaryLabel = computed(() => {
  if (!record.value) {
    return '开始填报'
  }
  if (record.value.landusedemand === '1') {
    return '查看填报'
  }
  return selectedStep.value ? `进入第 ${selectedStep.value} 步` : '继续填写'
})
const statusLabel = computed(() => {
  if (!record.value) {
    return '尚未填报'
  }
  return record.value.landusedemand === '1' ? '已提交' : '草稿待完善'
})

async function openLandDemand(): Promise<void> {
  await navigate('/pages/land-demand/index', {
    step: selectedStep.value ?? (record.value ? currentProgressStep.value : undefined),
  })
}

async function viewLandDemand(): Promise<void> {
  await replace('/pages/land-demand/index', { mode: 'view' })
}

async function editLandDemand(): Promise<void> {
  await replace('/pages/land-demand/index', {
    mode: 'edit',
    step: selectedStep.value ?? 5,
  })
}

function selectStep(step: LandDemandStep): void {
  if (step <= currentProgressStep.value) {
    selectedStep.value = step
  }
}

async function logout(): Promise<void> {
  auth.clearSession()
  await replace('/pages/login/index')
}
</script>

<template>
  <PageShell
    v-if="authorized"
    title="企业服务工作台"
    icon="home"
  >
    <template #actions>
      <view
        data-testid="logout"
        class="home__logout"
        @tap="logout"
      >
        退出登录
      </view>
    </template>

    <view class="home__hero">
      <view class="home__hero-content">
        <text class="home__hero-kicker">企业用地需求服务</text>
        <text class="home__hero-title">让项目需求更清晰</text>
      </view>
    </view>

    <view class="home__enterprise u-card">
      <view class="home__enterprise-mark">
        <text>企</text>
      </view>
      <view class="home__enterprise-copy">
        <text class="home__enterprise-label">当前登录企业</text>
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
      <text class="home__section-caption">LAND DEMAND</text>
    </view>

    <view class="home__product u-card">
      <view class="home__product-heading">
        <view>
          <text class="home__product-title">企业项目用地需求</text>
          <text class="home__product-caption">五步完成信息填报</text>
          <text class="home__date-range">填报时间：{{ fillingDateRange }}</text>
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
            'home__step--current': number === currentProgressStep,
            'home__step--selected': number === (selectedStep || currentProgressStep),
            'home__step--pending': number > currentProgressStep,
          }"
          @tap="selectStep(number)"
        >
          <text class="home__step-number">{{ number }}</text>
        </view>
      </view>
      <text class="home__steps-progress">{{ progressLabel }}</text>
      <view v-if="submitted" class="home__product-actions">
        <t-button
          data-testid="land-demand-view"
          theme="default"
          block
          :loading="landDemandQuery.isPending"
          @tap="viewLandDemand"
        >
          查看详情
        </t-button>
        <t-button
          data-testid="land-demand-edit"
          theme="primary"
          block
          :loading="landDemandQuery.isPending"
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
        :loading="landDemandQuery.isPending"
        @tap="openLandDemand"
      >
        {{ primaryLabel }}
      </t-button>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.home__enterprise,
.home__product {
  padding: $space-4;
}

.home__hero {
  position: relative;
  box-sizing: border-box;
  min-height: 190rpx;
  overflow: hidden;
  background: linear-gradient(135deg, #edf6ff 0%, #f8fbff 100%);
  border: 1rpx solid rgb(211 226 248 / 78%);
  border-radius: $radius-lg;
  box-shadow: $shadow-card;
}

.home__hero::after {
  position: absolute;
  top: -100rpx;
  right: -80rpx;
  width: 280rpx;
  height: 280rpx;
  content: '';
  background: rgb(95 153 241 / 10%);
  border-radius: 50%;
}

.home__hero-content {
  position: relative;
  z-index: 1;
  width: 100%;
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

.home__hero-kicker,
.home__hero-title,
.home__hero-copy,
.home__date-range,
.home__enterprise-label,
.home__product-caption,
.home__section-caption {
  display: block;
}

.home__hero-kicker {
  font-size: 21rpx;
  font-weight: 700;
  color: $color-primary;
}

.home__hero-title {
  display: block;
  margin-top: $space-2;
  font-size: 38rpx;
  font-weight: 800;
  line-height: 1.3;
  color: #173a77;
}

.home__hero-copy {
  margin-top: $space-2;
  font-size: 23rpx;
  line-height: 1.6;
  color: $color-text-secondary;
}

.home__date-range {
  margin-top: 6rpx;
  font-size: 20rpx;
  line-height: 1.45;
  color: $color-text-placeholder;
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

.home__enterprise-label {
  margin-bottom: 4rpx;
  font-size: 20rpx;
  color: $color-text-placeholder;
}

.home__product {
  margin-top: $space-3;
}

.home__enterprise-name,
.home__enterprise-creditcode,
.home__product-title,
.home__product-status,
.home__product-copy {
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

.home__section-caption {
  font-size: 18rpx;
  color: $color-text-placeholder;
  letter-spacing: 2rpx;
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

.home__product-caption {
  margin-top: 4rpx;
  font-size: 21rpx;
  color: $color-text-placeholder;
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

.home__step--pending {
  opacity: 0.5;
}

.home__step--active:not(:last-child)::after {
  background: #83affb;
}

.home__steps-progress {
  display: block;
  margin-top: 8rpx;
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

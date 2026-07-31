<script setup lang="ts">
import { computed } from 'wevu'
import landPlanningHero from '@/assets/land-planning-hero.webp'
import PageShell from '@/components/ui/page-shell/index.vue'
import { useLandDemandQuery } from '@/features/land-demand/queries'
import { navigate, replace } from '@/router/navigation'
import { useProtectedPage } from '@/router/protected-page'
import { useAuthStore } from '@/stores/auth'

definePageJson({
  navigationBarTitleText: '用地需求',
})

const auth = useAuthStore()
const { authorized } = useProtectedPage('/pages/home/index')
const enterprise = auth.enterprise
const creditcode = enterprise.value?.creditcode ?? ''
const landDemandQuery = useLandDemandQuery(creditcode)
const record = landDemandQuery.data
const submitted = computed(() => record.value?.landusedemand === '1')
const enterpriseName = computed(() => enterprise.value?.businessname ?? '企业信息加载中')
const enterpriseSubtitle = computed(() => enterprise.value?.businessname ?? '企业服务')
const enterpriseCreditcode = computed(() => enterprise.value?.creditcode ?? '--')
const primaryLabel = computed(() => {
  if (!record.value) {
    return '开始填报'
  }
  return record.value.landusedemand === '1' ? '查看填报' : '继续填写'
})
const statusLabel = computed(() => {
  if (!record.value) {
    return '尚未填报'
  }
  return record.value.landusedemand === '1' ? '已提交' : '草稿待完善'
})

async function openLandDemand(): Promise<void> {
  await navigate('/pages/land-demand/index')
}

async function viewLandDemand(): Promise<void> {
  await navigate('/pages/land-demand/index', { mode: 'view' })
}

async function editLandDemand(): Promise<void> {
  await navigate('/pages/land-demand/index', { mode: 'edit' })
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
    :subtitle="enterpriseSubtitle"
    icon="home"
  >
    <template #actions>
      <t-button
        data-testid="logout"
        size="small"
        theme="default"
        variant="text"
        @tap="logout"
      >
        退出登录
      </t-button>
    </template>

    <view class="home__hero">
      <image
        class="home__hero-image"
        :src="landPlanningHero"
        mode="aspectFill"
      />
      <view class="home__hero-shade" />
      <view class="home__hero-content">
        <text class="home__hero-kicker">企业用地需求服务</text>
        <text class="home__hero-title">让项目需求更清晰</text>
        <text class="home__hero-copy">在线填报、随时暂存，提交后由相关部门跟进服务</text>
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
        </view>
        <text data-testid="land-demand-status" class="home__product-status">
          {{ statusLabel }}
        </text>
      </view>
      <text class="home__product-copy">
        依次填写基本信息、用地需求、投资项目、融资及联系人，确认无误后提交。
      </text>
      <view class="home__steps">
        <view v-for="number in 5" :key="number" class="home__step">
          <text class="home__step-number">{{ number }}</text>
        </view>
      </view>
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
  height: 330rpx;
  overflow: hidden;
  background: $gradient-hero;
  border: 1rpx solid rgb(211 226 248 / 78%);
  border-radius: $radius-lg;
  box-shadow: $shadow-card;
}

.home__hero-image {
  position: absolute;
  right: -180rpx;
  bottom: -16rpx;
  width: 660rpx;
  height: 370rpx;
}

.home__hero-shade {
  position: absolute;
  inset: 0;
  background: linear-gradient(90deg, rgb(238 246 255 / 100%) 0%, rgb(238 246 255 / 94%) 42%, rgb(238 246 255 / 8%) 84%);
}

.home__hero-content {
  position: relative;
  z-index: 1;
  width: 58%;
  padding: $space-5 $space-4;
}

.home__hero-kicker,
.home__hero-title,
.home__hero-copy,
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
  display: flex;
  align-items: center;
  margin-top: $space-3;
}

.home__step {
  position: relative;
  display: flex;
  flex: 1;
  align-items: center;
}

.home__step:not(:last-child)::after {
  flex: 1;
  height: 2rpx;
  margin: 0 8rpx;
  content: '';
  background: #d9e6fa;
}

.home__step-number {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 34rpx;
  height: 34rpx;
  font-size: 19rpx;
  color: $color-primary;
  background: $color-primary-soft;
  border-radius: 50%;
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

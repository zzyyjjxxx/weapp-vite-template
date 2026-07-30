<script setup lang="ts">
import { computed } from 'wevu'
import AppIcon from '@/components/ui/app-icon/index.vue'
import { useLandDemandQuery } from '@/features/land-demand/queries'
import { navigate, replace } from '@/router/navigation'
import { useProtectedPage } from '@/router/protected-page'
import { useAuthStore } from '@/stores/auth'

definePageJson({
  navigationBarTitleText: '用地需求',
})

definePageMeta({
  layout: false,
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
  <view
    v-if="authorized"
    class="home-shell"
  >
    <view class="home-shell__header">
      <view class="home-shell__heading">
        <AppIcon
          class="home-shell__icon"
          name="home"
          :size="48"
          weight="Filled"
        />
        <view class="home-shell__heading-copy">
          <text class="home-shell__title">用地需求</text>
          <text class="home-shell__subtitle">{{ enterpriseSubtitle }}</text>
        </view>
      </view>
      <t-button
        data-testid="logout"
        size="small"
        theme="default"
        variant="text"
        @tap="logout"
      >
        退出登录
      </t-button>
    </view>

    <view class="home__enterprise u-card">
      <text class="home__enterprise-name">
        {{ enterpriseName }}
      </text>
      <text class="home__enterprise-creditcode">
        统一社会信用代码：{{ enterpriseCreditcode }}
      </text>
    </view>

    <view class="home__product u-card">
      <view class="home__product-heading">
        <text class="home__product-title">土地需求申报</text>
        <text data-testid="land-demand-status" class="home__product-status">
          {{ statusLabel }}
        </text>
      </view>
      <text class="home__product-copy">
        填写企业项目用地需求，提交后由相关部门跟进服务。
      </text>
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
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.home-shell {
  min-height: 100vh;
  padding: $space-5 $space-4 $space-5;
  background: $color-page;
}

.home-shell__header,
.home-shell__heading {
  display: flex;
  align-items: center;
}

.home-shell__header {
  justify-content: space-between;
  padding: $space-2 0 $space-4;
}

.home-shell__heading {
  min-width: 0;
}

.home-shell__icon {
  margin-right: $space-2;
}

.home-shell__heading-copy {
  min-width: 0;
}

.home-shell__title,
.home-shell__subtitle {
  display: block;
}

.home-shell__title {
  font-size: 44rpx;
  font-weight: 700;
  line-height: 1.25;
  color: $color-text;
}

.home-shell__subtitle {
  margin-top: $space-1;
  font-size: 24rpx;
  line-height: 1.6;
  color: $color-text-secondary;
}

.home__enterprise,
.home__product {
  padding: $space-4;
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
  font-size: 32rpx;
  font-weight: 700;
  color: $color-text;
}

.home__enterprise-creditcode,
.home__product-copy {
  margin-top: $space-2;
  font-size: 26rpx;
  line-height: 1.6;
  color: $color-text-secondary;
}

.home__product-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.home__product-status {
  padding: 4rpx $space-2;
  font-size: 22rpx;
  color: $color-primary;
  background: $color-primary-soft;
  border-radius: $radius-sm;
}

.home__product-action {
  margin-top: $space-4;
}

.home__product-actions {
  display: flex;
  gap: $space-2;
  margin-top: $space-4;
}
</style>

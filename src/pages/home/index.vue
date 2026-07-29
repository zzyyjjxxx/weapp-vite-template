<script setup lang="ts">
import { computed } from 'wevu'
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
    title="用地需求"
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
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

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

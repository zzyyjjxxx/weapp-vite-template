<script setup lang="ts">
import { computed, ref, watchEffect } from 'wevu'
import AppError from '@/components/ui/app-error/index.vue'
import AppLoading from '@/components/ui/app-loading/index.vue'
import PageShell from '@/components/ui/page-shell/index.vue'
import { useLandDemandQuery } from '@/features/land-demand/queries'
import { navigate, replace } from '@/router/navigation'
import { useProtectedPage } from '@/router/protected-page'
import { useAuthStore } from '@/stores/auth'

definePageJson({
  navigationBarTitleText: '填报完成',
})

const auth = useAuthStore()
const { authorized } = useProtectedPage('/pages/land-demand/success')
const enterprise = auth.enterprise
const creditcode = enterprise.value?.creditcode ?? ''
const query = useLandDemandQuery(creditcode)
const record = query.data
const redirected = ref(false)
const submitted = computed(() => record.value?.landusedemand === '1')
const queryErrorMessage = computed(() => query.error.value?.message ?? '请返回首页后重试')
const recordBusinessName = computed(() => record.value?.businessname ?? '--')
const recordUpdateTime = computed(() => record.value?.updatetime ?? '--')

watchEffect(() => {
  if (!authorized.value || query.isPending.value || query.isError.value || submitted.value || redirected.value) {
    return
  }
  redirected.value = true
  void replace('/pages/home/index')
})

async function backHome(): Promise<void> {
  await replace('/pages/home/index')
}

async function viewDetail(): Promise<void> {
  await navigate('/pages/land-demand/index', { mode: 'view' })
}
</script>

<template>
  <PageShell
    v-if="authorized"
    title="填报完成"
    :subtitle="submitted ? '用地需求已提交' : '正在核验提交结果'"
    icon="list-check"
  >
    <AppLoading v-if="query.isPending" />
    <AppError
      v-else-if="query.isError"
      title="提交结果加载失败"
      :message="queryErrorMessage"
    />
    <view v-else-if="submitted" data-testid="submit-success" class="u-card land-demand-success__notice">
      <text class="land-demand-success__status">已提交</text>
      <text class="land-demand-success__copy">企业名称：{{ recordBusinessName }}</text>
      <text class="land-demand-success__copy">提交时间：{{ recordUpdateTime }}</text>
      <view class="land-demand-success__actions">
        <view data-testid="success-back-home" class="land-demand-success__action">
          <t-button
            data-testid="back-home"
            theme="default"
            block
            @tap="backHome"
          >
            返回首页
          </t-button>
        </view>
        <t-button
          data-testid="success-view-detail"
          theme="primary"
          block
          @tap="viewDetail"
        >
          查看填报信息
        </t-button>
      </view>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.land-demand-success__notice {
  padding: $space-4;
  text-align: center;
}

.land-demand-success__status,
.land-demand-success__copy {
  display: block;
}

.land-demand-success__status {
  font-size: 36rpx;
  font-weight: 700;
  color: $color-success;
}

.land-demand-success__copy {
  margin-top: $space-2;
  font-size: 28rpx;
  color: $color-text-secondary;
}

.land-demand-success__actions {
  display: flex;
  gap: $space-2;
  margin-top: $space-4;
}

.land-demand-success__action {
  flex: 1;
}
</style>

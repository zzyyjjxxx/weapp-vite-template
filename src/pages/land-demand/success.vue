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
    <view class="land-demand-success">
      <AppLoading v-if="query.isPending" />
      <AppError
        v-else-if="query.isError"
        title="提交结果加载失败"
        :message="queryErrorMessage"
      />
      <view v-else-if="submitted" data-testid="submit-success" class="u-card land-demand-success__notice">
        <view class="land-demand-success__mark">
          <text>✓</text>
        </view>
        <text class="land-demand-success__status">填报提交成功</text>
        <text class="land-demand-success__description">您的企业用地需求已进入服务流程，请留意后续联系。</text>
        <view class="land-demand-success__detail">
          <view class="land-demand-success__row">
            <text class="land-demand-success__label">企业名称</text>
            <text class="land-demand-success__copy">{{ recordBusinessName }}</text>
          </view>
          <view class="land-demand-success__row">
            <text class="land-demand-success__label">提交时间</text>
            <text class="land-demand-success__copy">{{ recordUpdateTime }}</text>
          </view>
          <view class="land-demand-success__row">
            <text class="land-demand-success__label">当前状态</text>
            <text class="land-demand-success__badge">已提交</text>
          </view>
        </view>
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
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.land-demand-success__notice {
  position: relative;
  padding: $space-6 $space-4 $space-4;
  overflow: hidden;
  text-align: center;
}

.land-demand-success__notice::before {
  position: absolute;
  top: -180rpx;
  left: 50%;
  width: 520rpx;
  height: 360rpx;
  content: '';
  background: rgb(36 104 242 / 8%);
  border-radius: 50%;
  transform: translateX(-50%);
}

.land-demand-success__status,
.land-demand-success__description,
.land-demand-success__copy,
.land-demand-success__label {
  display: block;
}

.land-demand-success__mark {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 112rpx;
  height: 112rpx;
  margin: 0 auto $space-3;
  font-size: 54rpx;
  font-weight: 700;
  color: #fff;
  background: $gradient-primary;
  border: 10rpx solid #e4efff;
  border-radius: 50%;
  box-shadow: $shadow-button;
}

.land-demand-success__status {
  font-size: 36rpx;
  font-weight: 700;
  color: $color-text;
}

.land-demand-success__description {
  width: 88%;
  margin: $space-2 auto 0;
  font-size: 24rpx;
  line-height: 1.65;
  color: $color-text-secondary;
}

.land-demand-success__detail {
  padding: $space-2 $space-3;
  margin-top: $space-4;
  text-align: left;
  background: $color-primary-faint;
  border: 1rpx solid #deebff;
  border-radius: $radius-md;
}

.land-demand-success__row {
  display: flex;
  gap: $space-3;
  align-items: flex-start;
  justify-content: space-between;
  padding: $space-2 0;
}

.land-demand-success__row + .land-demand-success__row {
  border-top: 1rpx solid #e1ebf8;
}

.land-demand-success__label {
  flex: 0 0 128rpx;
  font-size: 23rpx;
  color: $color-text-placeholder;
}

.land-demand-success__copy {
  flex: 1;
  font-size: 24rpx;
  color: $color-text;
  text-align: right;
}

.land-demand-success__badge {
  padding: 4rpx 14rpx;
  font-size: 21rpx;
  color: $color-success;
  background: $color-success-soft;
  border-radius: 999rpx;
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

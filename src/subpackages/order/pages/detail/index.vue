<script setup lang="ts">
import { computed, onLoad, ref } from 'wevu'

import AppError from '@/components/ui/app-error/index.vue'
import AppLoading from '@/components/ui/app-loading/index.vue'
import PageShell from '@/components/ui/page-shell/index.vue'
import { useCancelOrderMutation, useOrderDetailQuery } from '@/features/order/queries'
import { readOrderId } from '@/features/order/route'
import { getRouter } from '@/router'

definePageJson({
  navigationBarTitleText: '订单详情',
})

const orderId = ref('')
const cancelMutation = useCancelOrderMutation()
const detailQuery = useOrderDetailQuery(orderId)
const order = detailQuery.data
const isPending = detailQuery.isPending
const isError = detailQuery.isError
const isCancelPending = cancelMutation.isPending
const errorMessage = computed(() => detailQuery.error.value?.message ?? '订单详情暂时不可用。')
const cancelErrorMessage = computed(() => cancelMutation.error.value?.message ?? '')

onLoad((query) => {
  orderId.value = readOrderId(query)
})

async function goBack(): Promise<void> {
  await getRouter().back()
}

async function cancel(): Promise<void> {
  if (!orderId.value || !detailQuery.data.value?.canCancel) {
    return
  }
  await cancelMutation.mutateAsync(orderId.value)
}
</script>

<template>
  <PageShell
    title="订单详情"
    subtitle="详情缓存会在取消成功后同步更新"
  >
    <AppError
      v-if="!orderId"
      title="缺少订单 ID"
      message="请从订单列表进入详情页。"
      @retry="goBack"
    />
    <AppLoading v-else-if="isPending" />
    <AppError
      v-else-if="isError"
      :message="errorMessage"
      @retry="() => { void detailQuery.refetch() }"
    />
    <view v-else-if="order" class="order-detail">
      <view class="order-detail__summary u-card">
        <text class="order-detail__number">
          {{ order.number }}
        </text>
        <text class="order-detail__status">
          {{ order.statusLabel }}
        </text>
        <text class="order-detail__amount">
          ¥{{ order.amount.toFixed(2) }}
        </text>
      </view>

      <view class="order-detail__info u-card">
        <view class="order-detail__row">
          <text>订单 ID</text>
          <text>{{ order.id }}</text>
        </view>
        <view class="order-detail__row">
          <text>创建时间</text>
          <text>{{ order.createdAt }}</text>
        </view>
        <view class="order-detail__row">
          <text>状态</text>
          <text>{{ order.statusLabel }}</text>
        </view>
      </view>

      <text v-if="cancelErrorMessage" class="order-detail__error">
        {{ cancelErrorMessage }}
      </text>
      <button
        v-if="order.canCancel"
        class="order-detail__cancel"
        :disabled="isCancelPending"
        @tap="cancel"
      >
        {{ isCancelPending ? '取消中...' : '取消订单' }}
      </button>
      <button class="order-detail__back" @tap="goBack">
        返回列表
      </button>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.order-detail__summary {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.order-detail__number {
  font-size: 30rpx;
  font-weight: 600;
  color: $color-text;
}

.order-detail__status {
  padding: 8rpx 16rpx;
  margin-top: $space-2;
  font-size: 24rpx;
  color: $color-success;
  background: #e8fffb;
  border-radius: 999rpx;
}

.order-detail__amount {
  margin-top: $space-3;
  font-size: 56rpx;
  font-weight: 800;
  color: $color-text;
}

.order-detail__info {
  margin-top: $space-3;
}

.order-detail__row {
  display: flex;
  justify-content: space-between;
  padding: $space-2 0;
  font-size: 24rpx;
  color: $color-text-secondary;
  border-bottom: 2rpx solid $color-border;
}

.order-detail__row:last-child {
  border-bottom: 0;
}

.order-detail__row text:last-child {
  max-width: 64%;
  overflow: hidden;
  text-overflow: ellipsis;
  color: $color-text;
  white-space: nowrap;
}

.order-detail__error {
  display: block;
  margin-top: $space-2;
  font-size: 24rpx;
  line-height: 1.6;
  color: $color-error;
}

.order-detail__cancel,
.order-detail__back {
  margin-top: $space-3;
  border: 0;
  border-radius: $radius-sm;
}

.order-detail__cancel {
  color: #fff;
  background: $color-warning;
}

.order-detail__back {
  color: $color-primary;
  background: $color-primary-soft;
}
</style>

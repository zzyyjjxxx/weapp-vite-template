<script setup lang="ts">
import type { Order, OrderStatus } from '@/features/order/models'

import { computed, ref } from 'wevu'
import AppEmpty from '@/components/ui/app-empty/index.vue'
import AppError from '@/components/ui/app-error/index.vue'
import AppLoading from '@/components/ui/app-loading/index.vue'
import PageShell from '@/components/ui/page-shell/index.vue'
import { useOrderListQuery } from '@/features/order/queries'
import { navigate } from '@/router/navigation'

definePageJson({
  navigationBarTitleText: '订单列表',
})

const statusOptions: Array<{ label: string, value: OrderStatus | '' }> = [
  { label: '全部', value: '' },
  { label: '待处理', value: 'pending' },
  { label: '处理中', value: 'processing' },
  { label: '已完成', value: 'completed' },
  { label: '已取消', value: 'cancelled' },
]

const keywordDraft = ref('')
const statusDraft = ref<OrderStatus | ''>('')
const input = ref({
  page: 1,
  pageSize: 10,
  status: undefined as OrderStatus | undefined,
  keyword: undefined as string | undefined,
})
const orderQuery = useOrderListQuery(input)
const isPending = orderQuery.isPending
const isError = orderQuery.isError
const orders = computed(() => orderQuery.data.value?.items ?? [])
const pageSummary = computed(() => {
  const data = orderQuery.data.value
  return data ? `第 ${data.page} 页，共 ${data.total} 条` : ''
})
const canPrevious = computed(() => input.value.page > 1)
const canNext = computed(() => {
  const data = orderQuery.data.value
  return Boolean(data && data.page * data.pageSize < data.total)
})
const errorMessage = computed(() => orderQuery.error.value?.message ?? '订单列表暂时不可用。')

function readInputValue(event: unknown): string {
  if (typeof event !== 'object' || event === null || !('detail' in event)) {
    return ''
  }
  const detail = event.detail
  if (typeof detail !== 'object' || detail === null || !('value' in detail)) {
    return ''
  }
  return typeof detail.value === 'string' ? detail.value : ''
}

function updateKeyword(event: unknown): void {
  keywordDraft.value = readInputValue(event)
}

function chooseStatus(value: OrderStatus | ''): void {
  statusDraft.value = value
}

function applyFilters(): void {
  input.value = {
    page: 1,
    pageSize: input.value.pageSize,
    status: statusDraft.value || undefined,
    keyword: keywordDraft.value.trim() || undefined,
  }
}

function changePage(delta: number): void {
  if (delta < 0 && !canPrevious.value) {
    return
  }
  if (delta > 0 && !canNext.value) {
    return
  }
  input.value = {
    ...input.value,
    page: input.value.page + delta,
  }
}

function formatAmount(amount: number): string {
  return `¥${amount.toFixed(2)}`
}

function formatDate(value: string): string {
  return value.replace('T', ' ').replace('.000Z', '')
}

async function openDetail(order: Order): Promise<void> {
  await navigate('/subpackages/order/pages/detail/index', { id: order.id })
}
</script>

<template>
  <PageShell
    title="订单列表"
    subtitle="列表筛选、分页与 Query Core 缓存"
    icon="list-check"
  >
    <view class="order-list__filters u-card">
      <input
        class="order-list__keyword"
        :value="keywordDraft"
        placeholder="按订单号或 ID 搜索"
        @input="updateKeyword"
      >
      <scroll-view class="order-list__statuses" scroll-x>
        <view class="order-list__status-list">
          <button
            v-for="item in statusOptions"
            :key="item.value || 'all'"
            class="order-list__status"
            :class="{ 'order-list__status--active': statusDraft === item.value }"
            @tap="chooseStatus(item.value)"
          >
            {{ item.label }}
          </button>
        </view>
      </scroll-view>
      <button class="order-list__filter-button" @tap="applyFilters">
        应用筛选
      </button>
    </view>

    <AppLoading v-if="isPending" />
    <AppError
      v-else-if="isError"
      :message="errorMessage"
      @retry="() => { void orderQuery.refetch() }"
    />
    <AppEmpty
      v-else-if="orders.length === 0"
      title="没有找到订单"
      description="可以调整关键词或状态后再次查询。"
    />
    <view v-else class="order-list__items">
      <view
        v-for="order in orders"
        :key="order.id"
        class="order-list__item u-card"
        @tap="openDetail(order)"
      >
        <view class="order-list__item-header">
          <text class="order-list__number">
            {{ order.number }}
          </text>
          <text class="order-list__status-tag">
            {{ order.statusLabel }}
          </text>
        </view>
        <view class="order-list__item-body">
          <text class="order-list__date">
            {{ formatDate(order.createdAt) }}
          </text>
          <text class="order-list__amount">
            {{ formatAmount(order.amount) }}
          </text>
        </view>
      </view>
      <view class="order-list__pagination">
        <button
          class="order-list__page-button"
          :disabled="!canPrevious"
          @tap="changePage(-1)"
        >
          上一页
        </button>
        <text class="order-list__summary">
          {{ pageSummary }}
        </text>
        <button
          class="order-list__page-button"
          :disabled="!canNext"
          @tap="changePage(1)"
        >
          下一页
        </button>
      </view>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.order-list__filters {
  padding: $space-3;
}

.order-list__keyword {
  width: 100%;
  height: 76rpx;
  padding: 0 $space-2;
  font-size: 26rpx;
  background: #f7f8fa;
  border: 2rpx solid $color-border;
  border-radius: $radius-sm;
}

.order-list__statuses {
  width: 100%;
  margin-top: $space-2;
  white-space: nowrap;
}

.order-list__status-list {
  display: flex;
  gap: $space-1;
}

.order-list__status {
  display: inline-block;
  min-width: 112rpx;
  padding: 12rpx 20rpx;
  margin: 0;
  font-size: 24rpx;
  color: $color-text-secondary;
  background: #f2f3f5;
  border: 0;
  border-radius: 999rpx;
}

.order-list__status--active {
  color: $color-primary;
  background: $color-primary-soft;
}

.order-list__filter-button {
  margin-top: $space-2;
  color: #fff;
  background: $color-primary;
  border: 0;
  border-radius: $radius-sm;
}

.order-list__items {
  margin-top: $space-3;
}

.order-list__item {
  margin-bottom: $space-2;
}

.order-list__item-header,
.order-list__item-body,
.order-list__pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.order-list__number {
  font-size: 28rpx;
  font-weight: 600;
  color: $color-text;
}

.order-list__status-tag {
  padding: 6rpx 12rpx;
  font-size: 22rpx;
  color: $color-success;
  background: #e8fffb;
  border-radius: 999rpx;
}

.order-list__item-body {
  margin-top: $space-3;
}

.order-list__date {
  font-size: 24rpx;
  color: $color-text-secondary;
}

.order-list__amount {
  font-size: 32rpx;
  font-weight: 700;
  color: $color-text;
}

.order-list__pagination {
  gap: $space-2;
  margin-top: $space-3;
}

.order-list__page-button {
  min-width: 152rpx;
  padding: 12rpx 16rpx;
  margin: 0;
  font-size: 24rpx;
  color: $color-primary;
  background: $color-primary-soft;
  border: 0;
  border-radius: $radius-sm;
}

.order-list__summary {
  flex: 1;
  font-size: 22rpx;
  color: $color-text-secondary;
  text-align: center;
}
</style>

<script setup lang="ts">
import { computed } from 'wevu'

import PageShell from '@/components/ui/page-shell/index.vue'
import { navigate } from '@/router/navigation'
import { useAuthStore } from '@/stores/auth'

definePageJson({
  navigationBarTitleText: '首页',
})

const auth = useAuthStore()
const isAuthenticated = auth.isAuthenticated
const sessionText = computed(() => auth.isAuthenticated.value ? '已登录' : '未登录')

async function openOrders(): Promise<void> {
  await navigate('/subpackages/order/pages/list/index')
}

async function openProfile(): Promise<void> {
  await navigate('/pages/profile/index')
}

async function openLogin(): Promise<void> {
  await navigate('/pages/login/index', {
    returnTo: '/subpackages/order/pages/list/index',
  })
}
</script>

<template>
  <PageShell
    title="业务工作台"
    subtitle="weapp-vite + Wevu + Hono 的最小可运行垂直切片"
    icon="home"
  >
    <view class="home__hero">
      <text class="home__eyebrow">
        LOCAL SCAFFOLD
      </text>
      <text class="home__hero-title">
        从登录到订单取消
      </text>
      <text class="home__hero-description">
        页面通过 Query Core 读取服务端状态，认证和偏好由 Wevu Store 管理。
      </text>
    </view>

    <view class="home__status u-card">
      <text class="home__status-label">
        当前会话
      </text>
      <text class="home__status-value">
        {{ sessionText }}
      </text>
    </view>

    <view class="home__actions">
      <button class="u-button home__button home__button--primary" @tap="openOrders">
        查看订单
      </button>
      <button class="u-button home__button" @tap="openProfile">
        个人资料
      </button>
      <button
        v-if="!isAuthenticated"
        class="u-button home__button home__button--ghost"
        @tap="openLogin"
      >
        使用演示账号登录
      </button>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.home__hero {
  padding: $space-4;
  background: linear-gradient(140deg, #e8f3ff 0%, #f5f7fa 100%);
  border: 2rpx solid #c6e2ff;
  border-radius: $radius-lg;
}

.home__eyebrow {
  display: block;
  font-size: 20rpx;
  font-weight: 700;
  color: $color-primary;
  letter-spacing: 2rpx;
}

.home__hero-title {
  display: block;
  margin-top: $space-2;
  font-size: 38rpx;
  font-weight: 700;
  line-height: 1.3;
  color: $color-text;
}

.home__hero-description {
  display: block;
  margin-top: $space-2;
  font-size: 24rpx;
  line-height: 1.7;
  color: $color-text-secondary;
}

.home__status {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: $space-3;
}

.home__status-label {
  font-size: 26rpx;
  color: $color-text-secondary;
}

.home__status-value {
  font-size: 28rpx;
  font-weight: 600;
  color: $color-success;
}

.home__actions {
  margin-top: $space-4;
}

.home__button {
  color: $color-text;
  background: $color-card;
  border: 2rpx solid $color-border;
}

.home__button--primary {
  color: #fff;
  background: $color-primary;
  border-color: $color-primary;
}

.home__button--ghost {
  color: $color-primary;
  background: $color-primary-soft;
  border-color: $color-primary-soft;
}
</style>

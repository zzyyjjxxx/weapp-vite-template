<script setup lang="ts">
import PageShell from '@/components/ui/page-shell/index.vue'
import { navigate } from '@/router/navigation'

definePageJson({
  navigationBarTitleText: '页面异常',
})

definePageMeta({
  layout: false,
})

async function goHome(): Promise<void> {
  await navigate('/pages/home/index')
}
</script>

<template>
  <PageShell
    title="页面异常"
    subtitle="当前页面无法完成请求"
  >
    <view class="error-page u-card">
      <text class="error-page__code">
        404
      </text>
      <text class="error-page__message">
        你可以返回首页，或稍后重新打开业务页面。
      </text>
      <button class="error-page__button" @tap="goHome">
        返回首页
      </button>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.error-page {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
}

.error-page__code {
  font-size: 96rpx;
  font-weight: 800;
  color: $color-primary;
}

.error-page__message {
  margin-top: $space-2;
  font-size: 26rpx;
  line-height: 1.7;
  color: $color-text-secondary;
}

.error-page__button {
  width: 100%;
  margin-top: $space-4;
  color: #fff;
  background: $color-primary;
  border: 0;
  border-radius: $radius-sm;
}
</style>

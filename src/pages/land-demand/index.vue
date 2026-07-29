<script setup lang="ts">
import PageShell from '@/components/ui/page-shell/index.vue'

definePageJson({
  navigationBarTitleText: '用地需求填报',
})
</script>

<template>
  <PageShell title="用地需求填报" subtitle="填报功能即将开放" icon="list-check">
    <view class="u-card land-demand-entry__notice">
      <text>请稍候，正在准备填报表单。</text>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.land-demand-entry__notice {
  padding: $space-4;
  font-size: 28rpx;
  color: $color-text-secondary;
}
</style>

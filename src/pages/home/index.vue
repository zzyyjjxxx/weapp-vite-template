<script setup lang="ts">
import PageShell from '@/components/ui/page-shell/index.vue'

definePageJson({
  navigationBarTitleText: '用地需求',
})
</script>

<template>
  <PageShell title="用地需求" subtitle="产品服务正在准备中" icon="home">
    <view class="home__notice u-card">
      <text class="home__notice-title">土地需求申报</text>
      <text class="home__notice-copy">请稍候，申报入口即将开放。</text>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.home__notice {
  padding: $space-4;
}

.home__notice-title,
.home__notice-copy {
  display: block;
}

.home__notice-title {
  font-size: 36rpx;
  font-weight: 700;
  color: $color-text;
}

.home__notice-copy {
  margin-top: $space-2;
  font-size: 26rpx;
  color: $color-text-secondary;
}
</style>

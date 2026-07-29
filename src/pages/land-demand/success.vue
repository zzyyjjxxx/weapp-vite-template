<script setup lang="ts">
import PageShell from '@/components/ui/page-shell/index.vue'

definePageJson({
  navigationBarTitleText: '填报完成',
})
</script>

<template>
  <PageShell title="填报完成" subtitle="用地需求已提交" icon="list-check">
    <view class="u-card land-demand-success__notice">
      <text>感谢填报，相关部门将跟进服务。</text>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.land-demand-success__notice {
  padding: $space-4;
  font-size: 28rpx;
  color: $color-text-secondary;
}
</style>

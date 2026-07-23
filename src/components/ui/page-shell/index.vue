<script setup lang="ts">
const props = defineProps<{
  title: string
  subtitle?: string
}>()

defineComponentJson({
  component: true,
})
</script>

<template>
  <view class="page-shell">
    <view class="page-shell__header">
      <text class="page-shell__title">
        {{ props.title }}
      </text>
      <text
        v-if="props.subtitle"
        class="page-shell__subtitle"
      >
        {{ props.subtitle }}
      </text>
    </view>
    <view class="page-shell__body">
      <slot />
    </view>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.page-shell {
  min-height: 100vh;
  padding: $space-5 $space-4 $space-5;
  background: $color-page;
}

.page-shell__header {
  padding: $space-2 0 $space-4;
}

.page-shell__title {
  display: block;
  font-size: 44rpx;
  font-weight: 700;
  line-height: 1.25;
  color: $color-text;
}

.page-shell__subtitle {
  display: block;
  margin-top: $space-1;
  font-size: 24rpx;
  line-height: 1.6;
  color: $color-text-secondary;
}

.page-shell__body {
  min-height: 480rpx;
}
</style>

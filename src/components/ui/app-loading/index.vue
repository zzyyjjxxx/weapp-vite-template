<script setup lang="ts">
defineComponentJson({
  component: true,
})
</script>

<template>
  <view class="state state--loading">
    <view class="state__dot" />
    <text>加载中...</text>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.state {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 240rpx;
  font-size: 26rpx;
  color: $color-text-secondary;
}

.state__dot {
  width: 24rpx;
  height: 24rpx;
  margin-right: $space-2;
  border: 4rpx solid $color-primary-soft;
  border-top-color: $color-primary;
  border-radius: 50%;
}
</style>

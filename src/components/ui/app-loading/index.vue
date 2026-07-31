<script setup lang="ts">
defineComponentJson({
  component: true,
})
</script>

<template>
  <view class="state state--loading">
    <view class="state__dot">
      <view class="state__dot-core" />
    </view>
    <text class="state__copy">正在加载，请稍候</text>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 320rpx;
  padding: $space-4;
  font-size: 26rpx;
  color: $color-text-secondary;
  background: rgb(255 255 255 / 86%);
  border: 1rpx solid rgb(218 228 243 / 78%);
  border-radius: $radius-lg;
  box-shadow: $shadow-card;
}

.state__dot {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 68rpx;
  height: 68rpx;
  background: $color-primary-soft;
  border-radius: 50%;
}

.state__dot-core {
  width: 24rpx;
  height: 24rpx;
  border: 4rpx solid #bfd6ff;
  border-top-color: $color-primary;
  border-radius: 50%;
}

.state__copy {
  margin-top: $space-3;
}
</style>

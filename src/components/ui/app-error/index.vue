<script setup lang="ts">
const props = withDefaults(defineProps<{
  title?: string
  message?: string
}>(), {
  title: '加载失败',
  message: '请稍后重试。',
})

const emit = defineEmits<{
  retry: []
}>()

defineComponentJson({
  component: true,
})
</script>

<template>
  <view class="state state--error">
    <view class="state__mark">!</view>
    <text class="state__title">
      {{ props.title }}
    </text>
    <text class="state__message">
      {{ props.message }}
    </text>
    <button class="state__button" @tap="emit('retry')">
      重试
    </button>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 240rpx;
  padding: $space-4;
  text-align: center;
  background: rgb(255 255 255 / 90%);
  border: 1rpx solid #f3ddda;
  border-radius: $radius-lg;
  box-shadow: $shadow-card;
}

.state__mark {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 72rpx;
  height: 72rpx;
  margin-bottom: $space-2;
  font-size: 32rpx;
  font-weight: 700;
  color: $color-error;
  background: $color-error-soft;
  border-radius: 50%;
}

.state__title {
  font-size: 28rpx;
  font-weight: 600;
  color: $color-error;
}

.state__message {
  margin-top: $space-1;
  font-size: 24rpx;
  line-height: 1.6;
  color: $color-text-secondary;
}

.state__button {
  min-width: 180rpx;
  margin-top: $space-3;
  color: $color-primary;
  background: $color-primary-soft;
  border: 0;
  border-radius: $radius-md;
}
</style>

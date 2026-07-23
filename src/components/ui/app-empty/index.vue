<script setup lang="ts">
const props = withDefaults(defineProps<{
  title?: string
  description?: string
}>(), {
  title: '暂无数据',
  description: '当前没有可展示的内容。',
})

defineComponentJson({
  component: true,
})
</script>

<template>
  <view class="state state--empty">
    <text class="state__title">
      {{ props.title }}
    </text>
    <text class="state__description">
      {{ props.description }}
    </text>
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
}

.state__title {
  font-size: 28rpx;
  font-weight: 600;
  color: $color-text;
}

.state__description {
  margin-top: $space-1;
  font-size: 24rpx;
  line-height: 1.6;
  color: $color-text-secondary;
}
</style>

<script setup lang="ts">
import type { AppIconName } from '@/components/ui/app-icon/icons'
import AppIcon from '@/components/ui/app-icon/index.vue'

const props = defineProps<{
  title: string
  subtitle?: string
  icon?: AppIconName
  compact?: boolean
}>()

defineComponentJson({
  component: true,
})
</script>

<template>
  <view class="page-shell" :class="{ 'page-shell--compact': props.compact }">
    <view class="page-shell__glow page-shell__glow--left" />
    <view class="page-shell__glow page-shell__glow--right" />
    <view class="page-shell__content">
      <view class="page-shell__header">
        <view class="page-shell__heading">
          <view v-if="props.icon" class="page-shell__icon-wrap">
            <AppIcon
              class="page-shell__icon"
              :name="props.icon"
              :size="40"
              weight="Filled"
            />
          </view>
          <view class="page-shell__heading-copy">
            <text class="page-shell__eyebrow">企业用地需求服务</text>
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
        </view>
        <view class="page-shell__actions">
          <slot name="actions" />
        </view>
      </view>
      <view class="page-shell__body">
        <slot />
      </view>
    </view>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.page-shell {
  position: relative;
  min-height: 100vh;
  overflow: hidden;
  background: $gradient-page;
}

.page-shell__content {
  position: relative;
  z-index: 1;
  padding: $space-4 $space-4 $space-6;
}

.page-shell__glow {
  position: absolute;
  width: 420rpx;
  height: 420rpx;
  pointer-events: none;
  background: rgb(96 159 255 / 14%);
  border-radius: 50%;
  filter: blur(12rpx);
}

.page-shell__glow--left {
  top: -240rpx;
  left: -230rpx;
}

.page-shell__glow--right {
  top: 170rpx;
  right: -300rpx;
}

.page-shell__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  padding: $space-2 0 $space-4;
}

.page-shell__heading {
  display: flex;
  flex: 1;
  align-items: center;
  min-width: 0;
}

.page-shell__actions {
  flex: 0 0 auto;
  margin-left: $space-2;
}

.page-shell__icon {
  filter: brightness(0) invert(1);
}

.page-shell__icon-wrap {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 72rpx;
  height: 72rpx;
  margin-right: $space-2;
  background: $gradient-primary;
  border: 6rpx solid rgb(255 255 255 / 72%);
  border-radius: 22rpx;
  box-shadow: $shadow-button;
}

.page-shell__heading-copy {
  flex: 1;
  min-width: 0;
}

.page-shell__eyebrow {
  display: block;
  margin-bottom: 4rpx;
  font-size: 20rpx;
  font-weight: 600;
  color: $color-primary;
  letter-spacing: 2rpx;
}

.page-shell__title {
  display: block;
  font-size: 40rpx;
  font-weight: 700;
  line-height: 1.25;
  color: $color-text;
}

.page-shell__subtitle {
  display: block;
  margin-top: 6rpx;
  font-size: 24rpx;
  line-height: 1.5;
  color: $color-text-secondary;
}

.page-shell__body {
  min-height: 480rpx;
}

.page-shell--compact .page-shell__content {
  padding-top: 0;
  padding-bottom: $space-4;
}

.page-shell--compact .page-shell__header {
  min-height: 76rpx;
  padding: $space-2 0 $space-2;
}

.page-shell--compact .page-shell__icon-wrap {
  width: 56rpx;
  height: 56rpx;
  border-width: 4rpx;
  border-radius: 18rpx;
}

.page-shell--compact .page-shell__icon {
  width: 30rpx;
  height: 30rpx;
}

.page-shell--compact .page-shell__eyebrow {
  display: none;
}

.page-shell--compact .page-shell__title {
  font-size: 34rpx;
  line-height: 1.2;
}

.page-shell--compact .page-shell__subtitle {
  margin-top: 4rpx;
  font-size: 21rpx;
  line-height: 1.35;
}

.page-shell--compact .page-shell__body {
  min-height: 0;
}
</style>

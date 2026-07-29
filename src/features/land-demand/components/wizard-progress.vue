<script setup lang="ts">
import type { LandDemandStep } from '../step-controller'

const props = defineProps<{ currentStep: LandDemandStep }>()

defineComponentJson({ component: true })

const steps = ['基本信息', '用地信息', '项目信息', '融资联系人', '确认提交']
</script>

<template>
  <scroll-view class="wizard-progress" scroll-x enhanced show-scrollbar="false">
    <view class="wizard-progress__track">
      <view
        v-for="(label, index) in steps"
        :key="label"
        class="wizard-progress__step"
        :class="{ 'wizard-progress__step--active': index + 1 <= props.currentStep }"
      >
        <text class="wizard-progress__number">{{ index + 1 }}</text>
        <text class="wizard-progress__label">{{ label }}</text>
      </view>
    </view>
  </scroll-view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.wizard-progress {
  width: 100%;
  margin-bottom: $space-3;
  white-space: nowrap;
}

.wizard-progress__track {
  display: inline-flex;
  min-width: 100%;
  padding: $space-2;
  background: $color-card;
  border-radius: $radius-md;
}

.wizard-progress__step {
  display: flex;
  flex: 0 0 136rpx;
  flex-direction: column;
  align-items: center;
  color: $color-text-placeholder;
}

.wizard-progress__step--active {
  color: $color-primary;
}

.wizard-progress__number {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40rpx;
  height: 40rpx;
  font-size: 22rpx;
  color: $color-card;
  background: currentcolor;
  border-radius: 50%;
}

.wizard-progress__label {
  margin-top: $space-1;
  font-size: 22rpx;
}
</style>

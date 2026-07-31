<script setup lang="ts">
import type { LandDemandStep } from '../step-controller'

const props = defineProps<{ currentStep: LandDemandStep }>()

defineComponentJson({ component: true })

const steps = ['基本信息', '用地需求', '投资项目', '融资及联系人', '确认提交']
</script>

<template>
  <view class="wizard-progress">
    <view class="wizard-progress__track">
      <view
        v-for="(label, index) in steps"
        :key="label"
        class="wizard-progress__step"
        :class="{
          'wizard-progress__step--active': index + 1 <= props.currentStep,
          'wizard-progress__step--current': index + 1 === props.currentStep,
        }"
      >
        <view class="wizard-progress__indicator">
          <text class="wizard-progress__number">{{ index + 1 }}</text>
          <view v-if="index < steps.length - 1" class="wizard-progress__connector" />
        </view>
        <text class="wizard-progress__label">{{ label }}</text>
      </view>
    </view>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.wizard-progress {
  width: 100%;
  margin-bottom: $space-3;
}

.wizard-progress__track {
  box-sizing: border-box;
  display: flex;
  width: 100%;
  padding: $space-2 $space-1;
  overflow: hidden;
  background: rgb(255 255 255 / 88%);
  border: 1rpx solid rgb(220 230 245 / 90%);
  border-radius: $radius-md;
  box-shadow: 0 8rpx 24rpx rgb(38 77 143 / 7%);
}

.wizard-progress__step {
  display: flex;
  flex: 1 1 0;
  flex-direction: column;
  min-width: 0;
  color: $color-text-placeholder;
}

.wizard-progress__step--active {
  color: $color-primary;
}

.wizard-progress__indicator {
  display: flex;
  align-items: center;
  width: 100%;
}

.wizard-progress__number {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 38rpx;
  height: 38rpx;
  font-size: 20rpx;
  font-weight: 700;
  color: $color-text-placeholder;
  background: #edf1f7;
  border: 3rpx solid #f8fafc;
  border-radius: 50%;
}

.wizard-progress__connector {
  flex: 1;
  height: 2rpx;
  margin: 0 5rpx;
  background: #d7e0ed;
}

.wizard-progress__step--active .wizard-progress__number {
  color: #fff;
  background: $gradient-primary;
  border-color: #dceaff;
  box-shadow: 0 6rpx 14rpx rgb(36 104 242 / 24%);
}

.wizard-progress__step--active .wizard-progress__connector {
  background: #83affb;
}

.wizard-progress__step--current .wizard-progress__number {
  border-color: #bed6ff;
  box-shadow: 0 0 0 6rpx rgb(48 117 244 / 10%);
}

.wizard-progress__label {
  width: 100%;
  padding: 0 2rpx;
  margin-top: 8rpx;
  overflow: hidden;
  font-size: 18rpx;
  line-height: 1.35;
  text-align: center;
  white-space: normal;
}
</style>

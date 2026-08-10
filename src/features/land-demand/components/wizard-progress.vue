<script setup lang="ts">
import type { LandDemandStep } from '../step-controller'

const props = withDefaults(defineProps<{
  currentStep: LandDemandStep
  progressStep?: LandDemandStep
  incompleteSteps?: readonly LandDemandStep[]
}>(), {
  progressStep: 1,
  incompleteSteps: () => [],
})

defineComponentJson({ component: true })
</script>

<template>
  <view class="wizard-progress">
    <view class="wizard-progress__track">
      <view
        class="wizard-progress__step"
        :class="{
          'wizard-progress__step--active': 1 <= Math.max(props.currentStep, props.progressStep ?? props.currentStep),
          'wizard-progress__step--current': props.currentStep === 1,
          'wizard-progress__step--complete': props.currentStep === 1 && !props.incompleteSteps?.includes(1),
          'wizard-progress__step--incomplete': props.incompleteSteps?.includes(1) ?? false,
        }"
      >
        <view class="wizard-progress__indicator">
          <text class="wizard-progress__number">1</text>
        </view>
        <view class="wizard-progress__connector" />
        <text class="wizard-progress__label">基本信息</text>
      </view>
      <view
        class="wizard-progress__step"
        :class="{
          'wizard-progress__step--active': 2 <= Math.max(props.currentStep, props.progressStep ?? props.currentStep),
          'wizard-progress__step--current': props.currentStep === 2,
          'wizard-progress__step--complete': props.currentStep === 2 && !props.incompleteSteps?.includes(2),
          'wizard-progress__step--incomplete': props.incompleteSteps?.includes(2) ?? false,
        }"
      >
        <view class="wizard-progress__indicator">
          <text class="wizard-progress__number">2</text>
        </view>
        <view class="wizard-progress__connector" />
        <text class="wizard-progress__label">用地需求</text>
      </view>
      <view
        class="wizard-progress__step"
        :class="{
          'wizard-progress__step--active': 3 <= Math.max(props.currentStep, props.progressStep ?? props.currentStep),
          'wizard-progress__step--current': props.currentStep === 3,
          'wizard-progress__step--complete': props.currentStep === 3 && !props.incompleteSteps?.includes(3),
          'wizard-progress__step--incomplete': props.incompleteSteps?.includes(3) ?? false,
        }"
      >
        <view class="wizard-progress__indicator">
          <text class="wizard-progress__number">3</text>
        </view>
        <view class="wizard-progress__connector" />
        <text class="wizard-progress__label">投资项目</text>
      </view>
      <view
        class="wizard-progress__step"
        :class="{
          'wizard-progress__step--active': 4 <= Math.max(props.currentStep, props.progressStep ?? props.currentStep),
          'wizard-progress__step--current': props.currentStep === 4,
          'wizard-progress__step--complete': props.currentStep === 4 && !props.incompleteSteps?.includes(4),
          'wizard-progress__step--incomplete': props.incompleteSteps?.includes(4) ?? false,
        }"
      >
        <view class="wizard-progress__indicator">
          <text class="wizard-progress__number">4</text>
        </view>
        <view class="wizard-progress__connector" />
        <text class="wizard-progress__label">融资及联系人</text>
      </view>
      <view
        class="wizard-progress__step"
        :class="{
          'wizard-progress__step--active': 5 <= Math.max(props.currentStep, props.progressStep ?? props.currentStep),
          'wizard-progress__step--current': props.currentStep === 5,
          'wizard-progress__step--complete': props.currentStep === 5,
        }"
      >
        <view class="wizard-progress__indicator">
          <text class="wizard-progress__number">5</text>
        </view>
        <text class="wizard-progress__label">确认提交</text>
      </view>
    </view>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.wizard-progress {
  width: 100%;
  margin-bottom: $space-2;
}

.wizard-progress__track {
  box-sizing: border-box;
  display: flex;
  width: 100%;
  padding: $space-2 0 $space-1;
  overflow: hidden;
  background: transparent;
}

.wizard-progress__step {
  position: relative;
  display: flex;
  flex: 1 1 0;
  flex-direction: column;
  align-items: center;
  min-width: 0;
  color: $color-text-placeholder;
}

.wizard-progress__step--active {
  color: $color-primary;
}

.wizard-progress__indicator {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 46rpx;
  height: 46rpx;
  margin: 0 auto;
}

.wizard-progress__number {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 42rpx;
  height: 42rpx;
  font-size: 21rpx;
  font-weight: 700;
  color: $color-text-placeholder;
  background: #edf1f7;
  border: 3rpx solid #f8fafc;
  border-radius: 50%;
}

.wizard-progress__connector {
  position: absolute;
  top: 22rpx;
  left: calc(50% + 23rpx);
  z-index: 0;
  width: calc(100% - 46rpx);
  height: 2rpx;
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

.wizard-progress__step--complete {
  color: $color-success;
}

.wizard-progress__step--complete .wizard-progress__number {
  color: #fff;
  background: $color-success;
  border-color: $color-success-soft;
  box-shadow: 0 6rpx 14rpx rgb(10 168 117 / 24%);
}

.wizard-progress__step--complete .wizard-progress__connector {
  background: $color-success;
}

.wizard-progress__step--incomplete {
  color: $color-error;
}

.wizard-progress__step--incomplete .wizard-progress__number {
  color: #fff;
  background: $color-error;
  border-color: $color-error-soft;
  box-shadow: 0 6rpx 14rpx rgb(213 73 65 / 24%);
}

.wizard-progress__step--incomplete .wizard-progress__connector {
  background: $color-error;
}

.wizard-progress__label {
  box-sizing: border-box;
  width: 100%;
  min-height: 52rpx;
  padding: 0 4rpx;
  margin-top: 8rpx;
  overflow: hidden;
  font-size: 20rpx;
  font-weight: 500;
  line-height: 1.35;
  text-align: center;
  white-space: normal;
}
</style>

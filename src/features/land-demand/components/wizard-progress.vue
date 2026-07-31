<script setup lang="ts">
import type { LandDemandStep } from '../step-controller'

const props = defineProps<{ currentStep: LandDemandStep }>()

defineComponentJson({ component: true })

const steps = ['基本信息', '用地需求', '投资项目', '融资及联系人', '确认提交']
</script>

<template>
  <scroll-view class="wizard-progress" scroll-x enhanced show-scrollbar="false">
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
  </scroll-view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.wizard-progress {
  width: 100%;
  margin-bottom: $space-4;
  white-space: nowrap;
}

.wizard-progress__track {
  display: inline-flex;
  min-width: 100%;
  padding: $space-3 $space-2;
  background: rgb(255 255 255 / 88%);
  border: 1rpx solid rgb(220 230 245 / 90%);
  border-radius: $radius-lg;
  box-shadow: 0 10rpx 30rpx rgb(38 77 143 / 8%);
}

.wizard-progress__step {
  display: flex;
  flex: 0 0 142rpx;
  flex-direction: column;
  color: $color-text-placeholder;
}

.wizard-progress__step--active {
  color: $color-primary;
}

.wizard-progress__indicator {
  display: flex;
  align-items: center;
  width: 100%;
  padding-left: 46rpx;
}

.wizard-progress__number {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 44rpx;
  height: 44rpx;
  font-size: 22rpx;
  font-weight: 700;
  color: $color-text-placeholder;
  background: #edf1f7;
  border: 4rpx solid #f8fafc;
  border-radius: 50%;
}

.wizard-progress__connector {
  flex: 1;
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

.wizard-progress__label {
  width: 136rpx;
  margin-top: 10rpx;
  font-size: 20rpx;
  text-align: center;
}
</style>

<script setup lang="ts">
import type { LandDemandStep } from '../step-controller'

const props = withDefaults(defineProps<{
  currentStep: LandDemandStep
  saving?: boolean
  transitioning?: boolean
}>(), {
  saving: false,
  transitioning: false,
})
const emit = defineEmits<{
  previous: []
  save: []
  next: []
}>()

function handlePrevious(): void {
  emit('previous')
}

function handleSave(): void {
  emit('save')
}

function handleNext(): void {
  emit('next')
}

defineComponentJson({ component: true })
</script>

<template>
  <view class="wizard-actions">
    <t-button
      v-if="props.currentStep > 1"
      data-testid="wizard-previous"
      class="wizard-actions__button"
      theme="default"
      variant="outline"
      block
      :disabled="props.saving || props.transitioning"
      @tap="handlePrevious"
    >
      上一步
    </t-button>
    <t-button
      data-testid="save-draft"
      class="wizard-actions__button"
      theme="default"
      block
      :disabled="props.saving || props.transitioning"
      @tap="handleSave"
    >
      暂存
    </t-button>
    <t-button
      v-if="props.currentStep < 5"
      data-testid="next-step"
      class="wizard-actions__button"
      theme="primary"
      block
      :disabled="props.saving || props.transitioning"
      @tap="handleNext"
    >
      下一步
    </t-button>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.wizard-actions {
  position: fixed;
  right: 0;
  bottom: 0;
  left: 0;
  z-index: 20;
  display: flex;
  gap: $space-2;
  padding: $space-3 $space-4 calc($space-3 + env(safe-area-inset-bottom));
  margin: 0;
  background: rgb(255 255 255 / 96%);
  border-top: 1rpx solid rgb(218 227 241 / 86%);
  box-shadow: 0 -12rpx 36rpx rgb(30 62 112 / 8%);
}

.wizard-actions__button {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  border-radius: $radius-md;
}

.wizard-actions__button:last-child {
  box-shadow: $shadow-button;
}
</style>

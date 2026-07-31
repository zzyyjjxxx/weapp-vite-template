<script setup lang="ts">
import type { LandDemandStep } from '../step-controller'

const props = withDefaults(defineProps<{
  currentStep: LandDemandStep
  saving?: boolean
}>(), {
  saving: false,
})
const emit = defineEmits<{
  previous: []
  save: []
  next: []
}>()

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
      @tap="emit('previous')"
    >
      上一步
    </t-button>
    <t-button
      data-testid="save-draft"
      class="wizard-actions__button"
      theme="default"
      :loading="props.saving"
      :disabled="props.saving"
      @tap="emit('save')"
    >
      暂存
    </t-button>
    <t-button
      v-if="props.currentStep < 5"
      data-testid="next-step"
      class="wizard-actions__button"
      theme="primary"
      :disabled="props.saving"
      @tap="emit('next')"
    >
      下一步
    </t-button>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.wizard-actions {
  position: sticky;
  bottom: 0;
  z-index: 20;
  display: flex;
  gap: $space-2;
  padding: $space-3 $space-4 calc($space-3 + env(safe-area-inset-bottom));
  margin: $space-4 (-$space-4) (-$space-6);
  background: rgb(255 255 255 / 96%);
  border-top: 1rpx solid rgb(218 227 241 / 86%);
  box-shadow: 0 -12rpx 36rpx rgb(30 62 112 / 8%);
}

.wizard-actions__button {
  flex: 1;
  overflow: hidden;
  border-radius: $radius-md;
}

.wizard-actions__button:last-child {
  box-shadow: $shadow-button;
}
</style>

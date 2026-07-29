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
      data-testid="wizard-save"
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
      data-testid="wizard-next"
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
  z-index: 10;
  display: flex;
  gap: $space-2;
  padding: $space-3 0 calc($space-3 + env(safe-area-inset-bottom));
  margin-top: $space-3;
  background: $color-page;
}

.wizard-actions__button {
  flex: 1;
}
</style>

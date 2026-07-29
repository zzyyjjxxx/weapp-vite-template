<script setup lang="ts">
import type { VerificationChallenge } from '../models'

import { readStringDetail } from '@/platform/event-detail'

const props = defineProps<{
  visible: boolean
  challenge?: VerificationChallenge
  code: string
  loading?: boolean
  error?: string
}>()
const emit = defineEmits<{
  change: [code: string]
  close: []
  submit: []
}>()

defineComponentJson({ component: true })

function close(): void {
  if (!props.loading) {
    emit('close')
  }
}
</script>

<template>
  <t-dialog
    :visible="props.visible"
    title="法人手机号验证"
    :close-on-overlay-click="false"
    :confirm-btn="false"
    :cancel-btn="false"
    @close="close"
  >
    <view class="verification-dialog">
      <text class="verification-dialog__copy">
        验证码已发送至 {{ props.challenge?.phone ?? '' }}
      </text>
      <t-input
        data-testid="verification-code"
        label="六位验证码"
        type="number"
        :maxlength="6"
        :value="props.code"
        :disabled="props.loading"
        @change="emit('change', readStringDetail($event))"
      />
      <view data-testid="mock-code" class="verification-dialog__mock">
        <text>Mock 测试验证码</text>
        <text>{{ props.challenge?.mockCode ?? '' }}</text>
      </view>
      <text v-if="props.error" class="verification-dialog__error">{{ props.error }}</text>
      <view class="verification-dialog__actions">
        <t-button
          theme="default"
          variant="outline"
          :disabled="props.loading"
          @tap="close"
        >
          取消
        </t-button>
        <t-button
          data-testid="verification-submit"
          theme="primary"
          :loading="props.loading"
          :disabled="props.loading || props.code.length !== 6"
          @tap="emit('submit')"
        >
          确认提交
        </t-button>
      </view>
    </view>
  </t-dialog>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.verification-dialog__copy,
.verification-dialog__error {
  display: block;
  font-size: 25rpx;
  line-height: 1.5;
}

.verification-dialog__copy {
  margin-bottom: $space-2;
  color: $color-text-secondary;
}

.verification-dialog__mock {
  display: flex;
  justify-content: space-between;
  padding: $space-2;
  margin-top: $space-2;
  font-size: 24rpx;
  color: $color-primary;
  background: $color-primary-soft;
  border-radius: $radius-sm;
}

.verification-dialog__error {
  margin-top: $space-2;
  color: $color-error;
}

.verification-dialog__actions {
  display: flex;
  gap: $space-2;
  justify-content: flex-end;
  margin-top: $space-3;
}
</style>

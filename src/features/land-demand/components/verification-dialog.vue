<script setup lang="ts">
import type { VerificationChallenge } from '../models'

import { computed } from 'wevu'
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
const challengePhone = computed(() => props.challenge?.phone ?? '')
const challengeMockCode = computed(() => props.challenge?.mockCode ?? '')
const submitDisabled = computed(() => Boolean(props.loading) || props.code.length !== 6)

defineComponentJson({ component: true })

function close(): void {
  if (!props.loading) {
    emit('close')
  }
}
</script>

<template>
  <view
    v-if="props.visible"
    data-testid="verification-dialog"
    class="verification-dialog-overlay"
  >
    <view class="verification-dialog" role="dialog" aria-label="法人手机号验证">
      <text class="verification-dialog__title">法人手机号验证</text>
      <text class="verification-dialog__copy">
        验证码已发送至 {{ challengePhone }}
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
        <text>{{ challengeMockCode }}</text>
      </view>
      <text v-if="props.error" class="verification-dialog__error">{{ props.error }}</text>
      <view class="verification-dialog__actions">
        <t-button
          class="verification-dialog__button"
          theme="default"
          variant="outline"
          :disabled="props.loading"
          @tap="close"
        >
          取消
        </t-button>
        <t-button
          data-testid="verification-submit"
          class="verification-dialog__button"
          theme="primary"
          :loading="props.loading"
          :disabled="submitDisabled"
          @tap="emit('submit')"
        >
          确认提交
        </t-button>
      </view>
    </view>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.verification-dialog-overlay {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: $space-4;
  background: rgb(0 0 0 / 55%);
}

.verification-dialog {
  width: 100%;
  padding: $space-5 $space-4 $space-4;
  background: $color-card;
  border-radius: $radius-lg;
  box-shadow: 0 16rpx 48rpx rgb(0 0 0 / 18%);
}

.verification-dialog__title {
  display: block;
  margin-bottom: $space-4;
  font-size: 34rpx;
  font-weight: 700;
  color: $color-text;
  text-align: center;
}

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

.verification-dialog__button {
  flex: 1;
}
</style>

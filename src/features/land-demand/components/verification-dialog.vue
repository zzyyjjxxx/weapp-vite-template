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
  <t-dialog
    :visible="props.visible"
    title="法人手机号验证"
    content=""
    :close-on-overlay-click="false"
    :confirm-btn="false"
    :cancel-btn="false"
    @close="close"
  >
    <view slot="content" class="verification-dialog">
      <view class="verification-dialog__mark">验</view>
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
        status="default"
        tips=""
        @change="emit('change', readStringDetail($event))"
      />
      <view data-testid="mock-code" class="verification-dialog__mock">
        <text>Mock 测试验证码</text>
        <text>{{ challengeMockCode }}</text>
      </view>
      <text v-if="props.error" class="verification-dialog__error">{{ props.error }}</text>
    </view>
    <template #cancel-btn>
      <t-button
        class="verification-dialog__action"
        theme="default"
        variant="text"
        :disabled="props.loading"
        @tap="close"
      >
        取消
      </t-button>
    </template>
    <template #confirm-btn>
      <t-button
        data-testid="verification-submit"
        class="verification-dialog__action"
        theme="primary"
        :loading="props.loading"
        :disabled="submitDisabled"
        @tap="emit('submit')"
      >
        确认提交
      </t-button>
    </template>
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

.verification-dialog__mark {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 72rpx;
  height: 72rpx;
  margin: 0 auto $space-2;
  font-size: 26rpx;
  font-weight: 700;
  color: #fff;
  background: $gradient-primary;
  border-radius: 22rpx;
  box-shadow: $shadow-button;
}

.verification-dialog__copy {
  margin-bottom: $space-2;
  color: $color-text-secondary;
  text-align: center;
}

.verification-dialog__mock {
  display: flex;
  justify-content: space-between;
  padding: $space-2;
  margin-top: $space-2;
  font-size: 24rpx;
  color: $color-primary;
  background: $color-primary-faint;
  border: 1rpx solid #deebff;
  border-radius: $radius-md;
}

.verification-dialog__error {
  margin-top: $space-2;
  color: $color-error;
}

.verification-dialog__action {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  border-radius: $radius-md;
}
</style>

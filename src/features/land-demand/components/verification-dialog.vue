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
    title=""
    content=""
    :close-on-overlay-click="false"
    :confirm-btn="false"
    :cancel-btn="false"
    @close="close"
  >
    <template #title>
      <view class="verification-dialog__title">
        <view class="verification-dialog__mark">验</view>
        <text>法人手机号验证</text>
      </view>
    </template>
    <view slot="content" class="verification-dialog">
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
    <view slot="cancel-btn" class="verification-dialog__slot-action">
      <t-button
        class="verification-dialog__action"
        block
        theme="default"
        variant="text"
        :disabled="props.loading"
        @tap="close"
      >
        取消
      </t-button>
    </view>
    <view slot="confirm-btn" class="verification-dialog__slot-action">
      <t-button
        data-testid="verification-submit"
        class="verification-dialog__action"
        block
        theme="primary"
        :loading="props.loading"
        :disabled="submitDisabled"
        @tap="emit('submit')"
      >
        确认提交
      </t-button>
    </view>
  </t-dialog>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.verification-dialog__title,
.verification-dialog__copy,
.verification-dialog__error {
  display: block;
  font-size: 25rpx;
  line-height: 1.5;
}

.verification-dialog__title {
  display: flex;
  gap: $space-2;
  align-items: center;
  justify-content: center;
  font-size: 30rpx;
  font-weight: 700;
  color: $color-text;
  text-align: center;
}

.verification-dialog__mark {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 52rpx;
  height: 52rpx;
  font-size: 24rpx;
  font-weight: 700;
  color: #fff;
  background: $gradient-primary;
  border-radius: 22rpx;
  box-shadow: $shadow-button;
}

.verification-dialog__copy {
  margin: $space-3 0 $space-2;
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

.verification-dialog__slot-action {
  flex: 1;
  min-width: 0;
  overflow: hidden;
}

.verification-dialog__slot-action + .verification-dialog__slot-action {
  margin-left: 24rpx;
}

.verification-dialog__action {
  width: 100%;
  border-radius: $radius-md;
}
</style>

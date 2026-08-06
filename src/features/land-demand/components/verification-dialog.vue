<script setup lang="ts">
import type { VerificationChallenge } from '../models'

import { computed, onUnmounted, ref, watch } from 'wevu'
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
  resend: []
  submit: []
}>()
const challengePhone = computed(() => props.challenge?.phone ?? '')
const description = computed(() => challengePhone.value
  ? `已发送至 ${challengePhone.value}`
  : '')
const retryCountdown = ref(0)
const resendDisabled = computed(() => (
  props.loading || retryCountdown.value > 0 || !challengePhone.value
))
const resendLabel = computed(() => retryCountdown.value > 0
  ? `${retryCountdown.value}秒`
  : '重新发送')
const submitDisabled = computed(() => Boolean(props.loading) || props.code.length !== 6)
let countdownTimer: ReturnType<typeof setInterval> | undefined

defineComponentJson({ component: true, styleIsolation: 'apply-shared' })

function clearCountdown(): void {
  if (countdownTimer !== undefined) {
    clearInterval(countdownTimer)
    countdownTimer = undefined
  }
}

function updateCountdown(): void {
  const retryAt = props.challenge?.retryAt ?? 0
  retryCountdown.value = Math.max(0, Math.ceil((retryAt - Date.now()) / 1000))
  if (retryCountdown.value === 0) {
    clearCountdown()
  }
}

function restartCountdown(): void {
  clearCountdown()
  if (!props.visible || !props.challenge) {
    retryCountdown.value = 0
    return
  }

  updateCountdown()
  if (retryCountdown.value > 0) {
    countdownTimer = setInterval(updateCountdown, 1000)
  }
}

watch(() => props.challenge?.retryAt, restartCountdown, { immediate: true })
watch(() => props.visible, restartCountdown)
onUnmounted(clearCountdown)

function close(): void {
  if (!props.loading) {
    emit('close')
  }
}

function confirm(): void {
  if (!submitDisabled.value) {
    emit('submit')
  }
}

function resend(): void {
  if (!resendDisabled.value) {
    emit('resend')
  }
}
</script>

<template>
  <t-dialog
    v-if="props.visible || props.challenge"
    :visible="props.visible"
    title="法人手机号验证"
    :content="description || ''"
    t-class-content="verification-dialog__dialog-content"
    button-layout="horizontal"
    :close-on-overlay-click="false"
    @close="close"
  >
    <view slot="content" class="verification-dialog">
      <view class="verification-dialog__code-row">
        <view class="verification-dialog__input-wrap">
          <t-input
            data-testid="verification-code"
            t-class="verification-dialog__input"
            t-class-input="verification-dialog__input-control"
            type="number"
            :maxlength="6"
            :value="props.code"
            :disabled="props.loading"
            align="left"
            placeholder="请输入验证码"
            status="default"
            tips=""
            @change="emit('change', readStringDetail($event))"
          />
        </view>
        <view class="verification-dialog__resend-wrap">
          <t-button
            data-testid="verification-resend"
            t-class="verification-dialog__resend-button"
            theme="primary"
            variant="text"
            size="extra-small"
            :disabled="resendDisabled"
            @tap="resend"
          >
            {{ resendLabel }}
          </t-button>
        </view>
      </view>
      <text v-if="props.error" class="verification-dialog__error">{{ props.error }}</text>
    </view>
    <view slot="cancel-btn" class="verification-dialog__slot-action">
      <t-button
        data-testid="verification-cancel"
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
        variant="text"
        :disabled="submitDisabled"
        @tap="confirm"
      >
        提交
      </t-button>
    </view>
  </t-dialog>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.verification-dialog__dialog-content {
  box-sizing: border-box;
  padding-right: 16rpx;
  padding-left: 32rpx;
}

.verification-dialog {
  box-sizing: border-box;
  width: 100%;
}

.verification-dialog__error {
  display: block;
  font-size: 25rpx;
  line-height: 1.5;
}

.verification-dialog__code-row {
  position: relative;
  box-sizing: border-box;
  display: block;
  width: 100%;
  margin-top: $space-2;
}

.verification-dialog__input-wrap {
  box-sizing: border-box;
  width: 100%;
  padding-right: 96rpx;
}

.verification-dialog__input {
  box-sizing: border-box;
  display: block;
  width: 100%;
  min-width: 0;

  --td-input-vertical-padding: 8rpx 32rpx;
}

.verification-dialog__input-control {
  box-sizing: border-box;
  display: block;
  width: 100%;
  min-width: 0;
}

.verification-dialog__resend-wrap {
  position: absolute;
  top: 0;
  right: 0;
  box-sizing: border-box;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  width: 88rpx;
  height: 64rpx;
}

.verification-dialog__resend-button {
  display: block;
  width: auto;
  min-width: 0;
  padding-right: 0;
  padding-left: 0;
  font-size: 22rpx;
  line-height: 1.4;
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

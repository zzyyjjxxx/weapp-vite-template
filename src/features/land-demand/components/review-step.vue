<script setup lang="ts">
import type { LandDemandForm } from '../models'

import { readBooleanDetail } from '@/platform/event-detail'
import { buildReviewGroups } from '../review'

const props = defineProps<{
  form: LandDemandForm
  accepted: boolean
  acceptanceError?: string
  submitting?: boolean
}>()
const emit = defineEmits<{
  edit: [step: 1 | 2 | 3 | 4]
  accept: [value: boolean]
  submit: []
}>()
defineComponentJson({ component: true })
</script>

<template>
  <view class="review-step">
    <view
      v-for="group in buildReviewGroups(props.form)"
      :key="group.step"
      class="review-step__group u-card"
    >
      <view class="review-step__heading">
        <text class="review-step__title">{{ group.title }}</text>
        <t-button
          size="small"
          theme="primary"
          variant="text"
          @tap="emit('edit', group.step)"
        >
          修改
        </t-button>
      </view>
      <view v-for="item in group.items" :key="item.field" class="review-step__item">
        <text class="review-step__label">{{ item.label }}</text>
        <text class="review-step__value">{{ item.value }}</text>
      </view>
    </view>

    <view class="review-step__promise u-card">
      <t-checkbox
        data-testid="review-accept"
        :checked="props.accepted"
        @change="emit('accept', readBooleanDetail($event))"
      >
        本企业承诺所填写的信息真实、准确、完整，并同意相关部门根据项目服务需要使用以上信息。
      </t-checkbox>
      <text v-if="props.acceptanceError" class="review-step__error">
        {{ props.acceptanceError }}
      </text>
      <t-button
        data-testid="review-submit"
        class="review-step__submit"
        theme="primary"
        block
        :loading="props.submitting"
        :disabled="props.submitting"
        @tap="emit('submit')"
      >
        验证并提交
      </t-button>
    </view>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.review-step__group,
.review-step__promise {
  padding: $space-4;
}

.review-step__group + .review-step__group,
.review-step__promise {
  margin-top: $space-3;
}

.review-step__heading,
.review-step__item {
  display: flex;
  gap: $space-3;
  align-items: flex-start;
  justify-content: space-between;
}

.review-step__title {
  font-size: 32rpx;
  font-weight: 700;
  color: $color-text;
}

.review-step__item {
  padding: $space-2 0;
  border-bottom: 1rpx solid $color-border;
}

.review-step__label,
.review-step__value,
.review-step__error {
  font-size: 25rpx;
  line-height: 1.5;
}

.review-step__label {
  flex: 0 0 220rpx;
  color: $color-text-secondary;
}

.review-step__value {
  color: $color-text;
  text-align: right;
  overflow-wrap: anywhere;
}

.review-step__error {
  display: block;
  margin-top: $space-2;
  color: $color-error;
}

.review-step__submit {
  margin-top: $space-3;
}
</style>

<script setup lang="ts">
import type { LandDemandForm } from '../models'

import { readBooleanDetail } from '@/platform/event-detail'
import { buildReviewGroups } from '../review'

const props = defineProps<{
  form: LandDemandForm
  accepted: boolean
  acceptanceError?: string
  submitting?: boolean
  readonly?: boolean
}>()
const emit = defineEmits<{
  edit: [step: 1 | 2 | 3 | 4]
  accept: [value: boolean]
  submit: []
}>()
defineComponentJson({ component: true, styleIsolation: 'apply-shared' })
</script>

<template>
  <view class="review-step">
    <view class="review-step__overview">
      <view class="review-step__overview-mark">✓</view>
      <view>
        <text class="review-step__overview-title">
          {{ props.readonly ? '已提交信息' : '请确认填报信息' }}
        </text>
        <text class="review-step__overview-copy">
          {{ props.readonly ? '以下内容为当前企业已提交的用地需求' : '请逐项核对，发现问题可返回对应步骤修改' }}
        </text>
      </view>
    </view>
    <view
      v-for="(group, groupIndex) in buildReviewGroups(props.form)"
      :key="group.step"
      class="review-step__group u-card"
    >
      <view class="review-step__heading">
        <view class="review-step__heading-copy">
          <text class="review-step__number">{{ groupIndex + 1 }}</text>
          <text class="review-step__title">{{ group.title }}</text>
        </view>
        <t-button
          v-if="!props.readonly"
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

    <view v-if="!props.readonly" class="review-step__promise u-card">
      <text class="review-step__promise-title">真实性承诺</text>
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

.review-step__overview {
  display: flex;
  gap: $space-3;
  align-items: center;
  padding: $space-4;
  margin-bottom: $space-3;
  color: #fff;
  background: $gradient-primary;
  border-radius: $radius-lg;
  box-shadow: $shadow-button;
}

.review-step__overview-mark {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 64rpx;
  height: 64rpx;
  font-size: 34rpx;
  font-weight: 700;
  background: rgb(255 255 255 / 18%);
  border: 2rpx solid rgb(255 255 255 / 42%);
  border-radius: 50%;
}

.review-step__overview-title,
.review-step__overview-copy,
.review-step__promise-title {
  display: block;
}

.review-step__overview-title {
  font-size: 30rpx;
  font-weight: 700;
}

.review-step__overview-copy {
  margin-top: 4rpx;
  font-size: 22rpx;
  line-height: 1.5;
  color: rgb(255 255 255 / 82%);
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

.review-step__heading {
  padding-bottom: $space-2;
  border-bottom: 1rpx solid $color-border-soft;
}

.review-step__heading-copy {
  display: flex;
  align-items: center;
}

.review-step__number {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 42rpx;
  height: 42rpx;
  margin-right: $space-2;
  font-size: 21rpx;
  font-weight: 700;
  color: $color-primary;
  background: $color-primary-soft;
  border-radius: 14rpx;
}

.review-step__title {
  font-size: 30rpx;
  font-weight: 700;
  color: $color-text;
}

.review-step__item {
  padding: 18rpx 0;
  border-bottom: 1rpx solid $color-border-soft;
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
  flex: 1;
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
  overflow: hidden;
  border-radius: $radius-md;
  box-shadow: $shadow-button;
}

.review-step__promise-title {
  margin-bottom: $space-3;
  font-size: 29rpx;
  font-weight: 700;
  color: $color-text;
}
</style>

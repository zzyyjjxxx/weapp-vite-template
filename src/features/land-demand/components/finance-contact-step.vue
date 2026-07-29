<script setup lang="ts">
import type { FieldError, FinancingChoice, LandDemandForm } from '../models'

const props = defineProps<{ form: LandDemandForm, errors: readonly FieldError[] }>()
const emit = defineEmits<{ change: [patch: Partial<LandDemandForm>] }>()

defineComponentJson({ component: true })

function readStringDetail(event: unknown): string {
  if (typeof event !== 'object' || event === null || !('detail' in event)) {
    return ''
  }
  const detail = event.detail
  if (typeof detail !== 'object' || detail === null || !('value' in detail)) {
    return ''
  }
  return typeof detail.value === 'string' ? detail.value : ''
}

function fieldError(field: keyof LandDemandForm): string {
  return props.errors.find(error => error.field === field)?.message ?? ''
}

function changeText(field: keyof LandDemandForm, event: unknown): void {
  emit('change', { [field]: readStringDetail(event) })
}

function changeFinancing(event: unknown): void {
  emit('change', { is_financing: readStringDetail(event) as FinancingChoice })
}
</script>

<template>
  <view class="step-card">
    <text class="step-card__title">融资与联系人</text>
    <view class="field">
      <text class="field__label">是否有融资需求</text>
      <t-radio-group
        data-testid="is-financing"
        :value="props.form.is_financing"
        @change="changeFinancing"
      >
        <t-radio data-testid="is-financing-yes" value="有">有</t-radio>
        <t-radio value="没有">没有</t-radio>
      </t-radio-group>
      <text v-if="fieldError('is_financing')" class="field__error">{{ fieldError('is_financing') }}</text>
    </view>
    <view v-if="props.form.is_financing === '有'">
      <t-input
        data-testid="financing-money"
        label="融资金额（万元）"
        type="digit"
        :value="props.form.financing_money"
        :status="fieldError('financing_money') ? 'error' : 'default'"
        @change="changeText('financing_money', $event)"
      />
      <text
        v-if="fieldError('financing_money')"
        data-testid="financing-money-error"
        class="field__error"
      >
        {{ fieldError('financing_money') }}
      </text>
      <t-input
        data-testid="financing-time"
        label="融资时间（YYYY-MM）"
        :value="props.form.financing_time"
        :status="fieldError('financing_time') ? 'error' : 'default'"
        @change="changeText('financing_time', $event)"
      />
      <text
        v-if="fieldError('financing_time')"
        data-testid="financing-time-error"
        class="field__error"
      >
        {{ fieldError('financing_time') }}
      </text>
    </view>
    <t-input
      data-testid="contact"
      label="联系人"
      :value="props.form.contact"
      :status="fieldError('contact') ? 'error' : 'default'"
      :tips="fieldError('contact')"
      @change="changeText('contact', $event)"
    />
    <t-input
      data-testid="office"
      label="职务（选填）"
      :value="props.form.office"
      :status="fieldError('office') ? 'error' : 'default'"
      :tips="fieldError('office')"
      @change="changeText('office', $event)"
    />
    <t-input
      data-testid="phone"
      label="手机号码"
      type="number"
      :maxlength="11"
      :value="props.form.phone"
      :status="fieldError('phone') ? 'error' : 'default'"
      :tips="fieldError('phone')"
      @change="changeText('phone', $event)"
    />
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.step-card {
  padding: $space-4;
  background: $color-card;
  border-radius: $radius-md;
}

.step-card__title,
.field__label,
.field__error {
  display: block;
}

.step-card__title {
  margin-bottom: $space-3;
  font-size: 34rpx;
  font-weight: 700;
  color: $color-text;
}

.field {
  padding: $space-3 0;
  border-bottom: 1rpx solid $color-border;
}

.field__label {
  margin-bottom: $space-2;
  font-size: 28rpx;
  color: $color-text;
}

.field__error {
  margin-top: $space-1;
  font-size: 24rpx;
  color: $color-error;
}
</style>

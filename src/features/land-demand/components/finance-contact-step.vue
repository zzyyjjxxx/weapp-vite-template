<script setup lang="ts">
import type { FieldError, FinancingChoice, LandDemandForm } from '../models'

import { ref } from 'wevu'
import { readStringDetail } from '@/platform/event-detail'

const props = defineProps<{ form: LandDemandForm, errors: readonly FieldError[] }>()
const emit = defineEmits<{ change: [patch: Partial<LandDemandForm>] }>()

defineComponentJson({ component: true })

const emptyOptions = ref<string[]>([])

function fieldError(field: keyof LandDemandForm): string {
  return props.errors.find(error => error.field === field)?.message ?? ''
}

function changeText(field: keyof LandDemandForm, detail: unknown): void {
  emit('change', { [field]: readStringDetail(detail) })
}

function changeFinancing(detail: unknown): void {
  emit('change', { is_financing: readStringDetail(detail) as FinancingChoice })
}
</script>

<template>
  <view class="step-card">
    <text class="step-card__title">融资及联系人</text>
    <text class="step-card__description">融资需求默认选择“没有”；如选择“有”，请补充金额和期望时间。</text>
    <view class="field">
      <text class="field__label">是否有融资需求</text>
      <t-radio-group
        data-testid="is-financing"
        :value="props.form.is_financing"
        :options="emptyOptions"
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
        status="default"
        tips=""
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
        status="default"
        tips=""
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
      status="default"
      tips=""
      @change="changeText('contact', $event)"
    />
    <t-input
      data-testid="office"
      label="职务（选填）"
      :value="props.form.office"
      status="default"
      tips=""
      @change="changeText('office', $event)"
    />
    <t-input
      data-testid="phone"
      label="手机号码"
      type="number"
      :maxlength="11"
      :value="props.form.phone"
      status="default"
      tips=""
      @change="changeText('phone', $event)"
    />
  </view>
</template>

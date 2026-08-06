<script setup lang="ts">
import type { FieldError, FinancingChoice, LandDemandForm } from '../models'

import { ref } from 'wevu'
import SinglePicker from '@/components/ui/single-picker/index.vue'
import { readStringDetail } from '@/platform/event-detail'
import { useInvalidFieldScroll } from '../invalid-field-scroll'

const props = defineProps<{ form: LandDemandForm, errors: readonly FieldError[] }>()
const emit = defineEmits<{ change: [patch: Partial<LandDemandForm>] }>()

defineComponentJson({ component: true, styleIsolation: 'apply-shared' })

useInvalidFieldScroll(() => props.errors, {
  is_financing: 'is-financing-field',
  financing_money: 'financing-money-field',
  financing_time: 'financing-time-field',
  contact: 'contact-field',
  office: 'office-field',
  phone: 'phone-field',
}, 'finance-contact-step')

const financingOptions = ['有', '没有'] as const
const financingTimeVisible = ref(false)

function fieldError(field: keyof LandDemandForm): string {
  return props.errors.find(error => error.field === field)?.message ?? ''
}

function changeText(field: keyof LandDemandForm, detail: unknown): void {
  emit('change', { [field]: readStringDetail(detail) })
}

function changeFinancing(detail: unknown): void {
  emit('change', { is_financing: readStringDetail(detail) as FinancingChoice })
}

function openFinancingTime(): void {
  financingTimeVisible.value = true
}

function closeFinancingTime(): void {
  financingTimeVisible.value = false
}

function changeFinancingTime(detail: unknown): void {
  const value = readStringDetail(detail)
  if (value) {
    emit('change', { financing_time: value })
  }
  closeFinancingTime()
}
</script>

<template>
  <view class="step-card">
    <text class="step-card__title">融资及联系人</text>
    <text class="step-card__description">融资需求默认选择“没有”；如选择“有”，请补充金额和期望时间。</text>

    <view id="is-financing-field" data-testid="is-financing-field" class="field field--selector">
      <SinglePicker
        data-testid="is-financing"
        title="是否有融资需求"
        :value="props.form.is_financing || ''"
        :options="financingOptions"
        placeholder="请选择"
        required
        @change="changeFinancing"
      />
      <text v-if="fieldError('is_financing')" class="field__error">{{ fieldError('is_financing') }}</text>
    </view>

    <view v-if="props.form.is_financing === '有'" id="financing-money-field" data-testid="financing-money-field" class="field field--control">
      <view class="field__label"><text>融资金额（万元）</text><text class="field__required">*</text></view>
      <t-input
        data-testid="financing-money"
        label=""
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
    </view>
    <view v-if="props.form.is_financing === '有'" id="financing-time-field" data-testid="financing-time-field" class="field field--selector">
      <t-cell
        data-testid="financing-time"
        title="融资时间"
        :note="props.form.financing_time || '请选择年月'"
        t-class-center="field-selector__center"
        t-class-note="field-selector__note"
        arrow
        required
        @tap="openFinancingTime"
      />
      <t-date-time-picker
        data-testid="financing-time-picker"
        :visible="financingTimeVisible"
        :value="props.form.financing_time || ''"
        mode="month"
        format="YYYY-MM"
        start="2020-01-01"
        end="2040-12-31"
        title="选择融资时间"
        @change="changeFinancingTime"
        @cancel="closeFinancingTime"
        @close="closeFinancingTime"
      />
      <text
        v-if="fieldError('financing_time')"
        data-testid="financing-time-error"
        class="field__error"
      >
        {{ fieldError('financing_time') }}
      </text>
    </view>

    <view id="contact-field" data-testid="contact-field" class="field field--control">
      <view class="field__label"><text>联系人</text><text class="field__required">*</text></view>
      <t-input
        data-testid="contact"
        label=""
        :value="props.form.contact"
        status="default"
        tips=""
        @change="changeText('contact', $event)"
      />
      <text v-if="fieldError('contact')" class="field__error">{{ fieldError('contact') }}</text>
    </view>
    <view id="office-field" data-testid="office-field" class="field field--control">
      <view class="field__label"><text>职务（选填）</text></view>
      <t-input
        data-testid="office"
        label=""
        :value="props.form.office"
        status="default"
        tips=""
        @change="changeText('office', $event)"
      />
      <text v-if="fieldError('office')" class="field__error">{{ fieldError('office') }}</text>
    </view>
    <view id="phone-field" data-testid="phone-field" class="field field--control">
      <view class="field__label"><text>手机号码</text><text class="field__required">*</text></view>
      <t-input
        data-testid="phone"
        label=""
        type="number"
        :maxlength="11"
        :value="props.form.phone"
        status="default"
        tips=""
        @change="changeText('phone', $event)"
      />
      <text v-if="fieldError('phone')" class="field__error">{{ fieldError('phone') }}</text>
    </view>
  </view>
</template>

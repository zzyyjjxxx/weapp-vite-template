<script setup lang="ts">
import type { FieldError, LandDemandForm } from '../models'

import { readStringDetail } from '@/platform/event-detail'
import { useInvalidFieldScroll } from '../invalid-field-scroll'
import { normalizeFieldErrorMessage } from '../validation'

const props = defineProps<{ form: LandDemandForm, errors?: readonly FieldError[] | null, scrollRequest: number, active: boolean }>()
const emit = defineEmits<{ change: [patch: Partial<LandDemandForm>] }>()

defineComponentJson({ component: true, styleIsolation: 'apply-shared' })

useInvalidFieldScroll(() => props.errors ?? [], () => props.scrollRequest, {
  contact: 'contact-field',
  office: 'office-field',
  phone: 'phone-field',
}, 'finance-contact-step', () => props.active)

function fieldError(field: keyof LandDemandForm): string {
  return normalizeFieldErrorMessage(props.errors?.find(error => error.field === field)?.message ?? '')
}

function changeText(field: keyof LandDemandForm, detail: unknown): void {
  emit('change', { [field]: readStringDetail(detail) })
}
</script>

<template>
  <view class="step-card">
    <text class="step-card__title">联系人信息</text>
    <text class="step-card__description">请填写项目联系人及联系方式。</text>

    <view id="contact-field" data-testid="contact-field" class="field field--control">
      <view class="field__label"><text>联系人</text><text class="field__required">*</text></view>
      <text v-if="fieldError('contact')" class="field__error field__error--before-control">{{ fieldError('contact') }}</text>
      <t-input
        data-testid="contact"
        label=""
        :value="props.form.contact"
        status="default"
        tips=""
        @change="changeText('contact', $event)"
      />
    </view>
    <view id="office-field" data-testid="office-field" class="field field--control">
      <view class="field__label"><text>职务（选填）</text></view>
      <text v-if="fieldError('office')" class="field__error field__error--before-control">{{ fieldError('office') }}</text>
      <t-input
        data-testid="office"
        label=""
        :value="props.form.office"
        status="default"
        tips=""
        @change="changeText('office', $event)"
      />
    </view>
    <view id="phone-field" data-testid="phone-field" class="field field--control">
      <view class="field__label"><text>手机号码</text><text class="field__required">*</text></view>
      <text v-if="fieldError('phone')" class="field__error field__error--before-control">{{ fieldError('phone') }}</text>
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
    </view>
  </view>
</template>

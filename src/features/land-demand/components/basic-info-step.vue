<script setup lang="ts">
import type { FieldError, LandDemandForm } from '../models'

const props = defineProps<{ form: LandDemandForm, errors: readonly FieldError[] }>()

defineComponentJson({ component: true, styleIsolation: 'apply-shared' })

function fieldError(field: keyof LandDemandForm): string {
  return props.errors.find(error => error.field === field)?.message ?? ''
}
</script>

<template>
  <view class="step-card">
    <text class="step-card__title">企业基本信息</text>
    <text class="step-card__description">以下企业归属信息来自当前登录身份，仅供核对，不可修改。</text>

    <view class="field field--control">
      <text class="field__label"><text class="field__required">*</text>企业名称</text>
      <t-input
        data-testid="businessname"
        label=""
        :value="props.form.businessname || ''"
        status="default"
        tips=""
        readonly
      />
      <text v-if="fieldError('businessname')" class="field__error">{{ fieldError('businessname') }}</text>
    </view>
    <view class="field field--control">
      <text class="field__label"><text class="field__required">*</text>统一社会信用代码</text>
      <t-input
        data-testid="creditcode"
        label=""
        :value="props.form.creditcode || ''"
        status="default"
        tips=""
        readonly
      />
      <text v-if="fieldError('creditcode')" class="field__error">{{ fieldError('creditcode') }}</text>
    </view>
    <view class="field field--control">
      <text class="field__label"><text class="field__required">*</text>所在区（县、市）</text>
      <t-input
        data-testid="county"
        label=""
        :value="props.form.county || ''"
        status="default"
        tips=""
        readonly
      />
      <text v-if="fieldError('county')" class="field__error">{{ fieldError('county') }}</text>
    </view>
    <view class="field field--control">
      <text class="field__label"><text class="field__required">*</text>所在镇街</text>
      <t-input
        data-testid="region"
        label=""
        :value="props.form.region || ''"
        status="default"
        tips=""
        readonly
      />
      <text v-if="fieldError('region')" class="field__error">{{ fieldError('region') }}</text>
    </view>
  </view>
</template>

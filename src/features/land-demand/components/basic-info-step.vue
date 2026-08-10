<script setup lang="ts">
import type { FieldError, LandDemandForm } from '../models'

import { useInvalidFieldScroll } from '../invalid-field-scroll'

const props = defineProps<{ form: LandDemandForm, errors: readonly FieldError[] }>()

defineComponentJson({ component: true, styleIsolation: 'apply-shared' })

useInvalidFieldScroll(() => props.errors, {
  businessname: 'businessname-field',
  creditcode: 'creditcode-field',
  county: 'county-field',
  region: 'region-field',
}, 'basic-info-step')

function fieldError(field: keyof LandDemandForm): string {
  return props.errors.find(error => error.field === field)?.message ?? ''
}
</script>

<template>
  <view class="step-card">
    <text class="step-card__title">企业基本信息</text>
    <text class="step-card__description">以下企业归属信息来自当前登录身份，仅供核对，不可修改。</text>

    <view id="businessname-field" data-testid="businessname-field" class="field field--control">
      <view class="field__label"><text>企业名称</text><text class="field__required">*</text></view>
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
    <view id="creditcode-field" data-testid="creditcode-field" class="field field--control">
      <view class="field__label"><text>统一社会信用代码</text><text class="field__required">*</text></view>
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
    <view id="county-field" data-testid="county-field" class="field field--control">
      <view class="field__label"><text>所在区（县、市）</text><text class="field__required">*</text></view>
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
    <view id="region-field" data-testid="region-field" class="field field--control">
      <view class="field__label"><text>所在镇街</text><text class="field__required">*</text></view>
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

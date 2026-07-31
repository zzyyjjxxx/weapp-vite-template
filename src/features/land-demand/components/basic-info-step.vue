<script setup lang="ts">
import type { FieldError, LandDemandForm } from '../models'

const props = defineProps<{ form: LandDemandForm, errors: readonly FieldError[] }>()

defineComponentJson({ component: true })
</script>

<template>
  <view class="step-card">
    <text class="step-card__title">企业基本信息</text>
    <text class="step-card__description">以下企业归属信息来自当前登录身份，仅供核对，不可修改。</text>
    <t-input label="企业名称" :value="props.form.businessname || ''" status="default" tips="" readonly />
    <t-input label="统一社会信用代码" :value="props.form.creditcode || ''" status="default" tips="" readonly />
    <t-input label="所在区（县、市）" :value="props.form.county || ''" status="default" tips="" readonly />
    <t-input label="所在镇街" :value="props.form.region || ''" status="default" tips="" readonly />
  </view>
</template>

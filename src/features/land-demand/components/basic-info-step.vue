<script setup lang="ts">
import type { FieldError, LandDemandForm } from '../models'

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

function changeCounty(event: unknown): void {
  emit('change', { county: readStringDetail(event) })
}

function changeRegion(event: unknown): void {
  emit('change', { region: readStringDetail(event) })
}
</script>

<template>
  <view class="step-card">
    <text class="step-card__title">企业基本信息</text>
    <text class="step-card__description">企业名称和统一社会信用代码来自登录信息，不可修改。</text>
    <t-input label="企业名称" :value="props.form.businessname" readonly />
    <t-input label="统一社会信用代码" :value="props.form.creditcode" readonly />
    <t-input label="所在区（县、市）" :value="props.form.county" @change="changeCounty" />
    <t-input label="所在镇街" :value="props.form.region" @change="changeRegion" />
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
.step-card__description {
  display: block;
}

.step-card__title {
  font-size: 34rpx;
  font-weight: 700;
  color: $color-text;
}

.step-card__description {
  margin: $space-2 0 $space-3;
  font-size: 24rpx;
  line-height: 1.6;
  color: $color-text-secondary;
}
</style>

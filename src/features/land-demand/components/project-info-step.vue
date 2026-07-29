<script setup lang="ts">
import type { FieldError, LandDemandForm } from '../models'

import { getDirections, INDUSTRY_TRACK_DIRECTIONS } from '../dictionaries/industry-tracks'

const props = defineProps<{ form: LandDemandForm, errors: readonly FieldError[] }>()
const emit = defineEmits<{ change: [patch: Partial<LandDemandForm>] }>()

defineComponentJson({ component: true })

const trackOptions = Object.keys(INDUSTRY_TRACK_DIRECTIONS)

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
</script>

<template>
  <view class="step-card">
    <text class="step-card__title">项目信息</text>
    <t-input
      data-testid="investment"
      label="项目总投资（万元）"
      type="digit"
      :value="props.form.investment"
      :status="fieldError('investment') ? 'error' : 'default'"
      :tips="fieldError('investment')"
      @change="changeText('investment', $event)"
    />
    <t-input
      data-testid="project-hydm"
      label="国民经济行业代码"
      :value="props.form.project_hydm"
      :status="fieldError('project_hydm') ? 'error' : 'default'"
      :tips="fieldError('project_hydm')"
      @change="changeText('project_hydm', $event)"
    />
    <view class="field">
      <text class="field__label">重点产业赛道</text>
      <t-radio-group
        data-testid="keyindustry"
        :value="props.form.keyindustry"
        :options="trackOptions"
        @change="changeText('keyindustry', $event)"
      />
      <text v-if="fieldError('keyindustry')" class="field__error">{{ fieldError('keyindustry') }}</text>
    </view>
    <view class="field">
      <text class="field__label">细分方向</text>
      <t-radio-group
        data-testid="futureindustry"
        :value="props.form.futureindustry"
        :options="getDirections(props.form.keyindustry)"
        @change="changeText('futureindustry', $event)"
      />
      <text v-if="fieldError('futureindustry')" class="field__error">{{ fieldError('futureindustry') }}</text>
    </view>
    <t-input
      data-testid="pred-ys"
      label="预计年营收（万元）"
      type="digit"
      :value="props.form.pred_ys"
      :status="fieldError('pred_ys') ? 'error' : 'default'"
      :tips="fieldError('pred_ys')"
      @change="changeText('pred_ys', $event)"
    />
    <t-input
      data-testid="pred-tax"
      label="预计年税收（万元）"
      type="digit"
      :value="props.form.pred_tax"
      :status="fieldError('pred_tax') ? 'error' : 'default'"
      :tips="fieldError('pred_tax')"
      @change="changeText('pred_tax', $event)"
    />
    <t-input
      data-testid="pred-rdex"
      label="预计研发投入（万元）"
      type="digit"
      :value="props.form.pred_rdex"
      :status="fieldError('pred_rdex') ? 'error' : 'default'"
      :tips="fieldError('pred_rdex')"
      @change="changeText('pred_rdex', $event)"
    />
    <t-input
      data-testid="pred-unitenergy"
      label="预计单位能耗"
      type="digit"
      :value="props.form.pred_unitenergy"
      :status="fieldError('pred_unitenergy') ? 'error' : 'default'"
      :tips="fieldError('pred_unitenergy')"
      @change="changeText('pred_unitenergy', $event)"
    />
    <t-textarea
      data-testid="projectdata"
      label="项目建设内容"
      :value="props.form.projectdata"
      :status="fieldError('projectdata') ? 'error' : 'default'"
      :tips="fieldError('projectdata')"
      placeholder="请说明主要产品、建设规模和工艺"
      @change="changeText('projectdata', $event)"
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

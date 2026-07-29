<script setup lang="ts">
import type { FieldError, LandDemandForm, YesNo } from '../models'

import { readStringArrayDetail, readStringDetail } from '@/platform/event-detail'
import { LAND_TYPE_OPTIONS } from '../dictionaries/land-types'
import { PARK_OPTIONS } from '../dictionaries/parks'

const props = defineProps<{ form: LandDemandForm, errors: readonly FieldError[] }>()
const emit = defineEmits<{ change: [patch: Partial<LandDemandForm>] }>()

defineComponentJson({ component: true })

const yesNoOptions = ['是', '否']
const parkOptions = PARK_OPTIONS.map(option => ({ ...option }))
const landTypeOptions = [...LAND_TYPE_OPTIONS]

function fieldError(field: keyof LandDemandForm): string {
  return props.errors.find(error => error.field === field)?.message ?? ''
}

function selectedParkNames(): string {
  return props.form.deploy_park
    .map(value => parkOptions.find(option => option.value === value)?.label ?? value)
    .join('、')
}

function changeText(field: keyof LandDemandForm, detail: unknown): void {
  emit('change', { [field]: readStringDetail(detail) })
}

function changeDeployChoice(detail: unknown): void {
  emit('change', { is_deploy: readStringDetail(detail) as YesNo })
}

function changeDeployParks(detail: unknown): void {
  emit('change', { deploy_park: readStringArrayDetail(detail) })
}

function changeSpecialUse(detail: unknown): void {
  emit('change', { is_specialuse: readStringDetail(detail) as YesNo })
}
</script>

<template>
  <view class="step-card">
    <text class="step-card__title">用地信息</text>

    <t-input
      data-testid="area"
      label="用地面积（亩）"
      type="digit"
      :value="props.form.area"
      :status="fieldError('area') ? 'error' : 'default'"
      :tips="fieldError('area')"
      @change="changeText('area', $event)"
    />
    <t-input
      data-testid="building-area"
      label="建筑面积（平方米）"
      type="digit"
      :value="props.form.building_area"
      :status="fieldError('building_area') ? 'error' : 'default'"
      :tips="fieldError('building_area')"
      @change="changeText('building_area', $event)"
    />
    <view class="field">
      <text class="field__label">意向园区</text>
      <t-radio-group
        data-testid="expect-park"
        :value="props.form.expect_park"
        :options="parkOptions"
        @change="changeText('expect_park', $event)"
      />
      <text v-if="fieldError('expect_park')" class="field__error">{{ fieldError('expect_park') }}</text>
    </view>
    <t-input
      data-testid="expect-time"
      label="预计用地时间（YYYY-MM）"
      :value="props.form.expect_time"
      :status="fieldError('expect_time') ? 'error' : 'default'"
      :tips="fieldError('expect_time')"
      @change="changeText('expect_time', $event)"
    />
    <view class="field">
      <text class="field__label">是否接受跨区域调剂</text>
      <t-radio-group
        data-testid="is-deploy"
        :value="props.form.is_deploy"
        :options="yesNoOptions"
        @change="changeDeployChoice"
      />
      <text v-if="fieldError('is_deploy')" class="field__error">{{ fieldError('is_deploy') }}</text>
    </view>
    <view v-if="props.form.is_deploy === '是'" class="field">
      <text class="field__label">可调剂园区</text>
      <t-checkbox-group
        data-testid="deploy-park"
        :value="props.form.deploy_park"
        :options="parkOptions"
        @change="changeDeployParks"
      />
      <text data-testid="deploy-park-selection" class="field__selection">
        {{ selectedParkNames() }}
      </text>
      <text v-if="fieldError('deploy_park')" class="field__error">{{ fieldError('deploy_park') }}</text>
    </view>

    <t-input
      data-testid="deploy-height"
      label="层高要求（米，选填）"
      type="digit"
      :value="props.form.deploy_height"
      :status="fieldError('deploy_height') ? 'error' : 'default'"
      :tips="fieldError('deploy_height')"
      @change="changeText('deploy_height', $event)"
    />
    <t-input
      data-testid="deploy-weight"
      label="承重要求（吨/平方米，选填）"
      type="digit"
      :value="props.form.deploy_weight"
      :status="fieldError('deploy_weight') ? 'error' : 'default'"
      :tips="fieldError('deploy_weight')"
      @change="changeText('deploy_weight', $event)"
    />

    <view class="field">
      <text class="field__label">是否有特殊用地需求</text>
      <t-radio-group
        data-testid="is-specialuse"
        :value="props.form.is_specialuse"
        @change="changeSpecialUse"
      >
        <t-radio value="是">是</t-radio>
        <t-radio data-testid="is-specialuse-no" value="否">否</t-radio>
      </t-radio-group>
      <text v-if="fieldError('is_specialuse')" class="field__error">{{ fieldError('is_specialuse') }}</text>
    </view>
    <view v-if="props.form.is_specialuse === '是'" class="field">
      <text class="field__label">特殊用地类型</text>
      <t-radio-group
        data-testid="deploy-landtype"
        :value="props.form.deploy_landtype"
        :options="landTypeOptions"
        @change="changeText('deploy_landtype', $event)"
      />
      <text v-if="fieldError('deploy_landtype')" class="field__error">{{ fieldError('deploy_landtype') }}</text>
    </view>
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
.field__selection,
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

.field__selection {
  margin-top: $space-1;
  font-size: 24rpx;
  color: $color-text-secondary;
}
</style>

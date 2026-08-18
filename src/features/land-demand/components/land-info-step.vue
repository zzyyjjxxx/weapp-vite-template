<script setup lang="ts">
import type { FieldError, LandDemandForm, YesNo } from '../models'

import { ref } from 'wevu'
import SinglePicker from '@/components/ui/single-picker/index.vue'
import { readStringArrayDetail, readStringDetail } from '@/platform/event-detail'
import { LAND_TYPE_OPTIONS } from '../dictionaries/land-types'
import { EXPECT_PARK_OPTIONS, PARK_OPTIONS } from '../dictionaries/parks'
import { useInvalidFieldScroll } from '../invalid-field-scroll'
import { normalizeFieldErrorMessage } from '../validation'

const props = defineProps<{ form: LandDemandForm, errors?: readonly FieldError[] | null, scrollRequest: number, active: boolean }>()
const emit = defineEmits<{ change: [patch: Partial<LandDemandForm>] }>()

defineComponentJson({ component: true, styleIsolation: 'apply-shared' })

useInvalidFieldScroll(() => props.errors ?? [], () => props.scrollRequest, {
  area: 'area-field',
  building_area: 'building-area-field',
  expect_park: 'expect-park-field',
  expect_time: 'expect-time-field',
  is_deploy: 'is-deploy-field',
  deploy_park: 'deploy-park-field',
  deploy_height: 'deploy-height-field',
  deploy_weight: 'deploy-weight-field',
  is_specialuse: 'is-specialuse-field',
  deploy_landtype: 'deploy-landtype-field',
}, 'land-info-step', () => props.active)

const yesNoOptions = ['是', '否'] as const
const expectTimeVisible = ref(false)
const parkOptions = PARK_OPTIONS
const expectParkOptions = EXPECT_PARK_OPTIONS
const landTypeOptions = LAND_TYPE_OPTIONS

function fieldError(field: keyof LandDemandForm): string {
  return normalizeFieldErrorMessage(props.errors?.find(error => error.field === field)?.message ?? '')
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

function openExpectTime(): void {
  expectTimeVisible.value = true
}

function closeExpectTime(): void {
  expectTimeVisible.value = false
}

function changeExpectTime(detail: unknown): void {
  const value = readStringDetail(detail)
  if (value) {
    emit('change', { expect_time: value })
  }
  closeExpectTime()
}
</script>

<template>
  <view class="step-card">
    <text class="step-card__title">用地需求</text>
    <text class="step-card__description">请填写项目所需空间、时间与调剂意向，选填项可按实际情况补充。</text>

    <view id="area-field" data-testid="area-field" class="field field--control">
      <view class="field__label"><text>用地面积（亩）</text><text class="field__required">*</text></view>
      <text v-if="fieldError('area')" class="field__error field__error--before-control">{{ fieldError('area') }}</text>
      <t-input
        data-testid="area"
        label=""
        type="digit"
        :value="props.form.area"
        status="default"
        tips=""
        @change="changeText('area', $event)"
      />
    </view>
    <view id="building-area-field" data-testid="building-area-field" class="field field--control">
      <view class="field__label"><text>建筑面积（平方米）</text><text class="field__required">*</text></view>
      <text v-if="fieldError('building_area')" class="field__error field__error--before-control">{{ fieldError('building_area') }}</text>
      <t-input
        data-testid="building-area"
        label=""
        type="digit"
        :value="props.form.building_area"
        status="default"
        tips=""
        @change="changeText('building_area', $event)"
      />
    </view>

    <view id="expect-park-field" data-testid="expect-park-field" class="field field--selector">
      <SinglePicker
        data-testid="expect-park"
        title="意向园区"
        :value="props.form.expect_park || ''"
        :options="expectParkOptions"
        placeholder="请选择园区"
        required
        @change="changeText('expect_park', $event)"
      >
        <template #error>
          <text v-if="fieldError('expect_park')" class="field__error field__error--inside-cell">{{ fieldError('expect_park') }}</text>
        </template>
      </SinglePicker>
    </view>

    <view id="expect-time-field" data-testid="expect-time-field" class="field field--selector">
      <t-cell
        data-testid="expect-time"
        title="预计用地时间"
        :note="props.form.expect_time || '请选择年月'"
        t-class-center="field-selector__center"
        t-class-note="field-selector__note"
        arrow
        required
        @tap="openExpectTime"
      >
        <template #description>
          <text v-if="fieldError('expect_time')" class="field__error field__error--inside-cell">{{ fieldError('expect_time') }}</text>
        </template>
      </t-cell>
      <t-date-time-picker
        data-testid="expect-time-picker"
        :visible="expectTimeVisible"
        :value="props.form.expect_time || ''"
        mode="month"
        format="YYYY-MM"
        start="2020-01-01"
        end="2040-12-31"
        title="选择预计用地时间"
        @change="changeExpectTime"
        @cancel="closeExpectTime"
        @close="closeExpectTime"
      />
    </view>

    <view id="is-deploy-field" data-testid="is-deploy-field" class="field field--selector">
      <SinglePicker
        data-testid="is-deploy"
        title="是否接受跨区域调剂"
        :value="props.form.is_deploy || ''"
        :options="yesNoOptions"
        placeholder="请选择"
        required
        @change="changeDeployChoice"
      >
        <template #error>
          <text v-if="fieldError('is_deploy')" class="field__error field__error--inside-cell">{{ fieldError('is_deploy') }}</text>
        </template>
      </SinglePicker>
    </view>
    <view v-if="props.form.is_deploy === '是'" id="deploy-park-field" data-testid="deploy-park-field" class="field field--multi">
      <view class="field__label"><text>可调剂园区</text><text class="field__required">*</text></view>
      <text v-if="fieldError('deploy_park')" class="field__error field__error--before-control">{{ fieldError('deploy_park') }}</text>
      <text
        v-if="props.form.deploy_park.length > 0"
        data-testid="deploy-park-selection"
        class="field__selection"
      >
        {{ selectedParkNames() }}
      </text>
      <t-checkbox-group
        data-testid="deploy-park"
        :value="props.form.deploy_park"
        :options="parkOptions"
        @change="changeDeployParks"
      />
    </view>

    <view id="deploy-height-field" data-testid="deploy-height-field" class="field field--control">
      <view class="field__label"><text>层高要求（米，选填）</text></view>
      <text v-if="fieldError('deploy_height')" class="field__error field__error--before-control">{{ fieldError('deploy_height') }}</text>
      <t-input
        data-testid="deploy-height"
        label=""
        type="digit"
        :value="props.form.deploy_height"
        status="default"
        tips=""
        @change="changeText('deploy_height', $event)"
      />
    </view>
    <view id="deploy-weight-field" data-testid="deploy-weight-field" class="field field--control">
      <view class="field__label"><text>承重要求（吨/平方米，选填）</text></view>
      <text v-if="fieldError('deploy_weight')" class="field__error field__error--before-control">{{ fieldError('deploy_weight') }}</text>
      <t-input
        data-testid="deploy-weight"
        label=""
        type="digit"
        :value="props.form.deploy_weight"
        status="default"
        tips=""
        @change="changeText('deploy_weight', $event)"
      />
    </view>

    <view id="is-specialuse-field" data-testid="is-specialuse-field" class="field field--selector">
      <SinglePicker
        data-testid="is-specialuse"
        title="是否有特殊用地需求"
        :value="props.form.is_specialuse || ''"
        :options="yesNoOptions"
        placeholder="请选择"
        required
        @change="changeSpecialUse"
      >
        <template #error>
          <text v-if="fieldError('is_specialuse')" class="field__error field__error--inside-cell">{{ fieldError('is_specialuse') }}</text>
        </template>
      </SinglePicker>
    </view>
    <view v-if="props.form.is_specialuse === '是'" id="deploy-landtype-field" data-testid="deploy-landtype-field" class="field field--selector">
      <SinglePicker
        data-testid="deploy-landtype"
        title="特殊用地类型"
        :value="props.form.deploy_landtype || ''"
        :options="landTypeOptions"
        placeholder="请选择类型"
        required
        @change="changeText('deploy_landtype', $event)"
      >
        <template #error>
          <text v-if="fieldError('deploy_landtype')" class="field__error field__error--inside-cell">{{ fieldError('deploy_landtype') }}</text>
        </template>
      </SinglePicker>
    </view>
  </view>
</template>

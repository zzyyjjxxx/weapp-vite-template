<script setup lang="ts">
import type { FieldError, LandDemandForm, YesNo } from '../models'

import { ref } from 'wevu'
import SinglePicker from '@/components/ui/single-picker/index.vue'
import { readStringArrayDetail, readStringDetail } from '@/platform/event-detail'
import { useInvalidFieldScroll } from '../invalid-field-scroll'

const props = defineProps<{ form: LandDemandForm, errors: readonly FieldError[] }>()
const emit = defineEmits<{ change: [patch: Partial<LandDemandForm>] }>()

defineComponentJson({ component: true, styleIsolation: 'apply-shared' })

useInvalidFieldScroll(() => props.errors, {
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
}, 'land-info-step')

const yesNoOptions = ['是', '否'] as const
const expectTimeVisible = ref(false)
const parkOptions = [
  { value: '330200', label: '宁波市' },
  { value: '330203', label: '海曙区' },
  { value: '330205', label: '江北区' },
  { value: '330206', label: '北仑区' },
  { value: '330211', label: '镇海区' },
  { value: '330212', label: '鄞州区' },
  { value: '330213', label: '奉化区' },
  { value: '330225', label: '象山县' },
  { value: '330226', label: '宁海县' },
  { value: '330262', label: '高新区' },
  { value: '330281', label: '余姚市' },
  { value: '330282', label: '慈溪市' },
  { value: '3302821', label: '前湾新区' },
] as const
const landTypeOptions = ['小微园', '租售型闲置空间', '租售型标准厂房', '以上皆可'] as const

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
      <t-input
        data-testid="area"
        label=""
        type="digit"
        :value="props.form.area"
        status="default"
        tips=""
        @change="changeText('area', $event)"
      />
      <text v-if="fieldError('area')" class="field__error">{{ fieldError('area') }}</text>
    </view>
    <view id="building-area-field" data-testid="building-area-field" class="field field--control">
      <view class="field__label"><text>建筑面积（平方米）</text><text class="field__required">*</text></view>
      <t-input
        data-testid="building-area"
        label=""
        type="digit"
        :value="props.form.building_area"
        status="default"
        tips=""
        @change="changeText('building_area', $event)"
      />
      <text v-if="fieldError('building_area')" class="field__error">{{ fieldError('building_area') }}</text>
    </view>

    <view id="expect-park-field" data-testid="expect-park-field" class="field field--selector">
      <SinglePicker
        data-testid="expect-park"
        title="意向园区"
        :value="props.form.expect_park || ''"
        :options="parkOptions"
        placeholder="请选择园区"
        required
        @change="changeText('expect_park', $event)"
      />
      <text v-if="fieldError('expect_park')" class="field__error">{{ fieldError('expect_park') }}</text>
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
      />
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
      <text v-if="fieldError('expect_time')" class="field__error">{{ fieldError('expect_time') }}</text>
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
      />
      <text v-if="fieldError('is_deploy')" class="field__error">{{ fieldError('is_deploy') }}</text>
    </view>
    <view v-if="props.form.is_deploy === '是'" id="deploy-park-field" data-testid="deploy-park-field" class="field field--multi">
      <view class="field__label"><text>可调剂园区</text><text class="field__required">*</text></view>
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
      <text v-if="fieldError('deploy_park')" class="field__error">{{ fieldError('deploy_park') }}</text>
    </view>

    <view id="deploy-height-field" data-testid="deploy-height-field" class="field field--control">
      <view class="field__label"><text>层高要求（米，选填）</text></view>
      <t-input
        data-testid="deploy-height"
        label=""
        type="digit"
        :value="props.form.deploy_height"
        status="default"
        tips=""
        @change="changeText('deploy_height', $event)"
      />
      <text v-if="fieldError('deploy_height')" class="field__error">{{ fieldError('deploy_height') }}</text>
    </view>
    <view id="deploy-weight-field" data-testid="deploy-weight-field" class="field field--control">
      <view class="field__label"><text>承重要求（吨/平方米，选填）</text></view>
      <t-input
        data-testid="deploy-weight"
        label=""
        type="digit"
        :value="props.form.deploy_weight"
        status="default"
        tips=""
        @change="changeText('deploy_weight', $event)"
      />
      <text v-if="fieldError('deploy_weight')" class="field__error">{{ fieldError('deploy_weight') }}</text>
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
      />
      <text v-if="fieldError('is_specialuse')" class="field__error">{{ fieldError('is_specialuse') }}</text>
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
      />
      <text v-if="fieldError('deploy_landtype')" class="field__error">{{ fieldError('deploy_landtype') }}</text>
    </view>
  </view>
</template>

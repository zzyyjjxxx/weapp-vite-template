<script setup lang="ts">
import type { FieldError, LandDemandForm } from '../models'

import { computed, ref } from 'wevu'
import { readStringDetail } from '@/platform/event-detail'
import { getDirections, INDUSTRY_TRACK_DIRECTIONS } from '../dictionaries/industry-tracks'
import { getIndustryDisplay, NATIONAL_INDUSTRY_OPTIONS } from '../industry-selector'

const props = defineProps<{ form: LandDemandForm, errors: readonly FieldError[] }>()
const emit = defineEmits<{ change: [patch: Partial<LandDemandForm>] }>()

defineComponentJson({ component: true })

const nationalIndustryOptions = computed(() => NATIONAL_INDUSTRY_OPTIONS)
const trackOptions = computed(() => Object.keys(INDUSTRY_TRACK_DIRECTIONS))
const directionOptions = computed(() => getDirections(props.form?.keyindustry ?? ''))
const industryDisplay = computed(() => getIndustryDisplay(props.form?.project_hydm ?? ''))
const fieldErrors = computed<Partial<Record<keyof LandDemandForm, string>>>(() => (
  Object.fromEntries((props.errors ?? []).map(error => [error.field, error.message]))
))
const industrySelectorVisible = ref(false)

function changeText(field: keyof LandDemandForm, detail: unknown): void {
  emit('change', { [field]: readStringDetail(detail) })
}

function openIndustrySelector(): void {
  industrySelectorVisible.value = true
}

function closeIndustrySelector(): void {
  industrySelectorVisible.value = false
}

function changeIndustry(detail: unknown): void {
  emit('change', { project_hydm: readStringDetail(detail) })
  closeIndustrySelector()
}
</script>

<template>
  <view class="step-card">
    <text class="step-card__title">项目信息</text>
    <t-input
      data-testid="investment"
      label="固定资产投资额（万元）"
      type="digit"
      :value="props.form.investment"
      :status="fieldErrors.investment ? 'error' : 'default'"
      :tips="fieldErrors.investment || ''"
      @change="changeText('investment', $event)"
    />
    <view class="field field--selector">
      <view
        data-testid="project-hydm"
        class="field__selector"
        @tap="openIndustrySelector"
      >
        <view>
          <text class="field__selector-title">国民经济行业</text>
          <text class="field__selector-note">
            {{ industryDisplay || '请选择行业' }}
          </text>
        </view>
        <text class="field__selector-arrow">›</text>
      </view>
      <text v-if="fieldErrors.project_hydm" class="field__error">{{ fieldErrors.project_hydm }}</text>
      <t-cascader
        v-if="industrySelectorVisible && nationalIndustryOptions.length > 0"
        data-testid="project-hydm-cascader"
        :visible="industrySelectorVisible"
        :value="props.form.project_hydm"
        :options="nationalIndustryOptions"
        :filterable="true"
        title="选择国民经济行业"
        placeholder="请选择"
        @change="changeIndustry"
        @close="closeIndustrySelector"
      />
    </view>
    <view class="field">
      <text class="field__label">重点产业赛道</text>
      <t-radio-group
        data-testid="keyindustry"
        :value="props.form.keyindustry"
        @change="changeText('keyindustry', $event)"
      >
        <t-radio
          v-for="option in trackOptions"
          :key="option"
          :value="option"
          :label="option"
        />
      </t-radio-group>
      <text v-if="fieldErrors.keyindustry" class="field__error">{{ fieldErrors.keyindustry }}</text>
    </view>
    <view class="field">
      <text class="field__label">细分方向</text>
      <t-radio-group
        data-testid="futureindustry"
        :value="props.form.futureindustry"
        @change="changeText('futureindustry', $event)"
      >
        <t-radio
          v-for="option in directionOptions"
          :key="option"
          :value="option"
          :label="option"
        />
      </t-radio-group>
      <text v-if="fieldErrors.futureindustry" class="field__error">{{ fieldErrors.futureindustry }}</text>
    </view>
    <t-input
      data-testid="pred-ys"
      label="预计年营收（万元）"
      type="digit"
      :value="props.form.pred_ys"
      :status="fieldErrors.pred_ys ? 'error' : 'default'"
      :tips="fieldErrors.pred_ys || ''"
      @change="changeText('pred_ys', $event)"
    />
    <t-input
      data-testid="pred-tax"
      label="预计年税收（万元）"
      type="digit"
      :value="props.form.pred_tax"
      :status="fieldErrors.pred_tax ? 'error' : 'default'"
      :tips="fieldErrors.pred_tax || ''"
      @change="changeText('pred_tax', $event)"
    />
    <t-input
      data-testid="pred-rdex"
      label="预计研发投入（万元）"
      type="digit"
      :value="props.form.pred_rdex"
      :status="fieldErrors.pred_rdex ? 'error' : 'default'"
      :tips="fieldErrors.pred_rdex || ''"
      @change="changeText('pred_rdex', $event)"
    />
    <t-input
      data-testid="pred-unitenergy"
      label="项目单位能耗增加值（万元/吨标煤）"
      type="digit"
      :value="props.form.pred_unitenergy"
      :status="fieldErrors.pred_unitenergy ? 'error' : 'default'"
      :tips="fieldErrors.pred_unitenergy || ''"
      @change="changeText('pred_unitenergy', $event)"
    />
    <t-textarea
      data-testid="projectdata"
      label="项目建设内容"
      :value="props.form.projectdata"
      :status="fieldErrors.projectdata ? 'error' : 'default'"
      :tips="fieldErrors.projectdata || ''"
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

.field--selector {
  padding-top: 0;
}

.field__selector {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 96rpx;
}

.field__selector-title,
.field__selector-note {
  display: block;
}

.field__selector-title {
  font-size: 28rpx;
  color: $color-text;
}

.field__selector-note {
  margin-top: $space-1;
  font-size: 24rpx;
  color: $color-text-secondary;
}

.field__selector-arrow {
  font-size: 40rpx;
  color: $color-text-secondary;
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

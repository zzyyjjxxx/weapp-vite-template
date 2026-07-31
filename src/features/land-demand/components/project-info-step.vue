<script setup lang="ts">
import type { FieldError, LandDemandForm } from '../models'

import { ref } from 'wevu'
import { readStringDetail } from '@/platform/event-detail'
import { getDirections, INDUSTRY_TRACK_DIRECTIONS } from '../dictionaries/industry-tracks'
import { getIndustryDisplay, NATIONAL_INDUSTRY_OPTIONS } from '../industry-selector'

const props = defineProps<{ form: LandDemandForm, errors: readonly FieldError[] }>()
const emit = defineEmits<{ change: [patch: Partial<LandDemandForm>] }>()

defineComponentJson({ component: true })

const trackOptions = Object.keys(INDUSTRY_TRACK_DIRECTIONS)
const industrySelectorVisible = ref(false)

function fieldError(field: keyof LandDemandForm): string {
  return props.errors.find(error => error.field === field)?.message ?? ''
}

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
    <text class="step-card__title">投资项目</text>
    <text class="step-card__description">请按项目实际情况填写投资、行业、产出和建设内容等核心指标。</text>
    <t-input
      data-testid="investment"
      label="固定资产投资额（万元）"
      type="digit"
      :value="props.form.investment"
      :status="fieldError('investment') ? 'error' : 'default'"
      :tips="fieldError('investment')"
      @change="changeText('investment', $event)"
    />
    <view class="field field--selector">
      <t-cell
        data-testid="project-hydm"
        title="国民经济行业"
        :note="getIndustryDisplay(props.form.project_hydm) || '请选择行业'"
        arrow
        @tap="openIndustrySelector"
      />
      <text v-if="fieldError('project_hydm')" class="field__error">{{ fieldError('project_hydm') }}</text>
      <t-cascader
        data-testid="project-hydm-cascader"
        :visible="industrySelectorVisible"
        :value="props.form.project_hydm"
        :options="NATIONAL_INDUSTRY_OPTIONS"
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
      label="项目单位能耗增加值（万元/吨标煤）"
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
.field--selector {
  padding-top: 0;
}
</style>

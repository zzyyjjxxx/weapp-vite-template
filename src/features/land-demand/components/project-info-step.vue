<script setup lang="ts">
import type { FieldError, LandDemandForm } from '../models'

import { onMounted, ref, watch } from 'wevu'
import SinglePicker from '@/components/ui/single-picker/index.vue'
import { readStringDetail } from '@/platform/event-detail'
import { getDirections, INDUSTRY_TRACK_DIRECTIONS } from '../dictionaries/industry-tracks'
import { getIndustryDisplay, NATIONAL_INDUSTRY_OPTIONS } from '../industry-selector'
import { useInvalidFieldScroll } from '../invalid-field-scroll'

const props = defineProps<{ form: LandDemandForm, errors: readonly FieldError[] }>()
const emit = defineEmits<{ change: [patch: Partial<LandDemandForm>] }>()

defineComponentJson({ component: true, styleIsolation: 'apply-shared' })

useInvalidFieldScroll(() => props.errors, {
  investment: 'investment-field',
  project_hydm: 'project-hydm-field',
  keyindustry: 'keyindustry-field',
  futureindustry: 'futureindustry-field',
  pred_ys: 'pred-ys-field',
  pred_tax: 'pred-tax-field',
  pred_rdex: 'pred-rdex-field',
  pred_unitenergy: 'pred-unitenergy-field',
  projectdata: 'projectdata-field',
}, 'project-info-step')

const trackOptions = ref(Object.keys(INDUSTRY_TRACK_DIRECTIONS))
const industryOptions = ref([...NATIONAL_INDUSTRY_OPTIONS])
const industrySelectorVisible = ref(false)
const industryNote = ref('请选择行业')
const directionOptions = ref<string[]>([])
const optionsReady = ref(false)

watch(() => props.form?.project_hydm ?? '', (value) => {
  industryNote.value = getIndustryDisplay(value) || '请选择行业'
}, { immediate: true })
watch(() => props.form?.keyindustry ?? '', (value) => {
  directionOptions.value = [...getDirections(value)]
}, { immediate: true })
onMounted(() => {
  optionsReady.value = true
})

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

    <view id="investment-field" data-testid="investment-field" class="field field--control">
      <view class="field__label"><text>固定资产投资额（万元）</text><text class="field__required">*</text></view>
      <t-input
        data-testid="investment"
        label=""
        type="digit"
        :value="props.form.investment"
        status="default"
        tips=""
        @change="changeText('investment', $event)"
      />
      <text v-if="fieldError('investment')" class="field__error">{{ fieldError('investment') }}</text>
    </view>

    <view v-if="optionsReady" id="project-hydm-field" data-testid="project-hydm-field" class="field field--selector">
      <t-cell
        data-testid="project-hydm"
        title="国民经济行业"
        :note="industryNote"
        t-class-center="field-selector__center"
        t-class-note="field-selector__note"
        required
        arrow
        @tap="openIndustrySelector"
      />
      <text v-if="fieldError('project_hydm')" class="field__error">{{ fieldError('project_hydm') }}</text>
      <t-cascader
        data-testid="project-hydm-cascader"
        :visible="industrySelectorVisible"
        :value="props.form.project_hydm"
        :options="industryOptions"
        :filterable="true"
        filter-placeholder="搜索行业"
        title="选择国民经济行业"
        placeholder="请选择"
        @change="changeIndustry"
        @close="closeIndustrySelector"
      />
    </view>

    <view v-if="optionsReady" id="keyindustry-field" data-testid="keyindustry-field" class="field field--selector">
      <SinglePicker
        data-testid="keyindustry"
        title="重点产业赛道"
        :value="props.form.keyindustry || ''"
        :options="trackOptions"
        placeholder="请选择赛道"
        required
        @change="changeText('keyindustry', $event)"
      />
      <text v-if="fieldError('keyindustry')" class="field__error">{{ fieldError('keyindustry') }}</text>
    </view>
    <view v-if="optionsReady" id="futureindustry-field" data-testid="futureindustry-field" class="field field--selector">
      <SinglePicker
        data-testid="futureindustry"
        title="细分方向"
        :value="props.form.futureindustry || ''"
        :options="directionOptions"
        placeholder="请选择方向"
        required
        @change="changeText('futureindustry', $event)"
      />
      <text v-if="fieldError('futureindustry')" class="field__error">{{ fieldError('futureindustry') }}</text>
    </view>

    <view id="pred-ys-field" data-testid="pred-ys-field" class="field field--control">
      <view class="field__label"><text>预计年营业收入（万元）</text><text class="field__required">*</text></view>
      <t-input
        data-testid="pred-ys"
        label=""
        type="digit"
        :value="props.form.pred_ys"
        status="default"
        tips=""
        @change="changeText('pred_ys', $event)"
      />
      <text v-if="fieldError('pred_ys')" class="field__error">{{ fieldError('pred_ys') }}</text>
    </view>
    <view id="pred-tax-field" data-testid="pred-tax-field" class="field field--control">
      <view class="field__label"><text>预计年税收（万元）</text><text class="field__required">*</text></view>
      <t-input
        data-testid="pred-tax"
        label=""
        type="digit"
        :value="props.form.pred_tax"
        status="default"
        tips=""
        @change="changeText('pred_tax', $event)"
      />
      <text v-if="fieldError('pred_tax')" class="field__error">{{ fieldError('pred_tax') }}</text>
    </view>
    <view id="pred-rdex-field" data-testid="pred-rdex-field" class="field field--control">
      <view class="field__label"><text>预计研发投入（万元）</text><text class="field__required">*</text></view>
      <t-input
        data-testid="pred-rdex"
        label=""
        type="digit"
        :value="props.form.pred_rdex"
        status="default"
        tips=""
        @change="changeText('pred_rdex', $event)"
      />
      <text v-if="fieldError('pred_rdex')" class="field__error">{{ fieldError('pred_rdex') }}</text>
    </view>
    <view id="pred-unitenergy-field" data-testid="pred-unitenergy-field" class="field field--control">
      <view class="field__label"><text>项目单位能耗增加值（万元/吨标煤）</text><text class="field__required">*</text></view>
      <t-input
        data-testid="pred-unitenergy"
        label=""
        type="digit"
        :value="props.form.pred_unitenergy"
        status="default"
        tips=""
        @change="changeText('pred_unitenergy', $event)"
      />
      <text v-if="fieldError('pred_unitenergy')" class="field__error">{{ fieldError('pred_unitenergy') }}</text>
    </view>
    <view id="projectdata-field" data-testid="projectdata-field" class="field field--control">
      <view class="field__label"><text>项目建设内容</text><text class="field__required">*</text></view>
      <t-textarea
        data-testid="projectdata"
        label=""
        :value="props.form.projectdata"
        placeholder="请说明主要产品、建设规模和工艺"
        @change="changeText('projectdata', $event)"
      />
      <text v-if="fieldError('projectdata')" class="field__error">{{ fieldError('projectdata') }}</text>
    </view>
  </view>
</template>

<style lang="scss">
.field--selector {
  padding-top: 0;
}
</style>

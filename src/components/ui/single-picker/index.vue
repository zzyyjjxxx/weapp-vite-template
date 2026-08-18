<script setup lang="ts">
import { computed, ref } from 'wevu'
import { readPickerValueDetail } from '@/platform/event-detail'

export interface SinglePickerOption {
  label: string
  value: string
}

type SinglePickerOptionInput = string | SinglePickerOption

const props = withDefaults(defineProps<{
  title?: string
  value?: string
  options?: readonly SinglePickerOptionInput[]
  placeholder?: string
  required?: boolean
  error?: string | null
}>(), {
  title: '',
  value: '',
  options: () => [],
  placeholder: '请选择',
  required: false,
  error: '',
})
const emit = defineEmits<{
  change: [detail: { value: string }]
}>()

defineComponentJson({ component: true, styleIsolation: 'apply-shared' })

const visible = ref(false)
const pickerOptions = computed<SinglePickerOption[]>(() => (props.options ?? []).map(option => (
  typeof option === 'string'
    ? { label: option, value: option }
    : { label: option.label, value: option.value }
)))
const pickerValue = computed(() => props.value ? [props.value] : [])
const displayValue = computed(() => (
  pickerOptions.value.find(option => option.value === props.value)?.label
  ?? props.placeholder
  ?? ''
))
const errorText = computed(() => props.error ?? '')

function open(): void {
  visible.value = true
}

function close(): void {
  visible.value = false
}

function confirm(detail: unknown): void {
  const value = readPickerValueDetail(detail)
  if (value) {
    emit('change', { value })
  }
  close()
}
</script>

<template>
  <view class="single-picker">
    <t-cell
      :title="props.title || ''"
      :note="displayValue || ''"
      :required="props.required"
      t-class-center="field-selector__center"
      t-class-note="field-selector__note"
      arrow
      hover
      @tap="open"
    >
      <template #description>
        <text v-if="errorText" class="field__error field__error--inside-cell">{{ errorText }}</text>
        <slot name="error" />
      </template>
    </t-cell>
    <t-picker
      v-if="visible"
      :visible="visible"
      :value="pickerValue || []"
      :title="props.title || ''"
      cancel-btn="取消"
      confirm-btn="确定"
      @confirm="confirm"
      @cancel="close"
      @close="close"
    >
      <t-picker-item :options="pickerOptions || []" />
    </t-picker>
  </view>
</template>

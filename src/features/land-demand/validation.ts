import type { FieldError, LandDemandForm } from './models'

import { getDirections, INDUSTRY_TRACK_DIRECTIONS } from './dictionaries/industry-tracks'
import { LAND_TYPE_OPTIONS } from './dictionaries/land-types'
import { PARK_OPTIONS } from './dictionaries/parks'
import { NATIONAL_INDUSTRY_OPTIONS } from './industry-selector'

type Field = keyof LandDemandForm
type LandDemandStep = 1 | 2 | 3 | 4 | 5

const STEPS: Readonly<Record<Field, 1 | 2 | 3 | 4>> = {
  county: 1,
  region: 1,
  businessname: 1,
  creditcode: 1,
  area: 2,
  building_area: 2,
  expect_park: 2,
  expect_time: 2,
  is_deploy: 2,
  deploy_park: 2,
  is_specialuse: 2,
  deploy_landtype: 2,
  deploy_height: 2,
  deploy_weight: 2,
  investment: 3,
  project_hydm: 3,
  keyindustry: 3,
  futureindustry: 3,
  pred_ys: 3,
  pred_tax: 3,
  pred_rdex: 3,
  pred_unitenergy: 3,
  projectdata: 3,
  contact: 4,
  office: 4,
  phone: 4,
}

const PROGRESS_FIELDS: Readonly<Record<1 | 2 | 3 | 4, readonly Field[]>> = {
  1: ['county', 'region', 'businessname', 'creditcode'],
  2: [
    'area',
    'building_area',
    'expect_park',
    'expect_time',
    'is_deploy',
    'deploy_park',
    'is_specialuse',
    'deploy_landtype',
    'deploy_height',
    'deploy_weight',
  ],
  3: [
    'investment',
    'project_hydm',
    'keyindustry',
    'futureindustry',
    'pred_ys',
    'pred_tax',
    'pred_rdex',
    'pred_unitenergy',
    'projectdata',
  ],
  // Contact and phone are prefilled from the authenticated enterprise
  // profile, so they do not prove that step 4 has been reached. The saved
  // step metadata (or a complete form) remains authoritative for that step.
  4: [],
}

const NUMERIC_FIELDS = new Set<Field>([
  'area',
  'building_area',
  'deploy_height',
  'deploy_weight',
  'investment',
  'pred_ys',
  'pred_tax',
  'pred_rdex',
  'pred_unitenergy',
])
const DATE_FIELDS = new Set<Field>(['expect_time'])
const DECIMAL_20_6_FIELDS = new Set<Field>([
  'investment',
  'pred_ys',
  'pred_tax',
  'pred_rdex',
  'pred_unitenergy',
])
const DECIMAL_10_2_FIELDS = new Set<Field>([
  'building_area',
  'deploy_height',
  'deploy_weight',
])
const YES_NO_VALUES = new Set(['是', '否'])
const PARK_VALUES = new Set<string>(PARK_OPTIONS.map(option => option.value))
const LAND_TYPE_VALUES = new Set<string>(LAND_TYPE_OPTIONS)
const INDUSTRY_TRACK_VALUES = new Set(Object.keys(INDUSTRY_TRACK_DIRECTIONS))
const NATIONAL_INDUSTRY_VALUES = new Set(
  NATIONAL_INDUSTRY_OPTIONS.flatMap(group => group.children.map(option => option.value)),
)
const MIN_EXPECT_TIME = '2020-01'
const MAX_EXPECT_TIME = '2040-12'
const CITY_PARK = '330200'

function hasValue(value: string | readonly string[] | null | undefined): boolean {
  return typeof value === 'string'
    ? value.trim().length > 0
    : Array.isArray(value) && value.length > 0
}

function error(field: Field, message: string): FieldError {
  return { field, step: STEPS[field], message }
}

export function normalizeFieldErrorMessage(message: string): string {
  return message.trim().toLowerCase() === 'error' ? '此项必填' : message
}

function isValidNumber(value: string, maxIntegerDigits: number, maxDecimals: number): boolean {
  if (!/^\d+(?:\.\d+)?$/.test(value)) {
    return false
  }

  const [integer, decimal = ''] = value.split('.')
  const significantInteger = integer.replace(/^0+(?=\d)/, '')
  return significantInteger.length <= maxIntegerDigits && decimal.length <= maxDecimals
}

function validateChoiceValue(
  field: Field,
  value: string | readonly string[],
  form: LandDemandForm,
): FieldError | undefined {
  if (field === 'deploy_park') {
    if (!Array.isArray(value)) {
      return error(field, '请选择有效的调剂园区')
    }

    if (value.some(park => !PARK_VALUES.has(park))) {
      return error(field, '请选择有效的调剂园区')
    }

    if (value.includes(CITY_PARK) && value.length > 1) {
      return error(field, '宁波市不能与其他区域同时选择')
    }

    return undefined
  }

  if (typeof value !== 'string') {
    return undefined
  }

  switch (field) {
    case 'expect_park':
      return PARK_VALUES.has(value) ? undefined : error(field, '请选择有效的意向园区')
    case 'expect_time':
      return value >= MIN_EXPECT_TIME && value <= MAX_EXPECT_TIME
        ? undefined
        : error(field, '请选择有效的预计用地时间')
    case 'is_deploy':
    case 'is_specialuse':
      return YES_NO_VALUES.has(value) ? undefined : error(field, '请选择“是”或“否”')
    case 'deploy_landtype':
      return LAND_TYPE_VALUES.has(value) ? undefined : error(field, '请选择有效的特殊用地类型')
    case 'project_hydm':
      return NATIONAL_INDUSTRY_VALUES.has(value) ? undefined : error(field, '请选择有效的国民经济行业')
    case 'keyindustry':
      return INDUSTRY_TRACK_VALUES.has(value) ? undefined : error(field, '请选择有效的重点产业赛道')
    case 'futureindustry':
      return getDirections(form.keyindustry).includes(value)
        ? undefined
        : error(field, '请选择该产业赛道下的有效发展方向')
    default:
      return undefined
  }
}

function validateValue(
  field: Field,
  value: string | readonly string[],
  form: LandDemandForm,
): FieldError | undefined {
  // Office is an optional free-form title. Keep it outside numeric validation
  // even if the numeric field list changes in a future form revision.
  if (field === 'office') {
    return undefined
  }

  const maxIntegerDigits = DECIMAL_20_6_FIELDS.has(field)
    ? 14
    : DECIMAL_10_2_FIELDS.has(field)
      ? 8
      : 18
  if (NUMERIC_FIELDS.has(field) && typeof value === 'string' && !isValidNumber(value, maxIntegerDigits, 2)) {
    return error(field, '请输入非负数字，最多两位小数')
  }

  if (DATE_FIELDS.has(field) && typeof value === 'string' && !/^\d{4}-(?:0[1-9]|1[0-2])$/.test(value)) {
    return error(field, '请输入正确的年月')
  }

  if (field === 'phone' && typeof value === 'string' && !/^1[3-9]\d{9}$/.test(value)) {
    return error(field, '请输入正确的手机号码')
  }

  return validateChoiceValue(field, value, form)
}

function collectFormatErrors(form: LandDemandForm): FieldError[] {
  const fields = Object.keys(STEPS) as Field[]

  return fields.flatMap((field) => {
    const value = form[field]
    if (!hasValue(value)) {
      return []
    }

    // These values are not applicable when their controlling choice hides the
    // field. Older records may still contain a stale value and should not be
    // marked invalid for a control the user cannot currently edit.
    if (field === 'deploy_park' && form.is_deploy !== '是') {
      return []
    }
    if (field === 'deploy_landtype' && form.is_specialuse !== '是') {
      return []
    }

    const fieldError = validateValue(field, value, form)
    return fieldError ? [fieldError] : []
  })
}

export function validateDraft(form: LandDemandForm): FieldError[] {
  return collectFormatErrors(form)
}

export function validateSubmission(form: LandDemandForm): FieldError[] {
  const errors = collectFormatErrors(form)
  const required: Field[] = [
    'county',
    'region',
    'businessname',
    'creditcode',
    'area',
    'building_area',
    'expect_park',
    'expect_time',
    'is_deploy',
    ...(form.is_deploy === '是' ? ['deploy_park' as const] : []),
    'is_specialuse',
    ...(form.is_specialuse === '是' ? ['deploy_landtype' as const] : []),
    'investment',
    'project_hydm',
    'keyindustry',
    'futureindustry',
    'pred_ys',
    'pred_tax',
    'pred_rdex',
    'pred_unitenergy',
    'projectdata',
    'contact',
    'phone',
  ]

  for (const field of required) {
    if (!hasValue(form[field]) && !errors.some(item => item.field === field)) {
      errors.push(error(field, '此项必填'))
    }
  }

  return errors
}

/**
 * Validate only the step the user is leaving.
 *
 * Submission validation intentionally remains the single source of truth for
 * required and conditional fields.  Filtering its result here keeps the
 * wizard and the final submit action in sync without maintaining a second
 * required-field list in the page.
 */
export function validateStep(
  form: LandDemandForm,
  step: 1 | 2 | 3 | 4 | 5,
): FieldError[] {
  return validateSubmission(form).filter(error => error.step === step)
}

/**
 * Resolve the furthest step represented by the current form values.
 *
 * Local draft metadata can be stale after a server save or an older build.
 * Values entered in a later step still mean that step has been reached, even
 * when the form is not complete yet. A fully valid form reaches the review
 * step (5), which has no editable fields of its own.
 */
export function resolveProgressStep(
  form: LandDemandForm,
  fallback: LandDemandStep = 1,
): LandDemandStep {
  if (validateSubmission(form).length === 0) {
    return 5
  }

  let progressStep = fallback
  for (const step of [1, 2, 3, 4] as const) {
    if (PROGRESS_FIELDS[step].some(field => hasValue(form[field]))) {
      progressStep = Math.max(progressStep, step) as LandDemandStep
    }
  }

  return progressStep
}

import type { FieldError, LandDemandForm } from './models'

type Field = keyof LandDemandForm

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
  is_financing: 4,
  financing_money: 4,
  financing_time: 4,
  contact: 4,
  office: 4,
  phone: 4,
}

const NUMERIC_FIELDS = new Set<Field>([
  'area',
  'building_area',
  'deploy_height',
  'deploy_weight',
  'investment',
  'financing_money',
  'pred_ys',
  'pred_tax',
  'pred_rdex',
  'pred_unitenergy',
])
const DATE_FIELDS = new Set<Field>(['expect_time', 'financing_time'])
const DECIMAL_20_6_FIELDS = new Set<Field>([
  'investment',
  'financing_money',
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

function hasValue(value: string | readonly string[]): boolean {
  return typeof value === 'string' ? value.trim().length > 0 : value.length > 0
}

function error(field: Field, message: string): FieldError {
  return { field, step: STEPS[field], message }
}

function isValidNumber(value: string, maxIntegerDigits: number, maxDecimals: number): boolean {
  if (!/^\d+(?:\.\d+)?$/.test(value)) {
    return false
  }

  const [integer, decimal = ''] = value.split('.')
  const significantInteger = integer.replace(/^0+(?=\d)/, '')
  return significantInteger.length <= maxIntegerDigits && decimal.length <= maxDecimals
}

function validateValue(field: Field, value: string | readonly string[]): FieldError | undefined {
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

  return undefined
}

function collectFormatErrors(form: LandDemandForm): FieldError[] {
  const fields = Object.keys(STEPS) as Field[]

  return fields.flatMap((field) => {
    const value = form[field]
    if (!hasValue(value)) {
      return []
    }

    const fieldError = validateValue(field, value)
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
    'is_specialuse',
    'investment',
    'project_hydm',
    'keyindustry',
    'futureindustry',
    'pred_ys',
    'pred_tax',
    'pred_rdex',
    'pred_unitenergy',
    'projectdata',
    'is_financing',
    'contact',
    'phone',
  ]

  if (form.is_deploy === '是') {
    required.push('deploy_park')
  }
  if (form.is_specialuse === '是') {
    required.push('deploy_landtype')
  }
  if (form.is_financing === '有') {
    required.push('financing_money', 'financing_time')
  }

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

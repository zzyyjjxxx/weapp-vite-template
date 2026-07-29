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

const TWO_DECIMAL_FIELDS = new Set<Field>(['building_area', 'deploy_height', 'deploy_weight'])
const SIX_DECIMAL_FIELDS = new Set<Field>([
  'area',
  'investment',
  'financing_money',
  'pred_ys',
  'pred_tax',
  'pred_rdex',
  'pred_unitenergy',
])
const DATE_FIELDS = new Set<Field>(['expect_time', 'financing_time'])

function hasValue(value: string | readonly string[]): boolean {
  return typeof value === 'string' ? value.trim().length > 0 : value.length > 0
}

function error(field: Field, message: string): FieldError {
  return { field, step: STEPS[field], message }
}

function isValidNumber(value: string, maxDecimals: number): boolean {
  if (!/^\d+(?:\.\d+)?$/.test(value)) {
    return false
  }

  const [integer, decimal = ''] = value.split('.')
  return integer.length + decimal.length <= 20 && decimal.length <= maxDecimals
}

function validateValue(field: Field, value: string | readonly string[]): FieldError | undefined {
  if (TWO_DECIMAL_FIELDS.has(field) && typeof value === 'string' && !isValidNumber(value, 2)) {
    return error(field, '请输入非负数字，最多两位小数')
  }

  if (SIX_DECIMAL_FIELDS.has(field) && typeof value === 'string' && !isValidNumber(value, 6)) {
    return error(field, '请输入非负数字')
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

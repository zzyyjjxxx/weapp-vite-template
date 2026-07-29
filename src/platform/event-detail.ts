interface DetailValue {
  value?: unknown
}

interface CheckedDetail extends DetailValue {
  checked?: unknown
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

export function readStringDetail(detail: unknown): string {
  if (!isObject(detail)) {
    return ''
  }
  const value = (detail as DetailValue).value
  return typeof value === 'string' ? value : ''
}

export function readStringArrayDetail(detail: unknown): string[] {
  if (!isObject(detail)) {
    return []
  }
  const value = (detail as DetailValue).value
  return Array.isArray(value)
    ? value.filter((item): item is string => typeof item === 'string')
    : []
}

export function readBooleanDetail(detail: unknown): boolean {
  if (!isObject(detail)) {
    return typeof detail === 'boolean' ? detail : false
  }
  const checkedDetail = detail as CheckedDetail
  if ('checked' in checkedDetail) {
    return Boolean(checkedDetail.checked)
  }
  return Boolean(checkedDetail.value)
}

export function readPatchDetail<T extends object>(detail: unknown): Partial<T> {
  return isObject(detail) ? detail as Partial<T> : {}
}

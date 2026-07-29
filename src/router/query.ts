export type QueryValue = string | number | boolean | null | undefined
export type RouteQuery = Record<string, QueryValue | QueryValue[]>

const DEFAULT_RETURN_TO = '/pages/home/index'

export type LandDemandMode = 'edit' | 'view'

export function parseLandDemandMode(value: unknown): LandDemandMode {
  return value === 'view' ? 'view' : 'edit'
}

export function encodeQuery(query?: RouteQuery): string {
  if (!query) {
    return ''
  }

  const pairs: string[] = []
  for (const [key, rawValue] of Object.entries(query)) {
    const values = Array.isArray(rawValue) ? rawValue : [rawValue]
    for (const value of values) {
      if (value === undefined || value === null) {
        continue
      }
      pairs.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
    }
  }

  return pairs.length > 0 ? `?${pairs.join('&')}` : ''
}

export function parseRequiredString(value: unknown, name: string): string {
  if (typeof value !== 'string' || value.trim() === '') {
    throw new Error(`缺少有效的 ${name}`)
  }
  return value.trim()
}

export function parseReturnTo(value: unknown): string {
  if (typeof value !== 'string' || value.trim() === '') {
    return DEFAULT_RETURN_TO
  }

  let decoded = value.trim()
  try {
    decoded = decodeURIComponent(decoded)
  }
  catch {
    return DEFAULT_RETURN_TO
  }

  if (
    !decoded.startsWith('/')
    || decoded.startsWith('//')
    || decoded === '/pages/login/index'
    || decoded.startsWith('/pages/login/index?')
  ) {
    return DEFAULT_RETURN_TO
  }

  return decoded
}

export function parseOptionalNumber(value: unknown, name: string): number | undefined {
  if (value === undefined || value === null) {
    return undefined
  }

  const parsed = typeof value === 'number' ? value : Number(value)
  if (typeof value === 'boolean' || value === '' || !Number.isFinite(parsed)) {
    throw new Error(`${name} 必须是有效数字`)
  }
  return parsed
}

export function parseEnum<const T extends string>(
  value: unknown,
  allowed: readonly T[],
  name: string,
): T {
  if (typeof value === 'string' && allowed.includes(value as T)) {
    return value as T
  }
  throw new Error(`${name} 不是有效值`)
}

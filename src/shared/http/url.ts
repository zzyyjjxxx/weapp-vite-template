import type { RequestOptions } from './types'

export function buildUrl(
  baseUrl: string,
  path: string,
  query?: RequestOptions['query'],
): string {
  const normalizedBase = baseUrl.replace(/\/+$/, '')
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  const pairs: string[] = []

  for (const [key, rawValue] of Object.entries(query ?? {})) {
    const values = Array.isArray(rawValue) ? rawValue : [rawValue]

    for (const value of values) {
      if (value === undefined || value === null) {
        continue
      }

      pairs.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
    }
  }

  const suffix = pairs.length > 0 ? `?${pairs.join('&')}` : ''
  return normalizedBase + normalizedPath + suffix
}

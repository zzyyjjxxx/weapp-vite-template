import { describe, expect, it } from 'vitest'
import { formatDateTime } from '@/platform/date-time'

describe('formatDateTime', () => {
  it('formats an ISO timestamp to local date and time without milliseconds or timezone', () => {
    const localDate = new Date(2026, 7, 4, 7, 1, 30, 279)

    expect(formatDateTime(localDate.toISOString())).toBe('2026-08-04 07:01:30')
  })

  it('returns a placeholder for missing or invalid timestamps', () => {
    expect(formatDateTime()).toBe('--')
    expect(formatDateTime('not-a-date')).toBe('--')
  })
})

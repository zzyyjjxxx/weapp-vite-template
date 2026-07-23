import { describe, expect, it, vi } from 'vitest'

import { mergeSignalWithTimeout } from '@/shared/http/abort'

describe('mergeSignalWithTimeout', () => {
  it('propagates external cancellation without marking a timeout', () => {
    const external = new AbortController()
    const merged = mergeSignalWithTimeout(external.signal, 10_000)

    external.abort()

    expect(merged.signal.aborted).toBe(true)
    expect(merged.didTimeout()).toBe(false)
    merged.cleanup()
  })

  it('marks timeout cancellation and clears its timer', () => {
    vi.useFakeTimers()
    const merged = mergeSignalWithTimeout(undefined, 100)

    vi.advanceTimersByTime(100)

    expect(merged.signal.aborted).toBe(true)
    expect(merged.didTimeout()).toBe(true)
    merged.cleanup()
    vi.useRealTimers()
  })
})

import { describe, expect, it, vi } from 'vitest'

import { mergeSignalWithTimeout } from '@/shared/http/abort'

describe('mergeSignalWithTimeout', () => {
  it('uses a runtime-safe controller when the host has no AbortController', () => {
    vi.stubGlobal('AbortController', undefined)
    try {
      const merged = mergeSignalWithTimeout(undefined, 10_000)

      expect(merged.signal.aborted).toBe(false)
      merged.cleanup()
    }
    finally {
      vi.unstubAllGlobals()
    }
  })

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

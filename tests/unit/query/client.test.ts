import { afterEach, describe, expect, it } from 'vitest'

import { ApiError } from '@/shared/http/errors'
import { createQueryClient } from '@/shared/query/client'

describe('query client', () => {
  let client: ReturnType<typeof createQueryClient> | undefined

  afterEach(() => {
    client?.clear()
    client?.unmount()
  })

  it('uses the project query and mutation defaults', () => {
    client = createQueryClient()
    const defaults = client.getDefaultOptions()
    const retry = defaults.queries?.retry

    expect(defaults.queries?.staleTime).toBe(30_000)
    expect(defaults.queries?.gcTime).toBe(300_000)
    expect(defaults.queries?.refetchOnWindowFocus).toBe(false)
    expect(defaults.queries?.refetchOnReconnect).toBe(true)
    expect(defaults.mutations?.retry).toBe(0)
    expect(typeof retry).toBe('function')

    if (typeof retry === 'function') {
      expect(retry(0, new ApiError({
        kind: 'network',
        message: 'temporary',
        retryable: true,
      }))).toBe(true)
      expect(retry(2, new ApiError({
        kind: 'network',
        message: 'temporary',
        retryable: true,
      }))).toBe(false)
      expect(retry(0, new ApiError({
        kind: 'business',
        message: 'invalid',
      }))).toBe(false)
    }
  })
})

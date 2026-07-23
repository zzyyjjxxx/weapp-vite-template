import { describe, expect, it } from 'vitest'

import { ApiError } from '@/shared/http/errors'

describe('ApiError', () => {
  it('keeps protocol metadata and defaults retryable to false', () => {
    const cause = new Error('network')
    const error = new ApiError({
      kind: 'network',
      message: '网络连接失败',
      status: 503,
      code: 'TEMPORARY',
      traceId: 'trace-1',
      cause,
    })

    expect(error.name).toBe('ApiError')
    expect(error.kind).toBe('network')
    expect(error.status).toBe(503)
    expect(error.code).toBe('TEMPORARY')
    expect(error.traceId).toBe('trace-1')
    expect(error.retryable).toBe(false)
    expect(error.cause).toBe(cause)
  })
})

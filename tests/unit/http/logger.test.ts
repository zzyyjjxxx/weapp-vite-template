import { describe, expect, it } from 'vitest'

import { ApiError } from '@/shared/http/errors'
import { sanitizeError } from '@/shared/logger'

describe('sanitizeError', () => {
  it('keeps safe ApiError fields and drops the cause', () => {
    const sanitized = sanitizeError(new ApiError({
      kind: 'http',
      message: 'failed',
      status: 500,
      code: 'SERVER_ERROR',
      traceId: 'trace-1',
      retryable: true,
      cause: { authorization: 'secret' },
    }))

    expect(sanitized).toEqual({
      name: 'ApiError',
      message: 'failed',
      kind: 'http',
      status: 500,
      code: 'SERVER_ERROR',
      traceId: 'trace-1',
      retryable: true,
    })
    expect(sanitized).not.toHaveProperty('cause')
  })
})

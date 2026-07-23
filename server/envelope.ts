import type { ApiEnvelope } from './types'

import { randomUUID } from 'node:crypto'

export function createTraceId(): string {
  return randomUUID()
}

export function success<T>(
  data: T,
  message = 'success',
  traceId = createTraceId(),
): ApiEnvelope<T> {
  return {
    code: 'SUCCESS',
    message,
    data,
    traceId,
  }
}

export function failure(
  code: string | number,
  message: string,
  traceId = createTraceId(),
): ApiEnvelope<null> {
  return {
    code,
    message,
    data: null,
    traceId,
  }
}

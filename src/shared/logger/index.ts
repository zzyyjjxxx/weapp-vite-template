import { ApiError } from '@/shared/http/errors'

export interface LogContext {
  route?: string
  traceId?: string
  action?: string
  durationMs?: number
  status?: number
  errorKind?: string
  errorCode?: string | number
  queryHash?: string
  mutationId?: string
}

export type SanitizedError = Record<string, string | number | boolean | undefined>

export function sanitizeError(error: unknown): SanitizedError {
  if (error instanceof ApiError) {
    return {
      name: error.name,
      message: error.message,
      kind: error.kind,
      status: error.status,
      code: error.code,
      traceId: error.traceId,
      retryable: error.retryable,
    }
  }

  if (error instanceof Error) {
    return {
      name: error.name,
      message: error.message,
    }
  }

  return {
    name: 'UnknownError',
    message: typeof error === 'string' ? error : '未知错误',
  }
}

type LogLevel = 'debug' | 'info' | 'warn' | 'error'

function writeLog(
  level: LogLevel,
  event: string,
  context: LogContext,
  error?: unknown,
): void {
  const payload: Record<string, unknown> = {
    ...context,
  }
  if (error !== undefined) {
    payload.error = sanitizeError(error)
  }

  globalThis.console?.[level]?.(`[${event}]`, payload)
}

export const logger = {
  debug: (event: string, context: LogContext = {}): void => {
    writeLog('debug', event, context)
  },
  info: (event: string, context: LogContext = {}): void => {
    writeLog('info', event, context)
  },
  warn: (event: string, context: LogContext = {}, error?: unknown): void => {
    writeLog('warn', event, context, error)
  },
  error: (event: string, context: LogContext = {}, error?: unknown): void => {
    writeLog('error', event, context, error)
  },
}

export const debug = logger.debug
export const info = logger.info
export const warn = logger.warn
export const error = logger.error

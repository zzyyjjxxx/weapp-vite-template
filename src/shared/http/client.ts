import type { RequestAuthMode, RequestOptions } from './types'
import { logger, sanitizeError } from '@/shared/logger'
import { ApiError } from './errors'
import { getAuthSessionStore } from './session'
import { refreshAccessTokenSingleFlight } from './token-refresh'
import { transportRequest } from './transport'

function authMode(options: RequestOptions): RequestAuthMode {
  return options.auth ?? 'required'
}

function missingAccessTokenError(): ApiError {
  return new ApiError({
    kind: 'unauthorized',
    message: '缺少访问凭据',
    code: 'MISSING_ACCESS_TOKEN',
    status: 401,
  })
}

function logRequestError<TBody>(options: RequestOptions<TBody>, error: unknown): void {
  const sanitized = sanitizeError(error)
  logger.error('http.request.failed', {
    route: options.path,
    status: typeof sanitized.status === 'number' ? sanitized.status : undefined,
    errorKind: typeof sanitized.kind === 'string' ? sanitized.kind : undefined,
    errorCode: typeof sanitized.code === 'string' || typeof sanitized.code === 'number'
      ? sanitized.code
      : undefined,
    traceId: typeof sanitized.traceId === 'string' ? sanitized.traceId : undefined,
  }, error)
}

export async function request<TResponse, TBody = unknown>(
  options: RequestOptions<TBody>,
): Promise<TResponse> {
  const mode = authMode(options)
  const store = getAuthSessionStore()
  const accessToken = mode === 'none' ? undefined : store.getAccessToken()

  if (mode === 'required' && !accessToken) {
    const error = missingAccessTokenError()
    logRequestError(options, error)
    throw error
  }

  try {
    return await transportRequest<TResponse, TBody>(options, accessToken)
  }
  catch (error) {
    if (
      error instanceof ApiError
      && error.kind === 'unauthorized'
      && mode !== 'none'
      && !options.skipTokenRefresh
      && store.getRefreshToken()
    ) {
      try {
        const session = await refreshAccessTokenSingleFlight()
        return await transportRequest<TResponse, TBody>(
          { ...options, skipTokenRefresh: true },
          session.accessToken,
        )
      }
      catch (refreshError) {
        logRequestError(options, refreshError)
        throw refreshError
      }
    }

    logRequestError(options, error)
    throw error
  }
}

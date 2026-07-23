export type ApiErrorKind
  = | 'cancelled'
    | 'timeout'
    | 'network'
    | 'http'
    | 'business'
    | 'unauthorized'
    | 'forbidden'
    | 'decode'
    | 'unknown'

export interface ApiErrorInit {
  kind: ApiErrorKind
  message: string
  status?: number
  code?: string | number
  traceId?: string
  retryable?: boolean
  cause?: unknown
}

export class ApiError extends Error {
  readonly kind: ApiErrorKind
  readonly status?: number
  readonly code?: string | number
  readonly traceId?: string
  readonly retryable: boolean
  override readonly cause?: unknown

  constructor(init: ApiErrorInit) {
    super(init.message)
    this.name = 'ApiError'
    this.kind = init.kind
    this.status = init.status
    this.code = init.code
    this.traceId = init.traceId
    this.retryable = init.retryable ?? false
    this.cause = init.cause
  }
}

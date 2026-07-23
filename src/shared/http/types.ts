export type HttpMethod
  = | 'GET'
    | 'POST'
    | 'PUT'
    | 'PATCH'
    | 'DELETE'
    | 'HEAD'

export type QueryPrimitive = string | number | boolean | null | undefined

export type RequestAuthMode = 'required' | 'optional' | 'none'

export interface RequestOptions<TBody = unknown> {
  path: string
  method?: HttpMethod
  query?: Record<string, QueryPrimitive | QueryPrimitive[]>
  body?: TBody
  headers?: Record<string, string>
  auth?: RequestAuthMode
  signal?: AbortSignal
  timeoutMs?: number
  skipTokenRefresh?: boolean
}

export interface ApiEnvelope<T> {
  code: string | number
  message: string
  data: T
  traceId?: string
}

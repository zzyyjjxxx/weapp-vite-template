import type { ApiEnvelope, RequestOptions } from './types'

import { fetch as wevuFetch } from 'wevu/fetch'

import { env } from '@/shared/env'
import { mergeSignalWithTimeout } from './abort'
import { ApiError } from './errors'
import { buildUrl } from './url'

const DEFAULT_TIMEOUT_MS = 15_000
const SUCCESS_CODES = new Set<string | number>(['SUCCESS', 0, 200])

export type FetchImplementation = (
  input: string,
  init?: RequestInit,
) => Promise<Response>

let fetchImplementation: FetchImplementation = (input, init) => (
  wevuFetch(input, init as Parameters<typeof wevuFetch>[1])
)

export function setFetchImplementation(next: FetchImplementation): void {
  fetchImplementation = next
}

export function resetFetchImplementation(): void {
  fetchImplementation = (input, init) => (
    wevuFetch(input, init as Parameters<typeof wevuFetch>[1])
  )
}

interface ParsedResponse {
  payload?: unknown
  rawText: string
}

function hasHeader(headers: Record<string, string>, name: string): string | undefined {
  const key = Object.keys(headers).find(item => item.toLowerCase() === name.toLowerCase())
  return key
}

function setHeader(headers: Record<string, string>, name: string, value: string): void {
  const existingKey = hasHeader(headers, name)
  headers[existingKey ?? name] = value
}

function isEnvelope(value: unknown): value is ApiEnvelope<unknown> {
  if (typeof value !== 'object' || value === null) {
    return false
  }

  const record = value as Record<string, unknown>
  return (
    (typeof record.code === 'string' || typeof record.code === 'number')
    && typeof record.message === 'string'
    && Object.hasOwn(record, 'data')
  )
}

function getTraceId(payload: unknown, response: Response): string | undefined {
  if (isEnvelope(payload) && typeof payload.traceId === 'string') {
    return payload.traceId
  }

  return response.headers.get('x-trace-id') ?? undefined
}

function getMessage(payload: unknown, response: Response, fallback: string): string {
  if (isEnvelope(payload)) {
    return payload.message
  }

  return fallback || response.statusText || `HTTP ${response.status}`
}

function isRetryableStatus(status: number): boolean {
  return status === 408 || status === 425 || status === 429 || status >= 500
}

function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === 'AbortError'
}

function mapHttpError(payload: unknown, response: Response): ApiError {
  const status = response.status
  const kind = status === 401 ? 'unauthorized' : status === 403 ? 'forbidden' : 'http'
  const code = isEnvelope(payload) ? payload.code : undefined

  return new ApiError({
    kind,
    message: getMessage(payload, response, typeof payload === 'string' ? payload : ''),
    status,
    code,
    traceId: getTraceId(payload, response),
    retryable: isRetryableStatus(status),
  })
}

async function readResponse(response: Response): Promise<ParsedResponse> {
  const rawText = await response.text()
  if (rawText.trim() === '') {
    return { rawText }
  }

  try {
    return { payload: JSON.parse(rawText) as unknown, rawText }
  }
  catch (cause) {
    return { rawText, payload: cause }
  }
}

function decodeSuccess<TResponse>(payload: unknown, response: Response): TResponse {
  if (!isEnvelope(payload)) {
    throw new ApiError({
      kind: 'decode',
      message: '响应格式无法解析',
      status: response.status,
      traceId: getTraceId(payload, response),
      cause: payload,
    })
  }

  if (!SUCCESS_CODES.has(payload.code)) {
    throw new ApiError({
      kind: 'business',
      message: payload.message,
      status: response.status,
      code: payload.code,
      traceId: payload.traceId,
    })
  }

  return payload.data as TResponse
}

function toRequestBody<TBody>(options: RequestOptions<TBody>, method: string): string | undefined {
  if (options.body === undefined || method === 'GET' || method === 'HEAD') {
    return undefined
  }

  return JSON.stringify(options.body)
}

export async function transportRequest<TResponse, TBody = unknown>(
  options: RequestOptions<TBody>,
  accessToken?: string,
): Promise<TResponse> {
  const method = options.method ?? 'GET'
  const headers: Record<string, string> = {
    Accept: 'application/json',
    ...(options.headers ?? {}),
  }
  const body = toRequestBody(options, method)

  if (body !== undefined && !hasHeader(headers, 'content-type')) {
    headers['Content-Type'] = 'application/json'
  }
  if (accessToken) {
    setHeader(headers, 'Authorization', `Bearer ${accessToken}`)
  }

  const mergedSignal = mergeSignalWithTimeout(
    options.signal,
    Math.max(1, options.timeoutMs ?? DEFAULT_TIMEOUT_MS),
  )

  try {
    const response = await fetchImplementation(
      buildUrl(env.apiBaseUrl, options.path, options.query),
      {
        method,
        headers,
        body,
        signal: mergedSignal.signal,
      },
    )

    if (response.status === 204) {
      return undefined as TResponse
    }

    const parsed = await readResponse(response)
    const parseFailed = parsed.payload instanceof Error

    if (!response.ok) {
      if (parseFailed) {
        return Promise.reject(mapHttpError(parsed.rawText, response))
      }

      throw mapHttpError(parsed.payload, response)
    }

    if (parseFailed || parsed.payload === undefined) {
      throw new ApiError({
        kind: 'decode',
        message: '响应格式无法解析',
        status: response.status,
        traceId: getTraceId(parsed.payload, response),
        cause: parseFailed ? parsed.payload : parsed.rawText,
      })
    }

    return decodeSuccess<TResponse>(parsed.payload, response)
  }
  catch (cause) {
    if (cause instanceof ApiError) {
      throw cause
    }

    if (mergedSignal.didTimeout()) {
      throw new ApiError({
        kind: 'timeout',
        message: '请求超时',
        retryable: true,
        cause,
      })
    }

    if (options.signal?.aborted || isAbortError(cause)) {
      throw new ApiError({
        kind: 'cancelled',
        message: '请求已取消',
        cause,
      })
    }

    if (cause instanceof TypeError) {
      throw new ApiError({
        kind: 'network',
        message: '网络请求失败',
        retryable: true,
        cause,
      })
    }

    throw new ApiError({
      kind: 'unknown',
      message: '请求失败',
      cause,
    })
  }
  finally {
    mergedSignal.cleanup()
  }
}

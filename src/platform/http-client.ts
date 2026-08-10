import { wpi } from 'wevu/api'

import { API_BASE_URL } from './api-config'

export type HttpMethod = 'GET' | 'POST'

export interface MiniProgramResponse {
  statusCode: number
  data?: unknown
}

export interface MiniProgramRequestOptions {
  url: string
  method: HttpMethod
  data?: unknown
  header?: Record<string, string>
  dataType?: 'json'
  success?: (response: MiniProgramResponse) => void
  fail?: (error: unknown) => void
}

export type MiniProgramRequest = (options: MiniProgramRequestOptions) => unknown

export interface ApiRequestOptions {
  body?: unknown
  token?: string
}

export interface ApiClient {
  request: <T>(
    method: HttpMethod,
    path: string,
    options?: ApiRequestOptions,
  ) => Promise<T>
}

export class ApiError extends Error {
  readonly statusCode?: number
  readonly code?: string

  constructor(
    message: string,
    options: { statusCode?: number, code?: string } = {},
  ) {
    super(message)
    this.name = 'ApiError'
    this.statusCode = options.statusCode
    this.code = options.code
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function parseResponseData(data: unknown): unknown {
  if (typeof data !== 'string') {
    return data
  }

  const text = data.trim()
  if (!text) {
    return undefined
  }

  try {
    return JSON.parse(text) as unknown
  }
  catch {
    return data
  }
}

function readErrorCode(data: unknown): string | undefined {
  if (!isRecord(data)) {
    return undefined
  }

  const error = data.error ?? data.status
  return typeof error === 'string' && error ? error : undefined
}

function createResponseError(statusCode: number, data: unknown): ApiError {
  const code = readErrorCode(data)
  return new ApiError(
    code ? `API request failed: ${code}` : `API request failed (${statusCode})`,
    { statusCode, code },
  )
}

function createNetworkError(): ApiError {
  return new ApiError(
    'Unable to connect to the local API. Check that the development service is running.',
    { code: 'network_error' },
  )
}

function normalizeBaseUrl(baseUrl: string): string {
  const value = baseUrl.trim().replace(/\/+$/, '')
  if (!value) {
    throw new Error('API base URL is required.')
  }
  return value
}

function createRequestUrl(baseUrl: string, path: string): string {
  return `${baseUrl}/${path.replace(/^\/+/, '')}`
}

const defaultRequest = wpi.request as unknown as MiniProgramRequest

export function createApiClient(options: {
  baseUrl?: string
  request?: MiniProgramRequest
} = {}): ApiClient {
  const baseUrl = normalizeBaseUrl(options.baseUrl ?? API_BASE_URL)
  const request = options.request ?? defaultRequest

  return {
    request<T>(
      method: HttpMethod,
      path: string,
      requestOptions: ApiRequestOptions = {},
    ) {
      const header: Record<string, string> = {
        Accept: 'application/json',
      }
      if (requestOptions.body !== undefined) {
        header['Content-Type'] = 'application/json'
      }
      if (requestOptions.token) {
        header.Authorization = `Bearer ${requestOptions.token}`
      }

      return new Promise<T>((resolve, reject) => {
        try {
          request({
            url: createRequestUrl(baseUrl, path),
            method,
            data: requestOptions.body,
            header,
            dataType: 'json',
            success(response) {
              const data = parseResponseData(response.data)
              if (response.statusCode >= 200 && response.statusCode < 300) {
                resolve(data as T)
                return
              }
              reject(createResponseError(response.statusCode, data))
            },
            fail() {
              reject(createNetworkError())
            },
          })
        }
        catch {
          reject(createNetworkError())
        }
      })
    },
  }
}

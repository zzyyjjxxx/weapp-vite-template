import type { ApiError } from '@/shared/http/errors'

import { afterEach, describe, expect, it } from 'vitest'
import {
  resetFetchImplementation,
  setFetchImplementation,
  transportRequest,
} from '@/shared/http/transport'

afterEach(() => {
  resetFetchImplementation()
})

describe('transportRequest', () => {
  it('decodes a successful envelope and sends auth headers', async () => {
    let receivedInit: RequestInit | undefined

    setFetchImplementation(async (_input, init) => {
      receivedInit = init
      return new Response(JSON.stringify({
        code: 'SUCCESS',
        message: 'ok',
        data: { id: 'order-1' },
        traceId: 'trace-1',
      }), { status: 200 })
    })

    const result = await transportRequest<{ id: string }>({
      path: '/orders',
      method: 'GET',
      query: { page: 1 },
    }, 'access-token')

    expect(result).toEqual({ id: 'order-1' })
    expect(new Headers(receivedInit?.headers).get('authorization'))
      .toBe('Bearer access-token')
  })

  it('maps unauthorized HTTP responses to ApiError', async () => {
    setFetchImplementation(async () => {
      return new Response(JSON.stringify({
        code: 'UNAUTHORIZED',
        message: 'expired',
        data: null,
        traceId: 'trace-401',
      }), { status: 401 })
    })

    await expect(transportRequest({ path: '/orders' }, 'expired'))
      .rejects
      .toMatchObject<ApiError>({
        kind: 'unauthorized',
        status: 401,
        code: 'UNAUTHORIZED',
        traceId: 'trace-401',
      })
  })

  it('handles a 204 response as undefined success', async () => {
    setFetchImplementation(async () => new Response(null, { status: 204 }))

    await expect(transportRequest<void>({
      path: '/orders/order-1/cancel',
      method: 'POST',
    }, 'access-token')).resolves.toBeUndefined()
  })

  it('maps a successful HTTP response with a business error code', async () => {
    setFetchImplementation(async () => new Response(JSON.stringify({
      code: 'ORDER_NOT_CANCELLABLE',
      message: '订单不能取消',
      data: null,
      traceId: 'trace-business',
    }), { status: 200 }))

    await expect(transportRequest({ path: '/orders/order-1/cancel' }, 'access-token'))
      .rejects
      .toMatchObject<ApiError>({
        kind: 'business',
        code: 'ORDER_NOT_CANCELLABLE',
        traceId: 'trace-business',
      })
  })

  it('maps a non-JSON server failure to a retryable HTTP error', async () => {
    setFetchImplementation(async () => new Response('gateway unavailable', {
      status: 503,
      statusText: 'Service Unavailable',
    }))

    await expect(transportRequest({ path: '/orders' }, 'access-token'))
      .rejects
      .toMatchObject<ApiError>({
        kind: 'http',
        status: 503,
        message: 'gateway unavailable',
        retryable: true,
      })
  })

  it('maps TypeError to a retryable network error', async () => {
    setFetchImplementation(async () => {
      throw new TypeError('socket failed')
    })

    await expect(transportRequest({ path: '/orders' }, 'access-token'))
      .rejects
      .toMatchObject<ApiError>({
        kind: 'network',
        retryable: true,
      })
  })

  it('distinguishes external cancellation from timeout', async () => {
    setFetchImplementation(async (_input, init) => {
      return new Promise<Response>((_resolve, reject) => {
        init?.signal?.addEventListener('abort', () => {
          const error = new Error('aborted')
          error.name = 'AbortError'
          reject(error)
        }, { once: true })
      })
    })

    const controller = new AbortController()
    const cancelled = transportRequest({
      path: '/orders',
      signal: controller.signal,
      timeoutMs: 100,
    }, 'access-token')
    controller.abort()

    await expect(cancelled).rejects.toMatchObject<ApiError>({ kind: 'cancelled' })
    await expect(transportRequest({
      path: '/orders',
      timeoutMs: 1,
    }, 'access-token')).rejects.toMatchObject<ApiError>({
      kind: 'timeout',
      retryable: true,
    })
  })
})

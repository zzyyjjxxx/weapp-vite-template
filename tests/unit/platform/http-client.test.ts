import type { ApiError, MiniProgramRequestOptions } from '@/platform/http-client'

import { describe, expect, it } from 'vitest'
import { createApiClient } from '@/platform/http-client'

describe('api client', () => {
  it('builds JSON requests with the bearer token', async () => {
    let captured: MiniProgramRequestOptions | undefined
    const client = createApiClient({
      baseUrl: 'http://localhost:17163/',
      request: (options) => {
        captured = options
        options.success?.({ statusCode: 200, data: { ok: true } })
      },
    })

    await expect(client.request('POST', '/customapi/example', {
      body: { value: 1 },
      token: 'access-token',
    })).resolves.toEqual({ ok: true })

    expect(captured).toMatchObject({
      url: 'http://localhost:17163/customapi/example',
      method: 'POST',
      data: { value: 1 },
      header: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        'Authorization': 'Bearer access-token',
      },
      dataType: 'json',
    })
  })

  it('exposes the server error code and status', async () => {
    const client = createApiClient({
      request: options => options.success?.({
        statusCode: 404,
        data: { error: 'land_demand_not_found' },
      }),
    })

    await expect(
      client.request('GET', '/customapi/landdemandapi/getlanddemand'),
    ).rejects.toMatchObject<ApiError>({
      code: 'land_demand_not_found',
      statusCode: 404,
    })
  })

  it('normalizes request failures as network errors', async () => {
    const client = createApiClient({
      request: options => options.fail?.({ errMsg: 'request:fail' }),
    })

    await expect(
      client.request('GET', '/customapi/example'),
    ).rejects.toMatchObject<ApiError>({ code: 'network_error' })
  })
})

import type { LandDemandForm, SaveLandDemandPayload } from '@/features/land-demand/models'
import type { ApiClient } from '@/platform/http-client'

import { describe, expect, it } from 'vitest'
import { createHttpLandDemandRepository } from '@/features/land-demand/http-repository'
import { ApiError } from '@/platform/http-client'
import { createMemoryStorage } from '../../helpers/memory-storage'

const form: LandDemandForm = {
  county: 'County',
  region: 'Town',
  businessname: 'Example Enterprise',
  creditcode: '91330200EXAMPLE001',
  area: '30',
  building_area: '10000',
  expect_park: '330203',
  expect_time: '2027-06',
  is_deploy: '是',
  deploy_park: ['330203'],
  is_specialuse: '否',
  deploy_landtype: '',
  deploy_height: '',
  deploy_weight: '',
  investment: '1000.5',
  project_hydm: '1811',
  keyindustry: '智能制造',
  futureindustry: '智能装备',
  pred_ys: '2000',
  pred_tax: '100',
  pred_rdex: '200',
  pred_unitenergy: '3',
  projectdata: 'Project details',
  contact: 'Contact',
  office: 'Office',
  phone: '13800000000',
}

const savePayload: SaveLandDemandPayload = {
  ...form,
  deploy_park: form.deploy_park.join(','),
  landusedemand: '2',
}

function createClient(response: unknown): {
  client: ApiClient
  calls: Array<{ method: string, path: string, options?: { body?: unknown, token?: string } }>
} {
  const calls: Array<{ method: string, path: string, options?: { body?: unknown, token?: string } }> = []
  const client: ApiClient = {
    async request<T>(method, path, options) {
      calls.push({ method, path, options })
      return response as T
    },
  }
  return { client, calls }
}

function createSequencedClient(responses: unknown[]): {
  client: ApiClient
  calls: Array<{ method: string, path: string, options?: { body?: unknown, token?: string } }>
} {
  const calls: Array<{ method: string, path: string, options?: { body?: unknown, token?: string } }> = []
  const client: ApiClient = {
    async request<T>(method, path, options) {
      calls.push({ method, path, options })
      return responses.shift() as T
    },
  }
  return { client, calls }
}

describe('HTTP land-demand repository', () => {
  it('converts the expected land month between the UI and Excel date serial', async () => {
    const { client, calls } = createClient({
      businessname: 'Example Enterprise',
      creditcode: '91330200EXAMPLE001',
      county: 'County',
      region: 'Town',
      expect_time: 45992,
      landusedemand: '2',
    })
    const repository = createHttpLandDemandRepository({
      client,
      getAccessToken: () => 'access-token',
      storage: createMemoryStorage(),
    })

    const record = await repository.save({ ...savePayload, expect_time: '2025-12' })
    const body = calls[0]?.options?.body as Record<string, unknown>

    expect(body.expect_time).toBe('45992')
    expect(record.expect_time).toBe('2025-12')
  })

  it('accepts a legacy zero status as an unsubmitted record', async () => {
    const { client } = createClient({
      businessname: 'Example Enterprise',
      creditcode: '91330200EXAMPLE001',
      county: 'County',
      region: 'Town',
      landusedemand: '0',
    })
    const repository = createHttpLandDemandRepository({
      client,
      getAccessToken: () => 'access-token',
      storage: createMemoryStorage(),
    })

    await expect(repository.get('91330200EXAMPLE001')).resolves.toMatchObject({
      creditcode: '91330200EXAMPLE001',
      landusedemand: '0',
    })
  })

  it('filters identity fields and converts numeric values for writes', async () => {
    const { client, calls } = createClient({
      businessname: 'Example Enterprise',
      creditcode: '91330200EXAMPLE001',
      county: 'County',
      region: 'Town',
      is_deploy: '1',
      is_specialuse: '0',
      building_area: 10000,
      investment: 1000.5,
      landusedemand: '2',
      updatetime: '46244.5',
    })
    const repository = createHttpLandDemandRepository({
      client,
      getAccessToken: () => 'access-token',
      storage: createMemoryStorage(),
    })

    const record = await repository.save(savePayload)
    const body = calls[0]?.options?.body as Record<string, unknown>

    expect(calls[0]).toMatchObject({
      method: 'POST',
      path: '/customapi/landdemandapi/addlanddemand',
      options: { token: 'access-token' },
    })
    expect(body).toMatchObject({
      area: '30',
      building_area: 10000,
      investment: 1000.5,
      deploy_park: '330203',
      landusedemand: '2',
    })
    expect(body).not.toHaveProperty('businessname')
    expect(body).not.toHaveProperty('creditcode')
    expect(body).not.toHaveProperty('county')
    expect(body).not.toHaveProperty('region')
    expect(record.building_area).toBe('10000')
    expect(record.investment).toBe('1000.5')
    expect(record.updatetime).toBe('2026-08-10T12:00:00')
    expect(record.is_deploy).toBe('是')
    expect(record.is_specialuse).toBe('否')
    expect(record).not.toHaveProperty('is_financing')
    expect(record).not.toHaveProperty('financing_money')
    expect(record).not.toHaveProperty('financing_time')
  })

  it('keeps legacy timestamp strings readable while mapping serial timestamps', async () => {
    const { client } = createClient({
      businessname: 'Example Enterprise',
      creditcode: '91330200EXAMPLE001',
      county: 'County',
      region: 'Town',
      updatetime: '2026-08-10 12:00:00',
      landusedemand: '2',
    })
    const repository = createHttpLandDemandRepository({
      client,
      getAccessToken: () => 'access-token',
      storage: createMemoryStorage(),
    })

    await expect(repository.save(savePayload)).resolves.toMatchObject({
      updatetime: '2026-08-10 12:00:00',
    })
  })

  it('reads the saved record again after a write response has no body', async () => {
    const { client, calls } = createSequencedClient([
      undefined,
      {
        businessname: 'Example Enterprise',
        creditcode: '91330200EXAMPLE001',
        county: 'County',
        region: 'Town',
        landusedemand: '1',
      },
    ])
    const repository = createHttpLandDemandRepository({
      client,
      getAccessToken: () => 'access-token',
      storage: createMemoryStorage(),
    })

    await expect(repository.save(savePayload)).resolves.toMatchObject({
      creditcode: '91330200EXAMPLE001',
      landusedemand: '1',
    })
    expect(calls.map(call => `${call.method} ${call.path}`)).toEqual([
      'POST /customapi/landdemandapi/addlanddemand',
      'GET /customapi/landdemandapi/getlanddemand',
    ])
  })

  it('does not parse a success marker from the write route as a filing record', async () => {
    const { client, calls } = createSequencedClient([
      { success: true },
      {
        businessname: 'Example Enterprise',
        creditcode: '91330200EXAMPLE001',
        county: 'County',
        region: 'Town',
        landusedemand: '1',
      },
    ])
    const repository = createHttpLandDemandRepository({
      client,
      getAccessToken: () => 'access-token',
      storage: createMemoryStorage(),
    })

    await expect(repository.save(savePayload)).resolves.toMatchObject({
      creditcode: '91330200EXAMPLE001',
      landusedemand: '1',
    })
    expect(calls).toHaveLength(2)
    expect(calls[1]?.path).toBe('/customapi/landdemandapi/getlanddemand')
  })

  it('unwraps a record returned inside a data response envelope', async () => {
    const { client } = createClient({
      data: {
        businessname: 'Example Enterprise',
        creditcode: '91330200EXAMPLE001',
        county: 'County',
        region: 'Town',
        landusedemand: '1',
      },
    })
    const repository = createHttpLandDemandRepository({
      client,
      getAccessToken: () => 'access-token',
      storage: createMemoryStorage(),
    })

    await expect(repository.save(savePayload)).resolves.toMatchObject({
      creditcode: '91330200EXAMPLE001',
      landusedemand: '1',
    })
  })

  it('maps a missing remote record to the existing empty-result contract', async () => {
    const client: ApiClient = {
      async request() {
        throw new ApiError('missing', { statusCode: 404, code: 'land_demand_not_found' })
      },
    }
    const repository = createHttpLandDemandRepository({
      client,
      getAccessToken: () => 'access-token',
      storage: createMemoryStorage(),
    })

    await expect(repository.get('91330200EXAMPLE001')).resolves.toBeUndefined()
  })

  it('treats a status-only 404 from the GET endpoint as an empty record', async () => {
    const client: ApiClient = {
      async request() {
        throw new ApiError('not found', { statusCode: 404 })
      },
    }
    const repository = createHttpLandDemandRepository({
      client,
      getAccessToken: () => 'access-token',
      storage: createMemoryStorage(),
    })

    await expect(repository.get('91330200EXAMPLE002')).resolves.toBeUndefined()
  })

  it('keeps verification code testing local while using the HTTP repository', async () => {
    const { client } = createClient({})
    const repository = createHttpLandDemandRepository({
      client,
      getAccessToken: () => 'access-token',
      storage: createMemoryStorage(),
      randomCode: () => '123456',
    })

    const challenge = await repository.sendCode('13800000000')

    expect(challenge.mockCode).toBe('123456')
    await expect(repository.verifyCode(challenge.phone, challenge.mockCode)).resolves.toBeUndefined()
  })
})

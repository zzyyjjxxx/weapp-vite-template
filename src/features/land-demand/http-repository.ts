import type {
  LandDemandRecord,
  SaveLandDemandPayload,
  UpdateLandDemandPayload,
} from './models'
import type { LandDemandRepository } from './repository'

import type { ApiClient } from '@/platform/http-client'
import type { StorageAdapter } from '@/platform/storage'
import { ApiError } from '@/platform/http-client'
import { createWpiStorageAdapter } from '@/platform/storage'
import { createMockLandDemandRepository } from './repository'

const GET_PATH = '/customapi/landdemandapi/getlanddemand'
const ADD_PATH = '/customapi/landdemandapi/addlanddemand'
const UPDATE_PATH = '/customapi/landdemandapi/updatelanddemand'

const DECIMAL_FIELDS = [
  'building_area',
  'deploy_height',
  'deploy_weight',
  'investment',
  'pred_ys',
  'pred_tax',
  'pred_rdex',
  'pred_unitenergy',
  'financing_money',
] as const

const TEXT_FIELDS = [
  'area',
  'expect_park',
  'expect_time',
  'is_deploy',
  'deploy_park',
  'is_specialuse',
  'deploy_landtype',
  'project_hydm',
  'keyindustry',
  'futureindustry',
  'projectdata',
  'is_financing',
  'financing_time',
  'contact',
  'office',
  'phone',
] as const

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function readRecordValue(payload: object, field: string): unknown {
  return (payload as Record<string, unknown>)[field]
}

function readString(value: unknown): string {
  return typeof value === 'string' ? value : value == null ? '' : String(value)
}

function readDecimalString(value: unknown): string {
  if (typeof value === 'number' && Number.isFinite(value)) {
    return String(value)
  }
  return typeof value === 'string' ? value : ''
}

function readYesNo(value: unknown): LandDemandRecord['is_deploy'] {
  const choice = readString(value)
  return choice === '是' || choice === '1'
    ? '是'
    : choice === '否' || choice === '0'
      ? '否'
      : ''
}

function readFinancing(value: unknown): LandDemandRecord['is_financing'] {
  const choice = readString(value)
  return choice === '有' || choice === '1' ? '有' : '没有'
}

function readStatus(value: unknown): LandDemandRecord['landusedemand'] {
  const status = readString(value)
  if (status !== '1' && status !== '2') {
    throw new Error('用地需求接口返回的状态无效')
  }
  return status
}

function mapRecord(value: unknown): LandDemandRecord {
  if (!isRecord(value)) {
    throw new Error('用地需求接口返回数据格式错误')
  }

  return {
    businessname: readString(value.businessname),
    creditcode: readString(value.creditcode),
    county: readString(value.county),
    region: readString(value.region),
    area: readString(value.area),
    building_area: readDecimalString(value.building_area),
    expect_park: readString(value.expect_park),
    expect_time: readString(value.expect_time),
    is_deploy: readYesNo(value.is_deploy),
    deploy_park: readString(value.deploy_park),
    is_specialuse: readYesNo(value.is_specialuse),
    deploy_landtype: readString(value.deploy_landtype),
    deploy_height: readDecimalString(value.deploy_height),
    deploy_weight: readDecimalString(value.deploy_weight),
    investment: readDecimalString(value.investment),
    project_hydm: readString(value.project_hydm),
    keyindustry: readString(value.keyindustry),
    futureindustry: readString(value.futureindustry),
    pred_ys: readDecimalString(value.pred_ys),
    pred_tax: readDecimalString(value.pred_tax),
    pred_rdex: readDecimalString(value.pred_rdex),
    pred_unitenergy: readDecimalString(value.pred_unitenergy),
    projectdata: readString(value.projectdata),
    is_financing: readFinancing(value.is_financing),
    financing_money: readDecimalString(value.financing_money),
    financing_time: readString(value.financing_time),
    contact: readString(value.contact),
    office: readString(value.office),
    phone: readString(value.phone),
    landusedemand: readStatus(value.landusedemand),
    updatetime: readString(value.updatetime),
    updateuser: readString(value.updateuser),
  }
}

function readOptionalString(value: unknown): string | undefined {
  if (typeof value !== 'string' || !value) {
    return undefined
  }
  return value
}

function readOptionalDecimal(value: unknown, field: string): number | undefined {
  if (value === undefined || value === null || value === '') {
    return undefined
  }

  const number = typeof value === 'number' ? value : Number(value)
  if (!Number.isFinite(number)) {
    throw new TypeError(`字段 ${field} 必须是数字`)
  }
  return number
}

function toWritePayload(payload: SaveLandDemandPayload | UpdateLandDemandPayload): Record<string, unknown> {
  const body: Record<string, unknown> = {}
  for (const field of TEXT_FIELDS) {
    const value = readOptionalString(readRecordValue(payload, field))
    if (value !== undefined) {
      body[field] = value
    }
  }
  for (const field of DECIMAL_FIELDS) {
    const value = readOptionalDecimal(readRecordValue(payload, field), field)
    if (value !== undefined) {
      body[field] = value
    }
  }

  body.landusedemand = readString(readRecordValue(payload, 'landusedemand'))
  return body
}

function mapLandDemandError(error: unknown, fallback: string): Error {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error : new Error(fallback)
  }

  switch (error.code) {
    case 'invalid_token':
      return new Error('登录状态已失效，请重新登录')
    case 'enterprise_not_found':
      return new Error('企业信息不存在')
    case 'land_demand_exists':
      return new Error('填报记录已存在')
    case 'land_demand_not_found':
      return new Error('填报记录不存在')
    case 'invalid_request':
      return new Error('用地需求数据格式不正确')
    case 'network_error':
      return new Error('无法连接本地 API，请确认 http://localhost:17163/ 可访问')
    default:
      return new Error(fallback)
  }
}

function readToken(getAccessToken: () => string | undefined): string {
  const token = getAccessToken()
  if (!token) {
    throw new Error('登录状态已失效，请重新登录')
  }
  return token
}

export function createHttpLandDemandRepository(options: {
  client: ApiClient
  getAccessToken: () => string | undefined
  storage?: StorageAdapter
  randomCode?: () => string
}): LandDemandRepository {
  const localRepository = createMockLandDemandRepository({
    storage: options.storage ?? createWpiStorageAdapter(),
    randomCode: options.randomCode,
  })

  async function requestRecord(
    method: 'GET' | 'POST',
    path: string,
    body?: SaveLandDemandPayload | UpdateLandDemandPayload,
  ): Promise<LandDemandRecord> {
    try {
      const response = await options.client.request<unknown>(method, path, {
        body: body ? toWritePayload(body) : undefined,
        token: readToken(options.getAccessToken),
      })
      return mapRecord(response)
    }
    catch (error) {
      throw mapLandDemandError(error, '用地需求请求失败，请稍后重试')
    }
  }

  return {
    async get() {
      try {
        const response = await options.client.request<unknown>('GET', GET_PATH, {
          token: readToken(options.getAccessToken),
        })
        return mapRecord(response)
      }
      catch (error) {
        if (error instanceof ApiError && error.code === 'land_demand_not_found') {
          return undefined
        }
        throw mapLandDemandError(error, '查询用地需求失败，请稍后重试')
      }
    },

    save: payload => requestRecord('POST', ADD_PATH, payload),
    update: payload => requestRecord('POST', UPDATE_PATH, payload),

    getDraft: creditcode => localRepository.getDraft(creditcode),
    setDraft: (creditcode, draft) => localRepository.setDraft(creditcode, draft),
    removeDraft: creditcode => localRepository.removeDraft(creditcode),

    // SMS is intentionally mocked until the local development service is
    // reachable. Persisted drafts remain local for the same reason.
    sendCode: phone => localRepository.sendCode(phone),
    verifyCode: (phone, code) => localRepository.verifyCode(phone, code),
  }
}

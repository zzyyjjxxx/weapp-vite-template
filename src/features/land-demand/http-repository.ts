import type {
  LandDemandRecord,
  LandDemandRecordStatus,
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
] as const

const TEXT_FIELDS = [
  'area',
  'expect_park',
  'is_deploy',
  'deploy_park',
  'is_specialuse',
  'deploy_landtype',
  'project_hydm',
  'keyindustry',
  'futureindustry',
  'projectdata',
  'contact',
  'office',
  'phone',
] as const

const EXPECT_TIME_PATTERN = /^(\d{4})-(0[1-9]|1[0-2])$/
const EXCEL_DATE_EPOCH_UTC = Date.UTC(1899, 11, 30)
const MILLISECONDS_PER_DAY = 24 * 60 * 60 * 1000

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

function readExcelDateTime(value: unknown): string {
  if (value === undefined || value === null || value === '') {
    return ''
  }

  const text = String(value).trim()
  if (!/^\d+(?:\.\d+)?$/.test(text)) {
    return text
  }

  const serial = Number(text)
  if (!Number.isFinite(serial)) {
    return text
  }

  const date = new Date(EXCEL_DATE_EPOCH_UTC + Math.round(serial * MILLISECONDS_PER_DAY))
  if (Number.isNaN(date.getTime())) {
    return text
  }

  const pad = (part: number): string => String(part).padStart(2, '0')
  return `${date.getUTCFullYear()}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())}T${pad(date.getUTCHours())}:${pad(date.getUTCMinutes())}:${pad(date.getUTCSeconds())}`
}

function readExpectTime(value: unknown): string {
  if (value === undefined || value === null || value === '') {
    return ''
  }

  const text = readString(value).trim()
  const serial = typeof value === 'number'
    ? value
    : /^\d+(?:\.\d+)?$/.test(text) ? Number(text) : undefined

  if (serial !== undefined && Number.isFinite(serial)) {
    const date = new Date(EXCEL_DATE_EPOCH_UTC + Math.trunc(serial * MILLISECONDS_PER_DAY))
    if (!Number.isNaN(date.getTime())) {
      return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, '0')}`
    }
  }

  const datePrefix = /^(\d{4})-(0[1-9]|1[0-2])(?:-\d{2})?/.exec(text)
  return datePrefix ? `${datePrefix[1]}-${datePrefix[2]}` : text
}

function writeExpectTime(value: unknown): string | undefined {
  if (value === undefined || value === null || value === '') {
    return undefined
  }

  const text = readString(value).trim()
  const match = EXPECT_TIME_PATTERN.exec(text)
  if (!match) {
    throw new TypeError('字段 expect_time 必须是 YYYY-MM 格式')
  }

  const date = Date.UTC(Number(match[1]), Number(match[2]) - 1, 1)
  return String(Math.round((date - EXCEL_DATE_EPOCH_UTC) / MILLISECONDS_PER_DAY))
}

function readYesNo(value: unknown): LandDemandRecord['is_deploy'] {
  const choice = readString(value)
  return choice === '是' || choice === '1'
    ? '是'
    : choice === '否' || choice === '0'
      ? '否'
      : ''
}

function readStatus(value: unknown): LandDemandRecordStatus {
  const status = readString(value)
  if (status === '0') {
    return '0'
  }
  if (status !== '1' && status !== '2') {
    throw new Error('用地需求接口返回的状态无效')
  }
  return status
}

function unwrapRecord(value: unknown): unknown {
  if (!isRecord(value)) {
    return value
  }

  for (const key of ['data', 'record', 'result'] as const) {
    const nested = value[key]
    if (isRecord(nested)) {
      return nested
    }
  }

  return value
}

function mapRecord(value: unknown): LandDemandRecord {
  const record = unwrapRecord(value)
  if (!isRecord(record)) {
    throw new Error('用地需求接口返回数据格式错误')
  }

  return {
    businessname: readString(record.businessname),
    creditcode: readString(record.creditcode),
    county: readString(record.county),
    region: readString(record.region),
    area: readString(record.area),
    building_area: readDecimalString(record.building_area),
    expect_park: readString(record.expect_park),
    expect_time: readExpectTime(record.expect_time),
    is_deploy: readYesNo(record.is_deploy),
    deploy_park: readString(record.deploy_park),
    is_specialuse: readYesNo(record.is_specialuse),
    deploy_landtype: readString(record.deploy_landtype),
    deploy_height: readDecimalString(record.deploy_height),
    deploy_weight: readDecimalString(record.deploy_weight),
    investment: readDecimalString(record.investment),
    project_hydm: readString(record.project_hydm),
    keyindustry: readString(record.keyindustry),
    futureindustry: readString(record.futureindustry),
    pred_ys: readDecimalString(record.pred_ys),
    pred_tax: readDecimalString(record.pred_tax),
    pred_rdex: readDecimalString(record.pred_rdex),
    pred_unitenergy: readDecimalString(record.pred_unitenergy),
    projectdata: readString(record.projectdata),
    contact: readString(record.contact),
    office: readString(record.office),
    phone: readString(record.phone),
    landusedemand: readStatus(record.landusedemand),
    updatetime: readExcelDateTime(record.updatetime),
    updateuser: readString(record.updateuser),
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

  const expectTime = writeExpectTime(readRecordValue(payload, 'expect_time'))
  if (expectTime !== undefined) {
    body.expect_time = expectTime
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
      return new Error('无法连接 API，请检查网络或稍后重试')
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
      const token = readToken(options.getAccessToken)
      await options.client.request<unknown>(method, path, {
        body: body ? toWritePayload(body) : undefined,
        token,
      })

      // The write route may return an empty body, a success marker, or a
      // response envelope depending on the deployed Forguncy version. Always
      // read the canonical record after a successful write so that the
      // verification dialog never tries to parse the write acknowledgement as
      // a land-demand record.
      const refreshed = await options.client.request<unknown>('GET', GET_PATH, { token })
      return mapRecord(refreshed)
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
        if (
          error instanceof ApiError
          && (error.code === 'land_demand_not_found' || error.statusCode === 404)
        ) {
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
    clearDrafts: creditcode => localRepository.clearDrafts(creditcode),

    // SMS is intentionally mocked until the local development service is
    // reachable. Persisted drafts remain local for the same reason.
    sendCode: phone => localRepository.sendCode(phone),
    verifyCode: (phone, code) => localRepository.verifyCode(phone, code),
  }
}

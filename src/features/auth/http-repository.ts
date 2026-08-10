import type {
  AuthRepository,
  AuthSession,
  EnterpriseInfo,
  EnterpriseProfile,
  LoginInput,
} from './models'

import type { ApiClient } from '@/platform/http-client'
import { ApiError } from '@/platform/http-client'

const LOGIN_PATH = '/customapi/enterpriseapi/login'
const REFRESH_PATH = '/customapi/enterpriseapi/refresh'
const GET_INFO_PATH = '/customapi/enterpriseapi/getinfo'

interface TokenResponse {
  access_token: string
  refresh_token: string
  token_type?: string
  expires_in: number
  refresh_expires_in: number
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function readString(value: unknown, field: string): string {
  if (typeof value !== 'string' || !value) {
    throw new Error(`API response field ${field} is invalid.`)
  }
  return value
}

function readSeconds(value: unknown, field: string): number {
  const seconds = typeof value === 'number' ? value : Number(value)
  if (!Number.isFinite(seconds) || seconds <= 0) {
    throw new Error(`API response field ${field} is invalid.`)
  }
  return seconds
}

function readTokenResponse(value: unknown): TokenResponse {
  if (!isRecord(value)) {
    throw new Error('The authentication response is invalid.')
  }

  return {
    access_token: readString(value.access_token, 'access_token'),
    refresh_token: readString(value.refresh_token, 'refresh_token'),
    token_type: typeof value.token_type === 'string' ? value.token_type : undefined,
    expires_in: readSeconds(value.expires_in, 'expires_in'),
    refresh_expires_in: readSeconds(value.refresh_expires_in, 'refresh_expires_in'),
  }
}

function readEnterpriseInfo(value: unknown): EnterpriseInfo {
  if (!isRecord(value)) {
    throw new Error('The enterprise response is invalid.')
  }

  return {
    businessname: readString(value.businessname, 'businessname'),
    creditcode: readString(value.creditcode, 'creditcode'),
    county: readString(value.county, 'county'),
    region: readString(value.region, 'region'),
  }
}

function mapEnterpriseProfile(
  username: string,
  info: EnterpriseInfo,
  previous?: EnterpriseProfile,
): EnterpriseProfile {
  return {
    id: previous?.id ?? info.creditcode,
    username: previous?.username ?? username,
    businessname: info.businessname,
    creditcode: info.creditcode,
    county: info.county,
    region: info.region,
    // getinfo deliberately returns only the authenticated enterprise
    // identity. Keep editable contact fields when refreshing a session.
    contact: previous?.contact ?? '',
    office: previous?.office ?? '',
    phone: previous?.phone ?? '',
  }
}

function mapAuthError(error: unknown, fallback: string): Error {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error : new Error(fallback)
  }

  switch (error.code) {
    case 'invalid_credentials':
      return new Error('账号或密码错误')
    case 'invalid_request':
      return new Error('登录请求无效')
    case 'enterprise_not_found':
      return new Error('企业信息不存在')
    case 'invalid_token':
    case 'invalid_refresh_token':
      return new Error('登录状态已失效，请重新登录')
    case 'network_error':
      return new Error('无法连接本地 API，请确认 http://localhost:17163/ 可访问')
    default:
      return new Error(fallback)
  }
}

export function createHttpAuthRepository(options: {
  client: ApiClient
  now?: () => number
}): AuthRepository {
  const now = options.now ?? Date.now

  async function getInfo(token: string): Promise<EnterpriseInfo> {
    try {
      const response = await options.client.request<unknown>('GET', GET_INFO_PATH, { token })
      return readEnterpriseInfo(response)
    }
    catch (error) {
      throw mapAuthError(error, '获取企业信息失败，请稍后重试')
    }
  }

  async function readTokens(
    method: 'POST',
    path: string,
    body: unknown,
  ): Promise<TokenResponse> {
    try {
      const response = await options.client.request<unknown>(method, path, { body })
      return readTokenResponse(response)
    }
    catch (error) {
      throw mapAuthError(error, '认证请求失败，请稍后重试')
    }
  }

  function createSession(
    tokens: TokenResponse,
    username: string,
    info: EnterpriseInfo,
    previous?: AuthSession,
  ): AuthSession {
    const timestamp = now()
    return {
      token: tokens.access_token,
      refreshToken: tokens.refresh_token,
      tokenType: tokens.token_type ?? 'Bearer',
      expiresAt: timestamp + tokens.expires_in * 1_000,
      refreshExpiresAt: timestamp + tokens.refresh_expires_in * 1_000,
      enterprise: mapEnterpriseProfile(username, info, previous?.enterprise),
    }
  }

  return {
    async login(input: LoginInput): Promise<AuthSession> {
      const tokens = await readTokens('POST', LOGIN_PATH, input)
      const info = await getInfo(tokens.access_token)
      return createSession(tokens, input.username, info)
    },

    async refresh(session: AuthSession): Promise<AuthSession> {
      if (!session.refreshToken) {
        throw new Error('登录状态无法刷新，请重新登录')
      }

      const tokens = await readTokens('POST', REFRESH_PATH, {
        refresh_token: session.refreshToken,
      })
      const info = await getInfo(tokens.access_token)
      return createSession(tokens, session.enterprise.username, info, session)
    },

    getInfo,
  }
}

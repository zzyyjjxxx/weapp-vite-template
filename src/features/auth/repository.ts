import type {
  AuthRepository,
  AuthSession,
  EnterpriseProfile,
  LoginInput,
} from './models'

const DEMO_CREDENTIALS = {
  username: 'demo',
  password: 'demo123',
} as const

const DEMO_ENTERPRISE: EnterpriseProfile = {
  id: 'enterprise-demo',
  username: 'demo',
  businessname: '宁波示范智造有限公司',
  creditcode: '91330200MA2DEMO001',
  county: '鄞州区',
  region: '首南街道',
  contact: '张示例',
  office: '法定代表人',
  phone: '13800000000',
}

const SESSION_DURATION_MS = 8 * 60 * 60 * 1_000
const REFRESH_DURATION_MS = 7 * 24 * 60 * 60 * 1_000

let configuredRepository: AuthRepository | undefined

function cloneSession(session: AuthSession): AuthSession {
  return {
    ...session,
    enterprise: { ...session.enterprise },
  }
}

function wait(delayMs: number): Promise<void> {
  return delayMs > 0
    ? new Promise(resolve => setTimeout(resolve, delayMs))
    : Promise.resolve()
}

export function createMockAuthRepository(options: {
  now?: () => number
  delayMs?: number
} = {}): AuthRepository {
  const now = options.now ?? Date.now
  const delayMs = options.delayMs ?? 0

  return {
    async login(input: LoginInput): Promise<AuthSession> {
      await wait(delayMs)

      if (
        input.username !== DEMO_CREDENTIALS.username
        || input.password !== DEMO_CREDENTIALS.password
      ) {
        throw new Error('账号或密码错误')
      }

      return cloneSession({
        token: 'mock-demo-session-token',
        refreshToken: 'mock-demo-refresh-token',
        tokenType: 'Bearer',
        expiresAt: now() + SESSION_DURATION_MS,
        refreshExpiresAt: now() + REFRESH_DURATION_MS,
        enterprise: DEMO_ENTERPRISE,
      })
    },

    async refresh(session: AuthSession): Promise<AuthSession> {
      await wait(delayMs)
      if (session.refreshToken !== 'mock-demo-refresh-token') {
        throw new Error('登录状态已失效，请重新登录')
      }

      return cloneSession({
        ...session,
        token: 'mock-demo-session-token',
        expiresAt: now() + SESSION_DURATION_MS,
        refreshExpiresAt: now() + REFRESH_DURATION_MS,
      })
    },
  }
}

export function configureAuthRepository(repository?: AuthRepository): void {
  configuredRepository = repository
}

export function getAuthRepository(): AuthRepository {
  return configuredRepository ?? createMockAuthRepository()
}

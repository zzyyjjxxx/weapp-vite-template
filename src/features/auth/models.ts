export interface LoginInput {
  username: string
  password: string
}

export interface EnterpriseProfile {
  id: string
  username: string
  businessname: string
  creditcode: string
  county: string
  region: string
  contact: string
  office: string
  phone: string
}

export interface EnterpriseInfo {
  businessname: string
  creditcode: string
  county: string
  region: string
  phone?: string
}

export interface AuthSession {
  token: string
  refreshToken?: string
  tokenType?: string
  expiresAt: number
  refreshExpiresAt?: number
  enterprise: EnterpriseProfile
}

export interface AuthRepository {
  login: (input: LoginInput) => Promise<AuthSession>
  refresh?: (session: AuthSession) => Promise<AuthSession>
  getInfo?: (token: string) => Promise<EnterpriseInfo>
}

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

export interface AuthSession {
  token: string
  expiresAt: number
  enterprise: EnterpriseProfile
}

export interface AuthRepository {
  login: (input: LoginInput) => Promise<AuthSession>
}

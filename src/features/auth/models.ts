import type { AuthSession } from '@/shared/http/session'

export interface LoginInput {
  username: string
  password: string
}

export interface User {
  id: string
  username: string
  displayName: string
  tenantId: string
}

export type { AuthSession }

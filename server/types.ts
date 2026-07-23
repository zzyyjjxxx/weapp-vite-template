export type OrderStatus = 'pending' | 'processing' | 'completed' | 'cancelled'

export interface User {
  id: string
  username: string
  displayName: string
  tenantId: string
}

export interface AuthSession {
  accessToken: string
  refreshToken: string
  expiresAt: number
  userId: string
  tenantId: string
}

export interface Order {
  id: string
  number: string
  status: OrderStatus
  statusLabel: string
  amount: number
  createdAt: string
  canCancel: boolean
}

export interface OrderListInput {
  page: number
  pageSize: number
  status?: OrderStatus
  keyword?: string
}

export interface OrderListResult {
  items: Order[]
  total: number
  page: number
  pageSize: number
}

export interface ApiEnvelope<T> {
  code: string | number
  message: string
  data: T
  traceId: string
}

export interface AppEnv {
  Variables: {
    user: User
  }
}

export interface SessionRecord {
  session: AuthSession
  userId: string
}

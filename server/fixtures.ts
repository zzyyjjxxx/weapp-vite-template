import type {
  AuthSession,
  Order,
  OrderStatus,
  SessionRecord,
  User,
} from './types'

import { randomUUID } from 'node:crypto'

export const demoUser: User = {
  id: 'user-demo',
  username: 'demo',
  displayName: '演示用户',
  tenantId: 'tenant-demo',
}

export const demoPassword = 'demo123'

const initialOrders: Order[] = [
  {
    id: 'order-1001',
    number: 'NO202607230001',
    status: 'pending',
    statusLabel: '待支付',
    amount: 128.5,
    createdAt: '2026-07-23T08:00:00.000Z',
    canCancel: true,
  },
  {
    id: 'order-1002',
    number: 'NO202607220002',
    status: 'processing',
    statusLabel: '处理中',
    amount: 399,
    createdAt: '2026-07-22T09:30:00.000Z',
    canCancel: true,
  },
  {
    id: 'order-1003',
    number: 'NO202607210003',
    status: 'completed',
    statusLabel: '已完成',
    amount: 68,
    createdAt: '2026-07-21T13:10:00.000Z',
    canCancel: false,
  },
  {
    id: 'order-1004',
    number: 'NO202607200004',
    status: 'cancelled',
    statusLabel: '已取消',
    amount: 88,
    createdAt: '2026-07-20T16:45:00.000Z',
    canCancel: false,
  },
]

function cloneOrders(source: Order[]): Order[] {
  return source.map(order => ({ ...order }))
}

let orders = cloneOrders(initialOrders)
const sessionsByAccessToken = new Map<string, SessionRecord>()
const refreshTokens = new Map<string, SessionRecord>()

function createToken(prefix: string): string {
  return `${prefix}-${randomUUID()}`
}

function createSession(userId: string): AuthSession {
  return {
    accessToken: createToken('access'),
    refreshToken: createToken('refresh'),
    expiresAt: Date.now() + 15 * 60_000,
    userId,
    tenantId: demoUser.tenantId,
  }
}

export function resetFixtures(): void {
  orders = cloneOrders(initialOrders)
  sessionsByAccessToken.clear()
  refreshTokens.clear()
}

export function findUserByCredentials(
  username: string,
  password: string,
): User | undefined {
  if (username !== demoUser.username || password !== demoPassword) {
    return undefined
  }

  return demoUser
}

export function findUserById(userId: string): User | undefined {
  return userId === demoUser.id ? demoUser : undefined
}

export function issueSession(userId: string): AuthSession {
  const session = createSession(userId)
  const record: SessionRecord = { session, userId }
  sessionsByAccessToken.set(session.accessToken, record)
  refreshTokens.set(session.refreshToken, record)
  return session
}

export function refreshSession(refreshToken: string): AuthSession | undefined {
  const record = refreshTokens.get(refreshToken)
  if (!record) {
    return undefined
  }

  refreshTokens.delete(refreshToken)
  sessionsByAccessToken.delete(record.session.accessToken)

  const session = createSession(record.userId)
  const nextRecord: SessionRecord = { session, userId: record.userId }
  sessionsByAccessToken.set(session.accessToken, nextRecord)
  refreshTokens.set(session.refreshToken, nextRecord)
  return session
}

export function resolveUserByAccessToken(accessToken: string): User | undefined {
  const record = sessionsByAccessToken.get(accessToken)
  if (!record || record.session.expiresAt <= Date.now()) {
    if (record) {
      sessionsByAccessToken.delete(accessToken)
    }
    return undefined
  }

  return findUserById(record.userId)
}

export function getOrders(): Order[] {
  return cloneOrders(orders)
}

export function getOrderById(id: string): Order | undefined {
  const order = orders.find(item => item.id === id)
  return order ? { ...order } : undefined
}

export function cancelOrderById(id: string): Order | undefined {
  const order = orders.find(item => item.id === id)
  if (!order) {
    return undefined
  }

  order.status = 'cancelled' satisfies OrderStatus
  order.statusLabel = '已取消'
  order.canCancel = false
  return { ...order }
}

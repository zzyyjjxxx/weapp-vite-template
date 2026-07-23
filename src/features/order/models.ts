export type OrderStatus = 'pending' | 'processing' | 'completed' | 'cancelled'

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

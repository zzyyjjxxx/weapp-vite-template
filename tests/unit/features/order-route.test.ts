import { describe, expect, it } from 'vitest'

import { readOrderId } from '@/features/order/route'

describe('order route query', () => {
  it('reads a scalar order id and rejects non-scalar values', () => {
    expect(readOrderId({ id: 'order-1' })).toBe('order-1')
    expect(readOrderId({ id: ['order-1'] })).toBe('')
    expect(readOrderId({ id: null })).toBe('')
  })
})

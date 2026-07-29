import { describe, expect, it, vi } from 'vitest'

import { createWpiStorageAdapter } from '@/platform/storage'

describe('WPI storage adapter', () => {
  it('returns undefined when a storage read is unavailable', () => {
    const adapter = createWpiStorageAdapter({
      getStorageSync: () => { throw new Error('missing') },
      setStorageSync: vi.fn(),
      removeStorageSync: vi.fn(),
    })

    expect(adapter.get('missing')).toBeUndefined()
  })

  it('propagates write and remove failures to repository callers', () => {
    const adapter = createWpiStorageAdapter({
      getStorageSync: vi.fn(),
      setStorageSync: () => { throw new Error('quota exceeded') },
      removeStorageSync: () => { throw new Error('remove denied') },
    })

    expect(() => adapter.set('record', { value: 1 })).toThrow('quota exceeded')
    expect(() => adapter.remove('draft')).toThrow('remove denied')
  })
})

import { describe, expect, it, vi } from 'vitest'

import { createWpiStorageAdapter } from '@/platform/storage'

describe('WPI storage adapter', () => {
  it('keeps a missing key as an empty result', () => {
    const adapter = createWpiStorageAdapter({
      getStorageSync: () => undefined,
      setStorageSync: vi.fn(),
      removeStorageSync: vi.fn(),
      getStorageInfoSync: () => ({ keys: ['one', 2, 'two'] }),
    })

    expect(adapter.get('missing')).toBeUndefined()
    expect(adapter.keys?.()).toEqual(['one', 'two'])
  })

  it('propagates genuine storage read failures', () => {
    const adapter = createWpiStorageAdapter({
      getStorageSync: () => { throw new Error('missing') },
      setStorageSync: vi.fn(),
      removeStorageSync: vi.fn(),
    })

    expect(() => adapter.get('record')).toThrow('missing')
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

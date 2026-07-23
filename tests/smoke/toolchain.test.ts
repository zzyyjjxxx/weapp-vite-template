import { describe, expect, it } from 'vitest'

describe('toolchain bootstrap', () => {
  it('loads the test runner and exposes a stable project marker', () => {
    expect('weapp-vite-wevu-hono').toBe('weapp-vite-wevu-hono')
  })
})

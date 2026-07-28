import { afterEach, describe, expect, it } from 'vitest'

import { createQueryClient } from '@/shared/query/client'

describe('query client', () => {
  let client: ReturnType<typeof createQueryClient> | undefined

  afterEach(() => {
    client?.clear()
    client?.unmount()
  })

  it('uses the project query and mutation defaults', () => {
    client = createQueryClient()
    const defaults = client.getDefaultOptions()
    expect(defaults.queries?.staleTime).toBe(30_000)
    expect(defaults.queries?.gcTime).toBe(300_000)
    expect(defaults.queries?.refetchOnWindowFocus).toBe(false)
    expect(defaults.queries?.refetchOnReconnect).toBe(true)
    expect(defaults.mutations?.retry).toBe(0)
    expect(defaults.queries?.retry).toBe(0)
  })
})

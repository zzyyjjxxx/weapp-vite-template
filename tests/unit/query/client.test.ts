import { MutationObserver } from '@tanstack/query-core'
import { afterEach, describe, expect, it, vi } from 'vitest'

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

  it('does not log errors that are handled by the verification dialog', async () => {
    client = createQueryClient()
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined)
    const mutation = new MutationObserver(client, {
      mutationKey: ['verification', 'send-code'],
      mutationFn: async () => {
        throw new Error('验证码错误')
      },
      meta: { suppressGlobalErrorLog: true },
    })

    await expect(mutation.mutate()).rejects.toThrow('验证码错误')
    expect(consoleError).not.toHaveBeenCalled()
    consoleError.mockRestore()
  })
})

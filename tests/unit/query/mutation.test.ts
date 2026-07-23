import type { QueryLifecycleAdapter } from '@/shared/query/lifecycle'

import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { createQueryClient } from '@/shared/query/client'
import {
  configureQueryLifecycleAdapter,

  resetQueryLifecycleAdapter,
} from '@/shared/query/lifecycle'
import { useMutation } from '@/shared/query/use-mutation'

function createLifecycle(): QueryLifecycleAdapter & { dispose: () => void } {
  const callbacks: Array<() => void> = []
  return {
    onUnmounted: (callback) => { callbacks.push(callback) },
    dispose: () => {
      for (const callback of callbacks.splice(0)) {
        callback()
      }
    },
  }
}

describe('useMutation', () => {
  let lifecycle: ReturnType<typeof createLifecycle>
  let client: ReturnType<typeof createQueryClient>

  beforeEach(() => {
    lifecycle = createLifecycle()
    configureQueryLifecycleAdapter(lifecycle)
    client = createQueryClient()
  })

  afterEach(() => {
    lifecycle.dispose()
    client.clear()
    client.unmount()
    resetQueryLifecycleAdapter()
  })

  it('exposes mutation data and resets state after an error', async () => {
    let shouldFail = false
    const mutation = useMutation(() => ({
      mutationKey: ['test', 'mutation'],
      mutationFn: async (value: string) => {
        if (shouldFail) {
          throw new Error('failed')
        }
        return value.toUpperCase()
      },
    }), client)

    await expect(mutation.mutateAsync('ok')).resolves.toBe('OK')
    expect(mutation.data.value).toBe('OK')
    expect(mutation.isSuccess.value).toBe(true)

    shouldFail = true
    await expect(mutation.mutateAsync('bad')).rejects.toThrow('failed')
    expect(mutation.isError.value).toBe(true)
    mutation.reset()
    expect(mutation.isIdle.value).toBe(true)
    expect(mutation.error.value).toBeNull()
  })
})

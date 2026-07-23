import type { QueryLifecycleAdapter } from '@/shared/query/lifecycle'

import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { nextTick, ref } from 'wevu'
import { createQueryClient } from '@/shared/query/client'
import {
  configureQueryLifecycleAdapter,

  resetQueryLifecycleAdapter,
} from '@/shared/query/lifecycle'
import { useQuery } from '@/shared/query/use-query'

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

describe('useQuery', () => {
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

  it('bridges one observer to Wevu refs and disposes it', async () => {
    const hook = useQuery(() => ({
      queryKey: ['test', 'one'] as const,
      queryFn: async () => 'ok',
    }), client)

    await hook.refetch()

    expect(hook.data.value).toBe('ok')
    expect(hook.isSuccess.value).toBe(true)
    expect(client.getQueryCache().find({ queryKey: ['test', 'one'] })?.getObserversCount())
      .toBe(1)

    lifecycle.dispose()

    expect(client.getQueryCache().find({ queryKey: ['test', 'one'] })?.getObserversCount())
      .toBe(0)
  })

  it('reacts to resolver key changes and does not fetch disabled queries', async () => {
    const key = ref('one')
    let calls = 0
    const hook = useQuery(() => ({
      queryKey: ['test', key.value] as const,
      queryFn: async ({ queryKey }) => {
        calls += 1
        return queryKey[1]
      },
      enabled: key.value !== 'disabled',
    }), client)

    await hook.refetch()
    expect(hook.data.value).toBe('one')

    key.value = 'two'
    await nextTick()
    await hook.refetch()
    expect(hook.data.value).toBe('two')

    key.value = 'disabled'
    await nextTick()
    expect(hook.isPending.value).toBe(true)
    expect(calls).toBe(2)
  })

  it('deduplicates the same key and refetches active queries after invalidation', async () => {
    let calls = 0
    const createHook = () => useQuery(() => ({
      queryKey: ['test', 'shared'] as const,
      queryFn: async () => {
        calls += 1
        return calls
      },
    }), client)

    const first = createHook()
    await first.refetch()
    createHook()
    await nextTick()
    expect(calls).toBe(1)

    await client.invalidateQueries({ queryKey: ['test', 'shared'] })
    expect(calls).toBe(2)
  })
})

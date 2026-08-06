import { describe, expect, it } from 'vitest'

import { usePageTransitionLoading } from '@/platform/page-transition'

describe('page transition loading', () => {
  it('shows loading before navigation and clears it after completion', async () => {
    const transition = usePageTransitionLoading()
    const result = await transition.run(async () => 'done')

    expect(result).toBe('done')
    expect(transition.pending.value).toBe(false)
  })

  it('blocks duplicate transitions while the first action is pending', async () => {
    const transition = usePageTransitionLoading()
    let resolveAction: (() => void) | undefined
    const first = transition.run(() => new Promise<void>((resolve) => {
      resolveAction = resolve
    }))

    expect(transition.pending.value).toBe(true)
    await expect(transition.run(async () => 'duplicate')).resolves.toBeUndefined()

    await new Promise(resolve => setTimeout(resolve, 0))
    resolveAction?.()
    await first
    expect(transition.pending.value).toBe(false)
  })
})

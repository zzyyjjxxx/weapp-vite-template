import { readFileSync } from 'node:fs'
import { describe, expect, it, vi } from 'vitest'

import { guardProtectedPage, runProtectedAction } from '@/router/protected-page'

describe('direct protected page guard', () => {
  it('allows an active session without navigating', async () => {
    const redirect = vi.fn(async () => undefined)

    await expect(guardProtectedPage({
      ensureActiveSession: () => true,
    }, '/pages/home/index', redirect)).resolves.toBe(true)
    expect(redirect).not.toHaveBeenCalled()
  })

  it('replaces an unauthenticated direct launch with an encoded login return target', async () => {
    const redirect = vi.fn(async () => undefined)

    await expect(guardProtectedPage({
      ensureActiveSession: () => false,
    }, '/pages/land-demand/success', redirect)).resolves.toBe(false)
    expect(redirect).toHaveBeenCalledWith(
      '/pages/login/index?returnTo=%2Fpages%2Fland-demand%2Fsuccess',
    )
  })

  it('registers the shared guard and gates rendering on every protected page', () => {
    const pages = [
      ['src/pages/home/index.vue', '/pages/home/index'],
      ['src/pages/land-demand/index.vue', '/pages/land-demand/index'],
      ['src/pages/land-demand/success.vue', '/pages/land-demand/success'],
    ] as const

    for (const [file, path] of pages) {
      const source = readFileSync(file, 'utf8')
      expect(source).toContain(`useProtectedPage('${path}')`)
      expect(source).toContain('v-if="authorized"')
    }
  })

  it('does not invoke a sensitive action after the session expires', async () => {
    const action = vi.fn(async () => 'persisted')
    const redirect = vi.fn(async () => undefined)

    await expect(runProtectedAction(
      { ensureActiveSession: () => false },
      '/pages/land-demand/index',
      action,
      redirect,
    )).resolves.toBeUndefined()
    expect(action).not.toHaveBeenCalled()
    expect(redirect).toHaveBeenCalledOnce()
  })

  it('wires every sensitive land-demand action through the foreground guard', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source.match(/runProtectedAction/g)?.length).toBeGreaterThanOrEqual(4)
    expect(source).toMatch(/saveDraft[\s\S]*runProtectedAction/)
    expect(source).toMatch(/persistSubmission[\s\S]*runProtectedAction/)
    expect(source).toMatch(/requestVerification[\s\S]*runProtectedAction/)
    expect(source).toMatch(/submitVerificationCode[\s\S]*runProtectedAction/)
  })
})

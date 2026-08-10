import type { PageScrollAdapter } from '@/platform/page-scroll'

import { describe, expect, it, vi } from 'vitest'

import { scrollPageToField, scrollPageToTop } from '@/platform/page-scroll'

describe('page scroll adapter', () => {
  it('scrolls the current page to the top without animation', () => {
    const pageScrollTo = vi.fn().mockResolvedValue(undefined)

    scrollPageToTop({ pageScrollTo })

    expect(pageScrollTo).toHaveBeenCalledWith({
      scrollTop: 0,
      duration: 0,
    })
  })

  it('does not propagate host scroll errors', async () => {
    const pageScrollTo = vi.fn().mockRejectedValue(new Error('not ready'))

    expect(() => scrollPageToTop({ pageScrollTo })).not.toThrow()
    await Promise.resolve()
  })

  it('scrolls an invalid field below the page header', async () => {
    const pageScrollTo = vi.fn().mockResolvedValue(undefined)
    const pageScrollApi: PageScrollAdapter = { pageScrollTo }

    await scrollPageToField('#land-info-step >>> #area-field', pageScrollApi)

    expect(pageScrollTo).toHaveBeenCalledWith({
      selector: '#land-info-step >>> #area-field',
      offsetTop: -32,
      duration: 180,
    })
  })
})

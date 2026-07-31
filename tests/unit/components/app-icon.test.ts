import { existsSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

import { describe, expect, it } from 'vitest'
import { appIconNames, getAppIconSource } from '@/components/ui/app-icon/icons'

const projectRoot = fileURLToPath(new URL('../../../', import.meta.url))

describe('AppIcon registry', () => {
  it('keeps the initial Reicon subset available in both weights', () => {
    expect(appIconNames).toEqual(['home', 'user-circle', 'lock', 'list-check', 'login'])

    for (const name of appIconNames) {
      for (const weight of ['Outline', 'Filled'] as const) {
        const source = getAppIconSource(name, weight)
        expect(source).toMatch(/^\/assets\/icons\/reicon\/.+\.svg$/)
        expect(existsSync(`${projectRoot}/public${source}`)).toBe(true)
      }
    }
  })
})

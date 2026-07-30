import { readFileSync } from 'node:fs'

import { describe, expect, it } from 'vitest'

describe('router runtime lifecycle', () => {
  it('rebinds the router when a restarted mini-program app runs setup again', () => {
    const source = readFileSync('src/router/index.ts', 'utf8')

    expect(source).toContain('export function setupRouter(): RouterNavigation {\n  router = createRouter()')
    expect(source).toContain('return router ?? setupRouter()')
    expect(source).not.toMatch(/setupRouter\(\)[\s\S]*if \(router\)/)
  })
})

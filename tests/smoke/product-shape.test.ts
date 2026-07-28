import { readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'

const pkg = JSON.parse(readFileSync('package.json', 'utf8'))
const vite = readFileSync('vite.config.ts', 'utf8')

describe('land-demand product shape', () => {
  it('has no Hono server or order subpackage scripts', () => {
    expect(pkg.dependencies).not.toHaveProperty('hono')
    expect(pkg.scripts).not.toHaveProperty('dev:api')
    expect(pkg.scripts).not.toHaveProperty('typecheck:server')
    expect(vite).not.toContain('subpackages/order')
  })

  it('declares the product test commands', () => {
    expect(pkg.scripts).toHaveProperty('test:e2e')
    expect(pkg.scripts.verify).toContain('pnpm build')
  })
})

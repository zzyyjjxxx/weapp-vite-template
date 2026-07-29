import { existsSync, readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'

const pkg = JSON.parse(readFileSync('package.json', 'utf8'))
const vite = readFileSync('vite.config.ts', 'utf8')
const productDocs = [
  'docs/architecture.md',
  'docs/routing.md',
  'docs/http-client.md',
  'docs/query-state.md',
  'docs/ui-guidelines.md',
  'docs/testing.md',
  'docs/agent-workflow.md',
]

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

  it('has no unused HTTP environment scaffold', () => {
    expect(existsSync('.env.example')).toBe(false)
    expect(existsSync('src/shared/env.ts')).toBe(false)
  })

  it('documents only the land demand product', () => {
    for (const file of ['README.md', ...productDocs]) {
      const source = readFileSync(file, 'utf8')
      expect(source).toContain('用地需求')
      expect(source).not.toMatch(/订单取消|Hono 测试后端|demo order/i)
    }
  })

  it('documents the intentional local-draft repository exception', () => {
    const source = readFileSync('docs/http-client.md', 'utf8')

    expect(source).toContain('Store → Repository')
    expect(source).toContain('不经过 Service 或 Query')
    expect(source).toContain('持久化记录')
  })
})

import { readFileSync } from 'node:fs'

import { describe, expect, it } from 'vitest'

describe('product navigation source contract', () => {
  it('exposes stable login and home automation hooks', () => {
    expect(readFileSync('src/pages/login/index.vue', 'utf8'))
      .toContain('data-testid="login-submit"')
    expect(readFileSync('src/pages/home/index.vue', 'utf8'))
      .toContain('data-testid="land-demand-primary"')
  })
})

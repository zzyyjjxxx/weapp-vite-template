import { readFileSync } from 'node:fs'

import { describe, expect, it } from 'vitest'

describe('land-demand visual system', () => {
  it('keeps the reference-inspired hero and project-specific login content together', () => {
    const source = readFileSync('src/pages/login/index.vue', 'utf8')

    expect(source).toContain('/assets/land-planning-hero.webp')
    expect(source).toContain('企业用地需求在线填报服务')
    expect(source).toContain('demo / demo123')
    expect(source).not.toContain('立即注册')
    expect(source).not.toContain('忘记密码')
  })

  it('uses one shared elevated card language across the application', () => {
    const tokens = readFileSync('src/styles/tokens.scss', 'utf8')
    const utilities = readFileSync('src/styles/utilities.scss', 'utf8')

    expect(tokens).toContain('$gradient-primary')
    expect(tokens).toContain('$shadow-card')
    expect(utilities).toContain('.u-section-heading')
    expect(utilities).toContain('.step-card')
  })

  it('preserves the real five-step journey in the redesigned progress rail', () => {
    const source = readFileSync(
      'src/features/land-demand/components/wizard-progress.vue',
      'utf8',
    )

    expect(source).toContain('['
      + '\'基本信息\', \'用地需求\', \'投资项目\', \'融资及联系人\', \'确认提交\''
      + ']')
    expect(source).toContain('wizard-progress__connector')
  })

  it('keeps the planning illustration inside the home hero and the filling page compact', () => {
    const home = readFileSync('src/pages/home/index.vue', 'utf8')
    const shell = readFileSync('src/components/ui/page-shell/index.vue', 'utf8')

    expect(home).toContain('mode="aspectFill"')
    expect(home).toContain('left: 5%')
    expect(home).toContain('width: 108%')
    expect(shell).toContain('page-shell--compact')
  })
})

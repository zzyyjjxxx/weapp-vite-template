import { readFileSync } from 'node:fs'

import { describe, expect, it } from 'vitest'

const componentRoot = 'src/features/land-demand/components'
const stepFiles = [
  'basic-info-step.vue',
  'land-info-step.vue',
  'project-info-step.vue',
  'finance-contact-step.vue',
].map(file => `${componentRoot}/${file}`)

describe('land demand wizard component contract', () => {
  it('exposes stable field hooks through TDesign form controls', () => {
    const sources = stepFiles.map(file => readFileSync(file, 'utf8')).join('\n')

    for (const id of [
      'area',
      'building-area',
      'expect-park',
      'expect-time',
      'is-deploy',
      'deploy-park',
      'is-specialuse',
      'deploy-landtype',
      'deploy-height',
      'deploy-weight',
      'investment',
      'project-hydm',
      'keyindustry',
      'futureindustry',
      'pred-ys',
      'pred-tax',
      'pred-rdex',
      'pred-unitenergy',
      'projectdata',
      'is-financing',
      'financing-money',
      'financing-time',
      'contact',
      'office',
      'phone',
    ]) {
      expect(sources).toContain(`data-testid="${id}"`)
    }
    expect(sources).toMatch(/<t-(input|radio-group|checkbox-group|cascader|picker)/)
  })

  it('keeps height and weight outside special-use conditional markup', () => {
    const source = readFileSync(`${componentRoot}/land-info-step.vue`, 'utf8')

    expect(source).toMatch(/data-testid="deploy-height"/)
    expect(source).toMatch(/data-testid="deploy-weight"/)
    expect(source).not.toMatch(/v-if="[^"]*is_specialuse[^"]*"[\s\S]*data-testid="deploy-height"/)
  })

  it('reads component event details and emits partial patches without mutating props', () => {
    const sources = stepFiles.map(file => readFileSync(file, 'utf8')).join('\n')

    expect(sources).toContain('event.detail')
    expect(sources).toContain('emit(\'change\', {')
    expect(sources).not.toMatch(/props\.form\.\w+\s*=(?!=)/)
  })

  it('uses an explicit TDesign dialog for destructive clears', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).toContain('<t-dialog')
    expect(source).toContain('destructive-clear-dialog')
  })
})

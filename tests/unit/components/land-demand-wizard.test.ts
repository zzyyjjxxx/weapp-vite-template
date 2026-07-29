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

  it('consumes already-unwrapped event details and emits partial patches without mutating props', () => {
    const sources = stepFiles.map(file => readFileSync(file, 'utf8')).join('\n')

    expect(sources).toContain('readStringDetail')
    expect(sources).not.toContain('event.detail')
    expect(sources).toContain('emit(\'change\', {')
    expect(sources).not.toMatch(/props\.form\.\w+\s*=(?!=)/)
  })

  it('passes child component patches directly to the page controller', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).toContain('readPatchDetail<LandDemandForm>(detail)')
    expect(source).not.toContain('event.detail')
  })

  it('uses an explicit TDesign dialog for destructive clears', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).toContain('<t-dialog')
    expect(source).toContain('destructive-clear-dialog')
    expect(source).toContain('data-testid="destructive-clear-confirm"')
  })

  it('does not recreate a local draft after an explicit server save', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).toContain('store.markPersisted(record)')
    expect(source).not.toMatch(/store\.markPersisted\(record\)\s+store\.saveLocalDraft\(\)/)
  })

  it('exposes every Task 7 control used by the Task 9 runtime contract', () => {
    const sources = [
      ...stepFiles,
      `${componentRoot}/wizard-actions.vue`,
    ].map(file => readFileSync(file, 'utf8')).join('\n')

    for (const id of [
      'next-step',
      'save-draft',
      'area',
      'deploy-height',
      'is-specialuse-no',
      'is-financing-yes',
      'financing-money-error',
      'financing-time-error',
    ]) {
      expect(sources).toContain(`data-testid="${id}"`)
    }
  })

  it('uses a searchable national-industry cascader instead of a free input', () => {
    const source = readFileSync(`${componentRoot}/project-info-step.vue`, 'utf8')

    expect(source).toContain('<t-cascader')
    expect(source).toContain(':filterable="true"')
    expect(source).toContain('NATIONAL_INDUSTRY_OPTIONS')
    expect(source).toContain('getIndustryDisplay')
    expect(source).not.toMatch(/<t-input[^>]+data-testid="project-hydm"/)
  })

  it('uses the confirmed investment and unit-energy labels', () => {
    const source = readFileSync(`${componentRoot}/project-info-step.vue`, 'utf8')

    expect(source).toContain('label="固定资产投资额（万元）"')
    expect(source).toContain('label="项目单位能耗增加值（万元/吨标煤）"')
    expect(source).not.toContain('label="项目总投资（万元）"')
    expect(source).not.toContain('label="预计单位能耗"')
  })

  it('loads local drafts through the Store boundary instead of the page repository', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).toContain('store.initializeFromLocalDraft')
    expect(source).toContain('const original = query.data.value')
    expect(source).not.toContain('@/features/land-demand/repository')
    expect(source).not.toContain('getLandDemandRepository')
    expect(source).not.toContain('originalRecord')
  })
})

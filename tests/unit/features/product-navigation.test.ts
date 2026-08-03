import { readFileSync } from 'node:fs'

import { describe, expect, it } from 'vitest'

describe('product navigation source contract', () => {
  it('exposes stable login and home automation hooks', () => {
    expect(readFileSync('src/pages/login/index.vue', 'utf8'))
      .toContain('data-testid="login-submit"')
    expect(readFileSync('src/pages/home/index.vue', 'utf8'))
      .toContain('data-testid="land-demand-primary"')
  })

  it('keeps all authenticated enterprise identity fields read-only', () => {
    const source = readFileSync('src/features/land-demand/components/basic-info-step.vue', 'utf8')

    expect(source.match(/<t-input[^>]+readonly/g)).toHaveLength(4)
    expect(source).not.toContain('@change=')
    expect(source).not.toContain('emit(\'change\'')
  })

  it('offers distinct submitted detail and edit actions with typed view navigation', () => {
    const home = readFileSync('src/pages/home/index.vue', 'utf8')
    const form = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(home).toContain('data-testid="land-demand-view"')
    expect(home).toContain('data-testid="land-demand-edit"')
    expect(home).toContain(['home-step-', '$', '{number}'].join(''))
    expect(home).toContain('selectStep(number)')
    expect(home).toContain('number === (selectedStep || currentProgressStep)')
    expect(home).toContain('step: selectedStep.value')
    expect(home).toContain('{ mode: \'view\' }')
    expect(form).toContain('parseLandDemandMode(query?.mode)')
    expect(form).toContain('parseLandDemandStep(query?.step)')
    expect(form).toContain('store.goToStep(requestedStep.value)')
    expect(form).toContain('!routeReady.value')
    expect(form).toContain('data-testid="detail-back-home"')
    expect(form).toContain('data-testid="detail-edit"')
    expect(form).toContain(':readonly="viewOnly"')
  })

  it('loads a submitted Query record before rendering the success state', () => {
    const source = readFileSync('src/pages/land-demand/success.vue', 'utf8')

    expect(source).toContain('useLandDemandQuery(creditcode)')
    expect(source).toContain('record.value?.landusedemand === \'1\'')
    expect(source).toContain('recordBusinessName = computed')
    expect(source).toContain('recordUpdateTime = computed')
    expect(source).toContain('{{ recordBusinessName }}')
    expect(source).toContain('{{ recordUpdateTime }}')
    expect(source).toContain('data-testid="success-view-detail"')
    expect(source).toContain('{ mode: \'view\' }')
  })
})

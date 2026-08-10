import { readFileSync } from 'node:fs'

import { describe, expect, it } from 'vitest'

describe('product navigation source contract', () => {
  it('exposes stable login and home automation hooks', () => {
    const login = readFileSync('src/pages/login/index.vue', 'utf8')
    const home = readFileSync('src/pages/home/index.vue', 'utf8')

    expect(login).toContain('data-testid="login-submit"')
    expect(login).toContain('text="正在登录"')
    expect(login).toContain('placeholder="请输入统一社会信用代码"')
    expect(login).toContain(':status="usernameError ? \'error\' : \'default\'"')
    expect(login).toContain(':tips="usernameError"')
    expect(login).toContain(':status="passwordError ? \'error\' : \'default\'"')
    expect(login).toContain(':tips="passwordError"')
    expect(login).not.toContain('demo / demo123')
    expect(login).not.toContain(':loading="isPending || transitioning"')
    expect(home).toContain('data-testid="land-demand-primary"')
    expect(home).not.toContain(':loading="landDemandQuery.isPending || transitioning"')
    expect(home).toContain('usePageTransitionLoading')
    expect(home).toContain('text="正在加载"')
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
    expect(home).toContain('number === (selectedStep || resumeStep)')
    expect(home).toContain('landDemandStore.progressStep.value')
    expect(home).toContain('lastSubmittedAt')
    expect(home).toContain('formatDateTime')
    expect(home).toContain('data-testid="land-demand-last-submitted-at"')
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
    expect(source).toContain('formatDateTime(record.value?.updatetime)')
    expect(source).toContain('{{ recordBusinessName }}')
    expect(source).toContain('{{ recordUpdateTime }}')
    expect(source).toContain('data-testid="success-view-detail"')
    expect(source).toContain('{ mode: \'view\' }')
    expect(source).toContain('text="正在加载"')
  })
})

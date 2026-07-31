import { readFileSync } from 'node:fs'

import { describe, expect, it } from 'vitest'

describe('mini-program runtime E2E contract', () => {
  it('serializes one opened WeChat DevTools project with a bounded timeout', () => {
    const config = readFileSync('playwright.config.ts', 'utf8')
    const fixture = readFileSync('e2e/fixtures/mini-program.ts', 'utf8')

    expect(config).toContain('fullyParallel: false')
    expect(config).toContain('workers: 1')
    expect(config).toContain('timeout: 60_000')
    expect(config).toContain('name: \'weapp\'')
    expect(fixture).toContain('resolveProjectAutomatorPort(PROJECT_PATH)')
    expect(fixture).toContain('wsEndpoint: AUTOMATOR_ENDPOINT')
    expect(fixture).toContain('timeout: 90_000')
    expect(fixture).toContain('scope: \'worker\'')
    expect(fixture).toContain('activeMiniProgram.disconnect()')
    expect(fixture).not.toContain('quitWechatIde()')
    expect(fixture).toContain('message.includes(\'[Component] property\')')
    expect(fixture).toContain('Runtime component property warnings:')
    expect(fixture).toContain('message.includes(\'[mutation.failed]\')')
    expect(fixture).toContain('Runtime mutation failures:')
  })

  it('covers every required land-demand runtime scenario and hook', () => {
    const spec = readFileSync('e2e/land-demand.spec.ts', 'utf8')

    for (const title of [
      'logs in, saves a draft and restores it',
      'keeps height while changing other-land acceptance',
      'requires financing details only when financing is 有',
      'submits with the mock code and reopens the existing record',
      'keeps only Ningbo after selecting a district and then the whole city',
      'restores the selected national industry leaf',
      'resets direction when the industry track changes',
      'restores an authenticated session after a cold relaunch',
      'modifies and resaves an existing submitted record',
    ]) {
      expect(spec).toContain(`test('${title}'`)
    }

    for (const id of [
      'area',
      'deploy-height',
      'is-specialuse-no',
      'is-financing-yes',
      'financing-money-error',
      'financing-time-error',
      'mock-code',
      'verification-code',
      'verification-submit',
      'deploy-park',
      'deploy-park-selection',
      'keyindustry',
      'futureindustry',
      'land-demand-edit',
      'save-draft',
    ]) {
      expect(spec).toContain(`getByTestId('${id}')`)
    }

    expect(spec).toContain('screenshot(\'.tmp/e2e-login.png\')')
    expect(spec).toContain('screenshot(\'.tmp/e2e-review.png\')')
    expect(spec).toContain('project_hydm: \'1811\'')
    expect(spec).toContain('getByTestId(\'project-hydm\').tap()')
    expect(spec.match(/getByTestId\('review-submit'\)\.tap\(\)/g)).toHaveLength(3)
    expect(spec).toContain('miniProgram.restart(\'/pages/home/index\')')
    expect(spec).not.toContain(
      'restores an authenticated session after a cold relaunch\', async ({ miniProgram }) => {\n    await miniProgram.relaunch',
    )
  })
})

import { expect } from '@playwright/test'

import { test } from './fixtures/mini-program'

const ROBOT_DIRECTION = '具身大模型（大脑与小脑）'

test.describe.serial('企业用地需求填报', () => {
  test('logs in, saves a draft and restores it', async ({ miniProgram }) => {
    await test.step('reset the runtime to login', async () => {
      await test.step('relaunch the login page', async () => {
        await miniProgram.relaunch('/pages/login/index')
      })
      await test.step('capture the login page', async () => {
        await miniProgram.screenshot('.tmp/e2e-login.png')
      })
      await test.step('clear persisted runtime state', async () => {
        await miniProgram.clearStorage()
      })
    })
    await test.step('authenticate', async () => {
      await miniProgram.getByTestId('username').fill('demo')
      await miniProgram.getByTestId('password').fill('demo123')
      await miniProgram.getByTestId('login-submit').tap()
      await miniProgram.expectPath('/pages/home/index')
    })
    await test.step('save a land-demand draft', async () => {
      await miniProgram.getByTestId('land-demand-primary').tap()
      await miniProgram.getByTestId('next-step').tap()
      await miniProgram.getByTestId('area').fill('30')
      await miniProgram.getByTestId('save-draft').tap()
      expect(await miniProgram.getByTestId('save-feedback').text()).toContain('已暂存')
    })
    await test.step('restore the draft', async () => {
      await miniProgram.relaunch('/pages/home/index')
      await miniProgram.getByTestId('land-demand-primary').tap()
      await miniProgram.getByTestId('next-step').tap()
      expect(await miniProgram.getByTestId('area').text()).toContain('30')
    })
  })

  test('keeps height while changing other-land acceptance', async ({ miniProgram }) => {
    await miniProgram.getByTestId('deploy-height').fill('8')
    await miniProgram.getByTestId('is-specialuse-no').tap()
    expect(await miniProgram.getByTestId('deploy-height').text()).toContain('8')
  })

  test('keeps only Ningbo after selecting a district and then the whole city', async ({ miniProgram }) => {
    await miniProgram.getByTestId('building-area').fill('1000')
    await miniProgram.getByTestId('expect-park').fill('330203')
    await miniProgram.getByTestId('expect-time').fill('2027-06')
    await miniProgram.getByTestId('is-deploy').fill('是')
    await miniProgram.getByTestId('deploy-park').fill('["330203"]')
    await miniProgram.getByTestId('deploy-park').fill('["330203","330200"]')

    expect(await miniProgram.getByTestId('deploy-park-selection').text()).toBe('宁波市')
  })

  test('restores the selected national industry leaf', async ({ miniProgram }) => {
    await miniProgram.getByTestId('next-step').tap()
    await miniProgram.getByTestId('investment').expectVisible()
    await miniProgram.getByTestId('project-hydm').expectVisible()
    await miniProgram.patchForm({
      investment: '5000',
      // 1811 is selected through its parent 181 in the cascader hierarchy.
      project_hydm: '1811',
      keyindustry: '智能机器人',
      futureindustry: ROBOT_DIRECTION,
      pred_ys: '10000',
      pred_tax: '800',
      pred_rdex: '500',
      pred_unitenergy: '20',
      projectdata: '建设智能机器人生产线',
    })
    await miniProgram.saveDraft()
    await miniProgram.relaunch('/pages/home/index')
    await miniProgram.getByTestId('land-demand-primary').tap()
    await miniProgram.getByTestId('next-step').tap()
    await miniProgram.getByTestId('next-step').tap()

    expect(await miniProgram.getByTestId('project-hydm').text()).toContain('运动机织服装制造（1811）')
    await miniProgram.getByTestId('project-hydm').tap()
    await miniProgram.relaunch('/pages/home/index')
    await miniProgram.getByTestId('land-demand-primary').tap()
    await miniProgram.getByTestId('next-step').tap()
    await miniProgram.getByTestId('next-step').tap()
  })

  test('resets direction when the industry track changes', async ({ miniProgram }) => {
    await miniProgram.goToStep(3)
    await miniProgram.getByTestId('keyindustry').fill('生物医药')
    await miniProgram.getByTestId('destructive-clear-confirm').tap()

    expect(await miniProgram.getByTestId('futureindustry').text()).toBe('')
  })

  test('requires financing details only when financing is 有', async ({ miniProgram }) => {
    await miniProgram.patchForm({ futureindustry: '其他' })
    await miniProgram.goToStep(4)
    await miniProgram.getByTestId('is-financing-yes').expectVisible()
    await miniProgram.patchForm({ is_financing: '有' })
    await miniProgram.goToStep(5)
    await miniProgram.getByTestId('review-submit').tap()
    await miniProgram.getByTestId('financing-money-error').expectVisible()
    await miniProgram.getByTestId('financing-time-error').expectVisible()

    await miniProgram.patchForm({
      financing_money: '2000',
      financing_time: '2027-03',
    })
    await miniProgram.getByTestId('next-step').tap()
  })

  test('submits with the mock code and reopens the existing record', async ({ miniProgram }) => {
    await miniProgram.screenshot('.tmp/e2e-review.png')
    await miniProgram.getByTestId('review-accept').tap()
    await miniProgram.getByTestId('review-submit').tap()
    expect(await miniProgram.getByTestId('mock-code').text()).toContain('123456')
    await miniProgram.screenshot('.tmp/e2e-verification.png')
    await miniProgram.getByTestId('review-submit').tap()
    expect(await miniProgram.getByTestId('mock-code').text()).toContain('123456')
    await miniProgram.getByTestId('verification-code').fill('123456')
    await miniProgram.getByTestId('verification-submit').tap()
    await miniProgram.expectPath('/pages/land-demand/success')
    await miniProgram.getByTestId('back-home').tap()
    expect(await miniProgram.getByTestId('land-demand-status').text()).toContain('已提交')
    await miniProgram.getByTestId('land-demand-view').tap()
    await miniProgram.getByTestId('detail-back-home').expectVisible()
    await miniProgram.getByTestId('detail-back-home').tap()
  })

  test('modifies and resaves an existing submitted record', async ({ miniProgram }) => {
    await miniProgram.getByTestId('land-demand-edit').tap()
    await miniProgram.getByTestId('next-step').tap()
    await miniProgram.patchForm({ area: '31' })
    await miniProgram.saveDraft()
    await miniProgram.backHome()
    expect(await miniProgram.getByTestId('land-demand-status').text()).toContain('草稿')
    await miniProgram.getByTestId('land-demand-primary').tap()
    await miniProgram.getByTestId('next-step').tap()
    expect(await miniProgram.getByTestId('area').text()).toContain('31')
  })

  test('restores an authenticated session after a cold relaunch', async ({ miniProgram }) => {
    await miniProgram.restart('/pages/home/index')
    await miniProgram.expectPath('/pages/home/index')
    expect(await miniProgram.getByTestId('land-demand-status').text()).toContain('草稿')
  })
})

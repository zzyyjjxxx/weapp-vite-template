import { expect } from '@playwright/test'

import { test } from './fixtures/mini-program'

const ROBOT_DIRECTION = '具身大模型（大脑与小脑）'

test.describe.serial('企业用地需求填报', () => {
  test('logs in, saves a draft and restores it', async ({ miniProgram }) => {
    await miniProgram.relaunch('/pages/login/index')
    await miniProgram.clearStorage()
    await miniProgram.restart('/pages/login/index')
    await miniProgram.screenshot('.tmp/e2e-login.png')
    await miniProgram.getByTestId('username').fill('demo')
    await miniProgram.getByTestId('password').fill('demo123')
    await miniProgram.getByTestId('login-submit').tap()
    await miniProgram.expectPath('/pages/home/index')
    await miniProgram.getByTestId('land-demand-primary').tap()
    await miniProgram.getByTestId('next-step').tap()
    await miniProgram.getByTestId('area').fill('30')
    await miniProgram.getByTestId('save-draft').tap()
    await miniProgram.relaunch('/pages/home/index')
    await miniProgram.getByTestId('land-demand-primary').tap()
    await miniProgram.getByTestId('next-step').tap()
    await expect.poll(() => miniProgram.getByTestId('area').text()).toContain('30')
  })

  test('restores an authenticated session after a cold relaunch', async ({ miniProgram }) => {
    await miniProgram.restart('/pages/home/index')
    await miniProgram.expectPath('/pages/home/index')
    await expect.poll(() => miniProgram.getByTestId('land-demand-status').text()).toContain('草稿')
    await miniProgram.getByTestId('land-demand-primary').tap()
  })

  test('keeps height while changing other-land acceptance', async ({ miniProgram }) => {
    await miniProgram.getByTestId('deploy-height').fill('8')
    await miniProgram.getByTestId('is-specialuse-no').tap()
    await expect.poll(() => miniProgram.getByTestId('deploy-height').text()).toContain('8')
  })

  test('keeps only Ningbo after selecting a district and then the whole city', async ({ miniProgram }) => {
    await miniProgram.getByTestId('building-area').fill('1000')
    await miniProgram.getByTestId('expect-park').fill('330203')
    await miniProgram.getByTestId('expect-time').fill('2027-06')
    await miniProgram.getByTestId('is-deploy').fill('是')
    await miniProgram.getByTestId('deploy-park').fill('["330203"]')
    await miniProgram.getByTestId('deploy-park').fill('["330203","330200"]')

    await expect.poll(() => miniProgram.getByTestId('deploy-park-selection').text()).toBe('宁波市')
  })

  test('restores the selected national industry leaf', async ({ miniProgram }) => {
    await miniProgram.getByTestId('next-step').tap()
    await miniProgram.getByTestId('investment').fill('5000')
    await miniProgram.getByTestId('project-hydm').tap()
    // 1811 is selected through its parent 181 in the cascader hierarchy.
    await miniProgram.getByTestId('project-hydm-cascader').fill('1811')
    await miniProgram.getByTestId('keyindustry').fill('智能机器人')
    await miniProgram.getByTestId('futureindustry').fill(ROBOT_DIRECTION)
    await miniProgram.getByTestId('pred-ys').fill('10000')
    await miniProgram.getByTestId('pred-tax').fill('800')
    await miniProgram.getByTestId('pred-rdex').fill('500')
    await miniProgram.getByTestId('pred-unitenergy').fill('20')
    await miniProgram.getByTestId('projectdata').fill('建设智能机器人生产线')
    await miniProgram.getByTestId('save-draft').tap()
    await miniProgram.relaunch('/pages/home/index')
    await miniProgram.getByTestId('land-demand-primary').tap()
    await miniProgram.getByTestId('next-step').tap()
    await miniProgram.getByTestId('next-step').tap()

    await expect.poll(() => miniProgram.getByTestId('project-hydm').text()).toContain('运动机织服装制造（1811）')
  })

  test('resets direction when the industry track changes', async ({ miniProgram }) => {
    await miniProgram.getByTestId('keyindustry').fill('生物医药')
    await miniProgram.getByTestId('destructive-clear-confirm').tap()

    await expect.poll(() => miniProgram.getByTestId('futureindustry').text()).toBe('')
  })

  test('requires financing details only when financing is 有', async ({ miniProgram }) => {
    await miniProgram.getByTestId('futureindustry').fill('其他')
    await miniProgram.getByTestId('next-step').tap()
    await miniProgram.getByTestId('is-financing-yes').tap()
    await miniProgram.getByTestId('next-step').tap()
    await miniProgram.getByTestId('financing-money-error').expectVisible()
    await miniProgram.getByTestId('financing-time-error').expectVisible()

    await miniProgram.getByTestId('financing-money').fill('2000')
    await miniProgram.getByTestId('financing-time').fill('2027-03')
    await miniProgram.getByTestId('next-step').tap()
  })

  test('submits with the mock code and reopens the existing record', async ({ miniProgram }) => {
    await miniProgram.screenshot('.tmp/e2e-review.png')
    await miniProgram.getByTestId('review-accept').tap()
    await miniProgram.getByTestId('review-submit').tap()
    await miniProgram.getByTestId('verification-resend').expectVisible()
    await miniProgram.getByTestId('verification-code').fill('123456')
    await miniProgram.getByTestId('verification-submit').tap()
    await miniProgram.expectPath('/pages/land-demand/success')
    await miniProgram.getByTestId('back-home').tap()
    await expect.poll(() => miniProgram.getByTestId('land-demand-status').text()).toContain('已提交')
    await miniProgram.getByTestId('land-demand-view').tap()
    await miniProgram.getByTestId('detail-back-home').expectVisible()
    await miniProgram.getByTestId('detail-back-home').tap()
  })

  test('modifies and resaves an existing submitted record', async ({ miniProgram }) => {
    await miniProgram.getByTestId('land-demand-edit').tap()
    await miniProgram.getByTestId('next-step').tap()
    await miniProgram.getByTestId('area').fill('31')
    await miniProgram.getByTestId('save-draft').tap()
    await miniProgram.relaunch('/pages/home/index')
    await expect.poll(() => miniProgram.getByTestId('land-demand-status').text()).toContain('草稿')
    await miniProgram.getByTestId('land-demand-primary').tap()
    await miniProgram.getByTestId('next-step').tap()
    await expect.poll(() => miniProgram.getByTestId('area').text()).toContain('31')
  })
})

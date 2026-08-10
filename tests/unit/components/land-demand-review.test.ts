import type { LandDemandForm } from '@/features/land-demand/models'

import { readFileSync } from 'node:fs'

import { describe, expect, it } from 'vitest'
import { buildReviewGroups } from '@/features/land-demand/review'

const validForm: LandDemandForm = {
  county: '鄞州区',
  region: '首南街道',
  businessname: '宁波示范智造有限公司',
  creditcode: '91330200MA2DEMO001',
  area: '30',
  building_area: '10000',
  expect_park: '330203',
  expect_time: '2027-06',
  is_deploy: '是',
  deploy_park: ['330205'],
  is_specialuse: '是',
  deploy_landtype: '小微园',
  deploy_height: '8',
  deploy_weight: '2',
  investment: '1000',
  project_hydm: '1811',
  keyindustry: '智能机器人',
  futureindustry: '具身大模型（大脑与小脑）',
  pred_ys: '2000',
  pred_tax: '100',
  pred_rdex: '200',
  pred_unitenergy: '3',
  projectdata: '建设智能机器人生产线',
  is_financing: '有',
  financing_money: '500',
  financing_time: '2027-03',
  contact: '张示例',
  office: '法定代表人',
  phone: '13800000000',
}

describe('land demand review', () => {
  it('mirrors the four form steps and renders dictionary values', () => {
    const groups = buildReviewGroups(validForm)
    const items = groups.flatMap(group => group.items)

    expect(groups.map(group => group.step)).toEqual([1, 2, 3, 4])
    expect(items.find(item => item.field === 'project_hydm')?.value)
      .toBe('运动机织服装制造（1811）')
    expect(items.find(item => item.field === 'expect_park')?.value).toBe('海曙区')
    expect(items.find(item => item.field === 'deploy_park')?.value).toBe('江北区')
  })

  it('suppresses conditionally irrelevant values but always keeps height and weight', () => {
    const items = buildReviewGroups({
      ...validForm,
      is_deploy: '否',
      deploy_park: [],
      is_specialuse: '否',
      deploy_landtype: '',
      is_financing: '没有',
      financing_money: '',
      financing_time: '',
    }).flatMap(group => group.items)

    expect(items.some(item => item.field === 'deploy_park')).toBe(false)
    expect(items.some(item => item.field === 'deploy_landtype')).toBe(false)
    expect(items.some(item => item.field === 'financing_money')).toBe(false)
    expect(items.some(item => item.field === 'financing_time')).toBe(false)
    expect(items.some(item => item.field === 'deploy_height')).toBe(true)
    expect(items.some(item => item.field === 'deploy_weight')).toBe(true)
  })

  it('exposes the review and verification runtime hooks', () => {
    const source = [
      'src/features/land-demand/components/review-step.vue',
      'src/features/land-demand/components/verification-dialog.vue',
      'src/pages/land-demand/success.vue',
    ].map(file => readFileSync(file, 'utf8')).join('\n')

    for (const id of [
      'review-accept',
      'review-submit',
      'verification-code',
      'verification-resend',
      'submit-success',
      'success-back-home',
      'back-home',
    ]) {
      expect(source).toContain(`data-testid="${id}"`)
    }
    expect(source).toContain('emit(\'edit\', group.step)')
  })

  it('wires submission through mutations, Store cleanup, and typed navigation', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).toContain('createSubmitController')
    expect(source).toContain('useSendVerificationCodeMutation')
    expect(source).toContain('useVerifyVerificationCodeMutation')
    expect(source).toContain('store.markPersisted(record)')
    expect(source).toContain('replace(\'/pages/land-demand/success\')')
    expect(source).toContain('const verificationVisible = ref(false)')
    expect(source).toContain('existingChallenge: challenge.value')
    expect(source).toContain('forceResend')
    expect(source).toContain('@resend="resendVerification"')
    expect(source).not.toMatch(/wx\.(?:request|navigateTo|redirectTo|reLaunch)/)
    expect(source).not.toContain('getLandDemandRepository')
  })

  it('keeps verification controls locked while submission is pending', () => {
    const source = readFileSync(
      'src/features/land-demand/components/verification-dialog.vue',
      'utf8',
    )

    expect(source).toContain(':disabled="props.loading"')
    expect(source).toContain('if (!props.loading)')
    expect(source).toContain('placeholder="请输入验证码"')
    expect(source).not.toContain('六位验证码')
    expect(source).not.toContain('Mock 测试验证码')
    expect(source).toContain('retryCountdown.value}秒')
    expect(source).not.toContain('秒后重新发送')
    expect(source).toContain('重新发送')
    expect(source).not.toContain('重新发送验证码')
    expect(source).toContain('t-class="verification-dialog__input"')
    expect(source).toContain('t-class-input="verification-dialog__input-control"')
    expect(source).toContain('t-class="verification-dialog__resend-button"')
    expect(source).toContain('请输入验证码')
    expect(source).toContain('已发送至')
    expect(source).not.toContain('验证码已发送至')
    expect(source).toContain('margin-top: $space-2')
    expect(source).not.toContain('verification-dialog__resend-hint')
    expect(source).not.toContain('秒后可重新发送')
    expect(source).toContain('--td-input-vertical-padding: 8rpx 32rpx')
    expect(source).toContain('padding-right: 96rpx')
    expect(source).toContain('width: 88rpx')
    expect(source).toContain('size="extra-small"')
    expect(source).toContain('setInterval(updateCountdown, 1000)')
    expect(source).toContain('button-layout="horizontal"')
    expect(source).toContain('cancel-btn="取消"')
    expect(source).toContain('confirm-btn="提交"')
    expect(source).toContain('if (!submitDisabled.value)')
    expect(source).toContain('@cancel="close"')
    expect(source).toContain('@confirm="confirm"')
    expect(source).not.toContain(':cancel-btn="cancelButton"')
    expect(source).not.toContain(':confirm-btn="confirmButton"')
    expect(source).not.toContain('slot="cancel-btn"')
    expect(source).not.toContain('slot="confirm-btn"')
  })
})

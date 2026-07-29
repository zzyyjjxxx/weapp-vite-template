import type { LandDemandForm, LandDemandRecord } from '@/features/land-demand/models'
import type { VerificationChallenge } from '@/features/land-demand/repository'

import { describe, expect, it, vi } from 'vitest'
import { createSubmitController } from '@/features/land-demand/submit'

const validForm: LandDemandForm = {
  county: '鄞州区',
  region: '首南街道',
  businessname: '宁波示范智造有限公司',
  creditcode: '91330200MA2DEMO001',
  area: '30',
  building_area: '10000',
  expect_park: '330203',
  expect_time: '2027-06',
  is_deploy: '否',
  deploy_park: [],
  is_specialuse: '否',
  deploy_landtype: '',
  deploy_height: '',
  deploy_weight: '',
  investment: '1000',
  project_hydm: '1811',
  keyindustry: '智能机器人',
  futureindustry: '具身大模型（大脑与小脑）',
  pred_ys: '2000',
  pred_tax: '100',
  pred_rdex: '200',
  pred_unitenergy: '3',
  projectdata: '建设智能机器人生产线',
  is_financing: '没有',
  financing_money: '',
  financing_time: '',
  contact: '张示例',
  office: '法定代表人',
  phone: '13800000000',
}

const challenge: VerificationChallenge = {
  phone: validForm.phone,
  expiresAt: 301_000,
  retryAt: 61_000,
  mockCode: '123456',
}

const submittedRecord: LandDemandRecord = {
  ...validForm,
  deploy_park: '',
  landusedemand: '1',
  updatetime: '2026-07-29T00:00:00.000Z',
  updateuser: 'demo',
}

describe('land demand submit controller', () => {
  it('does not request a code before full form validation passes', async () => {
    const sendCode = vi.fn()
    const controller = createSubmitController({
      sendCode,
      verifyCode: vi.fn(),
      persist: vi.fn(),
    })

    const result = await controller.requestCode({
      ...validForm,
      is_financing: '有',
      financing_money: '',
      financing_time: '',
    }, true)

    expect(result.errors).toEqual(expect.arrayContaining([
      expect.objectContaining({ field: 'financing_money', step: 4 }),
      expect.objectContaining({ field: 'financing_time', step: 4 }),
    ]))
    expect(sendCode).not.toHaveBeenCalled()
  })

  it('requires the information promise before requesting a code', async () => {
    const sendCode = vi.fn()
    const controller = createSubmitController({
      sendCode,
      verifyCode: vi.fn(),
      persist: vi.fn(),
    })

    const result = await controller.requestCode(validForm, false)

    expect(result.errors).toEqual([])
    expect(result.acceptanceError).toBe('请阅读并同意信息真实性承诺')
    expect(sendCode).not.toHaveBeenCalled()
  })

  it('requests a challenge only after validation and promise acceptance', async () => {
    const sendCode = vi.fn(async () => challenge)
    const controller = createSubmitController({
      sendCode,
      verifyCode: vi.fn(),
      persist: vi.fn(),
    })

    await expect(controller.requestCode(validForm, true)).resolves.toEqual({
      errors: [],
      challenge,
    })
    expect(sendCode).toHaveBeenCalledOnce()
    expect(sendCode).toHaveBeenCalledWith(validForm.phone)
  })

  it('verifies once before persisting status 1', async () => {
    const events: string[] = []
    const controller = createSubmitController({
      sendCode: async () => challenge,
      verifyCode: async () => { events.push('verify') },
      persist: async () => {
        events.push('persist')
        return submittedRecord
      },
    })

    await expect(controller.submitCode(validForm.phone, '123456')).resolves.toEqual(submittedRecord)
    expect(events).toEqual(['verify', 'persist'])
  })

  it('does not persist when verification fails', async () => {
    const persist = vi.fn()
    const controller = createSubmitController({
      sendCode: async () => challenge,
      verifyCode: async () => { throw new Error('验证码错误') },
      persist,
    })

    await expect(controller.submitCode(validForm.phone, '000000')).rejects.toThrow('验证码错误')
    expect(persist).not.toHaveBeenCalled()
  })

  it('does not consume a verified code twice when persistence is retried', async () => {
    const verifyCode = vi.fn(async () => undefined)
    const persist = vi.fn()
      .mockRejectedValueOnce(new Error('暂时无法保存'))
      .mockResolvedValueOnce(submittedRecord)
    const controller = createSubmitController({
      sendCode: async () => challenge,
      verifyCode,
      persist,
    })

    await expect(controller.submitCode(validForm.phone, '123456')).rejects.toThrow('暂时无法保存')
    await expect(controller.submitCode(validForm.phone, '123456')).resolves.toEqual(submittedRecord)
    expect(verifyCode).toHaveBeenCalledOnce()
    expect(persist).toHaveBeenCalledTimes(2)
  })
})

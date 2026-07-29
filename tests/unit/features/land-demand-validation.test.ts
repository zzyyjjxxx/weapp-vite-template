import type { LandDemandForm } from '@/features/land-demand/models'

import { describe, expect, it } from 'vitest'
import { validateDraft, validateSubmission } from '@/features/land-demand/validation'

const validForm: LandDemandForm = {
  county: '海曙区',
  region: '集士港镇',
  businessname: '示例企业',
  creditcode: '91330200MA2DEMO001',
  area: '30',
  building_area: '10000',
  expect_park: '330203',
  expect_time: '2027-06',
  is_deploy: '是',
  deploy_park: ['330203'],
  is_specialuse: '是',
  deploy_landtype: '小微园',
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
  projectdata: '项目建设内容',
  is_financing: '有',
  financing_money: '100',
  financing_time: '2027-06',
  contact: '张三',
  office: '总经理',
  phone: '13800138000',
}

describe('land demand validation', () => {
  it('validates only populated values in a draft', () => {
    expect(validateDraft({ ...validForm, area: '' })).toEqual([])
    expect(validateDraft({ ...validForm, area: '-1' })[0]?.field).toBe('area')
    expect(validateDraft({ ...validForm, pred_tax: '1.123' })[0]?.field).toBe('pred_tax')
  })

  it('enforces backend integer precision while keeping two UI decimals', () => {
    for (const field of [
      'investment',
      'financing_money',
      'pred_ys',
      'pred_tax',
      'pred_rdex',
      'pred_unitenergy',
    ] as const) {
      expect(validateDraft({ ...validForm, [field]: '99999999999999.99' }))
        .not
        .toEqual(expect.arrayContaining([expect.objectContaining({ field })]))
      expect(validateDraft({ ...validForm, [field]: '100000000000000' }))
        .toEqual(expect.arrayContaining([expect.objectContaining({ field })]))
    }

    for (const field of ['building_area', 'deploy_height', 'deploy_weight'] as const) {
      expect(validateDraft({ ...validForm, [field]: '99999999.99' }))
        .not
        .toEqual(expect.arrayContaining([expect.objectContaining({ field })]))
      expect(validateDraft({ ...validForm, [field]: '100000000' }))
        .toEqual(expect.arrayContaining([expect.objectContaining({ field })]))
    }

    expect(validateDraft({ ...validForm, investment: '' })).toEqual([])
    expect(validateDraft({ ...validForm, investment: '0' })).toEqual([])
  })

  it('makes all four project metrics required for submission', () => {
    const errors = validateSubmission({ ...validForm, pred_rdex: '', pred_unitenergy: '' })

    expect(errors.map(error => error.field)).toEqual(expect.arrayContaining(['pred_rdex', 'pred_unitenergy']))
  })

  it('requires the remaining required values and accepts unlimited project content', () => {
    expect(validateSubmission({ ...validForm, investment: '' }).some(error => error.field === 'investment')).toBe(true)
    expect(validateSubmission({ ...validForm, phone: '123' }).some(error => error.field === 'phone')).toBe(true)
    expect(validateSubmission({ ...validForm, projectdata: '项'.repeat(2_000) }).some(error => error.field === 'projectdata')).toBe(false)
  })

  it('requires conditional fields only when their choices make them visible', () => {
    expect(validateSubmission({ ...validForm, is_deploy: '是', deploy_park: [] })
      .some(error => error.field === 'deploy_park')).toBe(true)
    expect(validateSubmission({ ...validForm, is_specialuse: '是', deploy_landtype: '' })
      .some(error => error.field === 'deploy_landtype')).toBe(true)
    expect(validateSubmission({ ...validForm, is_financing: '有', financing_money: '', financing_time: '' })
      .map(error => error.field)).toEqual(expect.arrayContaining(['financing_money', 'financing_time']))
  })
})

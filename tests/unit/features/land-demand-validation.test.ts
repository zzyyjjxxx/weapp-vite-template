import type { LandDemandForm } from '@/features/land-demand/models'

import { describe, expect, it } from 'vitest'
import {
  normalizeFieldErrorMessage,
  resolveProgressStep,
  validateDraft,
  validateStep,
  validateSubmission,
} from '@/features/land-demand/validation'

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
  contact: '张三',
  office: '总经理',
  phone: '13800138000',
}

describe('land demand validation', () => {
  it('normalizes a generic runtime error to the required-field message', () => {
    expect(normalizeFieldErrorMessage('error')).toBe('此项必填')
    expect(normalizeFieldErrorMessage('请输入非负数字，最多两位小数'))
      .toBe('请输入非负数字，最多两位小数')
  })

  it('treats a transiently uninitialized form as empty instead of throwing', () => {
    const emptyForm = {} as LandDemandForm

    expect(validateDraft(emptyForm)).toEqual([])
    expect(validateStep(emptyForm, 1).map(error => error.field)).toEqual([
      'county',
      'region',
      'businessname',
      'creditcode',
    ])
  })

  it('validates only populated values in a draft', () => {
    expect(validateDraft({ ...validForm, area: '' })).toEqual([])
    expect(validateDraft({ ...validForm, area: '-1' })[0]?.field).toBe('area')
    expect(validateDraft({ ...validForm, pred_tax: '1.123' })[0]?.field).toBe('pred_tax')
  })

  it('rejects populated dictionary values that are no longer in the current options', () => {
    const invalidValues: Array<[keyof LandDemandForm, string]> = [
      ['expect_park', '330299'],
      ['expect_time', '2041-01'],
      ['is_deploy', 'maybe'],
      ['is_specialuse', 'maybe'],
      ['deploy_landtype', '旧类型'],
      ['project_hydm', '181'],
      ['keyindustry', '旧赛道'],
      ['futureindustry', '旧方向'],
    ]

    for (const [field, value] of invalidValues) {
      expect(validateDraft({ ...validForm, [field]: value } as LandDemandForm))
        .toEqual(expect.arrayContaining([expect.objectContaining({ field })]))
    }

    expect(validateDraft({ ...validForm, deploy_park: ['330203', '旧园区'] }))
      .toEqual(expect.arrayContaining([expect.objectContaining({ field: 'deploy_park' })]))
    expect(validateStep({ ...validForm, expect_park: '330299' }, 2))
      .toEqual(expect.arrayContaining([expect.objectContaining({ field: 'expect_park' })]))
  })

  it('does not validate stale values for hidden conditional fields', () => {
    expect(validateDraft({ ...validForm, is_deploy: '否', deploy_park: ['旧园区'] }))
      .not
      .toEqual(expect.arrayContaining([expect.objectContaining({ field: 'deploy_park' })]))
    expect(validateDraft({ ...validForm, is_specialuse: '否', deploy_landtype: '旧类型' }))
      .not
      .toEqual(expect.arrayContaining([expect.objectContaining({ field: 'deploy_landtype' })]))
  })

  it('keeps the optional office field as free-form text', () => {
    const form = { ...validForm, office: '生产部/总经理' }

    expect(validateDraft(form)).not.toEqual(expect.arrayContaining([
      expect.objectContaining({ field: 'office' }),
    ]))
    expect(validateSubmission(form)).not.toEqual(expect.arrayContaining([
      expect.objectContaining({ field: 'office' }),
    ]))
  })

  it('enforces backend integer precision while keeping two UI decimals', () => {
    for (const field of [
      'investment',
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
    expect(validateSubmission({ ...validForm, is_deploy: '是', deploy_park: [], is_specialuse: '' })
      .map(error => error.field)).toEqual(['deploy_park', 'is_specialuse'])
    expect(validateSubmission({ ...validForm, is_specialuse: '是', deploy_landtype: '' })
      .some(error => error.field === 'deploy_landtype')).toBe(true)
  })

  it('derives reached progress from values instead of stale local step metadata', () => {
    const throughStep2: LandDemandForm = {
      ...validForm,
      investment: '',
      project_hydm: '',
      keyindustry: '',
      futureindustry: '',
      pred_ys: '',
      pred_tax: '',
      pred_rdex: '',
      pred_unitenergy: '',
      projectdata: '',
      contact: '',
      office: '',
      phone: '',
    }

    expect(resolveProgressStep({ ...validForm, contact: '', office: '', phone: '' }, 1)).toBe(3)
    expect(resolveProgressStep(throughStep2, 1)).toBe(2)
    expect(resolveProgressStep(validForm, 1)).toBe(5)
    expect(resolveProgressStep({ ...validForm, area: '' }, 1)).toBe(3)
  })
})

import type { LandDemandForm, LandDemandRecord } from '@/features/land-demand/models'

import { describe, expect, it } from 'vitest'
import { buildSavePayload, buildUpdatePayload } from '@/features/land-demand/payload'

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
  deploy_park: ['330203', '330205'],
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

const original: LandDemandRecord = {
  ...validForm,
  deploy_park: '330203',
  landusedemand: '2',
  updatetime: '2026-07-28T00:00:00.000Z',
  updateuser: 'demo',
  industryCode: 'legacy-industry',
  is_energy: '是',
  energy: '4',
  energy_time: '2028-01',
  qyhydm: 'QY-001',
  registrationType: 2,
}

describe('land demand payload adapters', () => {
  it('builds a save payload with the selected status', () => {
    const payload = buildSavePayload(validForm, '2')

    expect(payload.landusedemand).toBe('2')
    expect(payload.deploy_park).toBe('330203,330205')
    expect(payload).not.toHaveProperty('updatetime')
    expect(payload).not.toHaveProperty('is_financing')
    expect(payload).not.toHaveProperty('financing_money')
    expect(payload).not.toHaveProperty('financing_time')
  })

  it('builds an update payload with the selected status', () => {
    expect(buildUpdatePayload(validForm, { ...original, creditcode: 'STALE-RECORD-OWNER' }, '1'))
      .toMatchObject({
        creditcode: validForm.creditcode,
        landusedemand: '1',
        newproject: '1',
      })
  })

  it('preserves hidden original fields and empty optional numeric values on update', () => {
    const payload = buildUpdatePayload({ ...validForm, deploy_height: '', deploy_weight: '' }, original, '2')

    expect(payload).toMatchObject({
      industryCode: 'legacy-industry',
      is_energy: '是',
      energy: '4',
      energy_time: '2028-01',
      qyhydm: 'QY-001',
      registrationType: 2,
      deploy_height: '',
      deploy_weight: '',
    })
    expect(payload).not.toHaveProperty('county')
    expect(payload).not.toHaveProperty('updatetime')
  })
})

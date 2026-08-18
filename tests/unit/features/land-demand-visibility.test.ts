import type { LandDemandForm } from '@/features/land-demand/models'

import { describe, expect, it } from 'vitest'
import {
  applySpecialUseChoice,
  applyTrackChoice,
  selectDeployPark,
} from '@/features/land-demand/visibility'

const form: LandDemandForm = {
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

describe('land demand field visibility transitions', () => {
  it('makes Ningbo city exclusive with regional parks', () => {
    expect(selectDeployPark(['330203'], '330200')).toEqual(['330200'])
    expect(selectDeployPark(['330200'], '330205')).toEqual(['330205'])
  })

  it('toggles an already selected whole-city choice off', () => {
    expect(selectDeployPark(['330200'], '330200')).toEqual([])
  })

  it('does not hide or clear optional height and weight with special use', () => {
    const next = applySpecialUseChoice({ ...form, deploy_height: '8', deploy_weight: '2' }, '否')

    expect(next.deploy_landtype).toBe('')
    expect(next.deploy_height).toBe('8')
    expect(next.deploy_weight).toBe('2')
  })

  it('clears the direction when changing an industry track', () => {
    expect(applyTrackChoice({ ...form, futureindustry: '具身大模型（大脑与小脑）' }, '生物医药').futureindustry).toBe('')
  })
})

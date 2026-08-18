import type { EnterpriseProfile } from '@/features/auth/models'
import type { LandDemandRecordInput } from '@/features/land-demand/models'

import { describe, expect, it } from 'vitest'
import { createLandDemandForm } from '@/features/land-demand/defaults'

const enterprise: EnterpriseProfile = {
  id: 'enterprise-1',
  username: 'demo',
  businessname: '示例企业',
  creditcode: '91330200MA2DEMO001',
  county: '海曙区',
  region: '集士港镇',
  contact: '张三',
  office: '总经理',
  phone: '13800138000',
}

describe('land demand form defaults', () => {
  it('creates a fresh form from the enterprise profile', () => {
    expect(createLandDemandForm(enterprise)).toMatchObject({
      county: enterprise.county,
      region: enterprise.region,
      businessname: enterprise.businessname,
      creditcode: enterprise.creditcode,
      contact: enterprise.contact,
      office: enterprise.office,
      phone: enterprise.phone,
      deploy_park: [],
    })
  })

  it('normalizes an existing comma-separated park value', () => {
    const form = createLandDemandForm(enterprise, { deploy_park: '330203,330205' })

    expect(form.deploy_park).toEqual(['330203', '330205'])
  })

  it('does not restore removed financing fields from a legacy record', () => {
    const legacyRecord = {
      is_financing: '',
      financing_money: '100',
      financing_time: '2027-06',
    } as unknown as Partial<LandDemandRecordInput>
    const form = createLandDemandForm(enterprise, legacyRecord)

    expect(form).not.toHaveProperty('is_financing')
    expect(form).not.toHaveProperty('financing_money')
    expect(form).not.toHaveProperty('financing_time')
  })
})

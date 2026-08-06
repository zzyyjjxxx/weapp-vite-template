import type { EnterpriseProfile } from '@/features/auth/models'
import type { LandDemandForm } from '@/features/land-demand/models'

import { beforeEach, describe, expect, it } from 'vitest'
import { configureLandDemandRepository, createMockLandDemandRepository } from '@/features/land-demand/repository'
import { useLandDemandStore } from '@/stores/land-demand'
import { createMemoryStorage } from '../../helpers/memory-storage'

const enterprise: EnterpriseProfile = {
  id: 'enterprise-demo',
  username: 'demo',
  businessname: '示例企业',
  creditcode: '91330200MA2DEMO001',
  county: '鄞州区',
  region: '首南街道',
  contact: '张三',
  office: '总经理',
  phone: '13800000000',
}

const form: LandDemandForm = {
  county: enterprise.county,
  region: enterprise.region,
  businessname: enterprise.businessname,
  creditcode: enterprise.creditcode,
  area: '30',
  building_area: '10000',
  expect_park: '330203',
  expect_time: '2027-06',
  is_deploy: '是',
  deploy_park: ['330203'],
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
  projectdata: '项目建设内容',
  is_financing: '没有',
  financing_money: '',
  financing_time: '',
  contact: enterprise.contact,
  office: enterprise.office,
  phone: enterprise.phone,
}

describe('land demand store', () => {
  const repository = createMockLandDemandRepository({ storage: createMemoryStorage() })

  beforeEach(() => {
    configureLandDemandRepository(repository)
    useLandDemandStore().$reset()
  })

  it('restores a local draft into the editable snapshot and tracks edits', () => {
    const store = useLandDemandStore()
    store.initialize(enterprise, undefined, { form, currentStep: 3, savedAt: 1_000 })

    expect(store.currentStep.value).toBe(3)
    expect(store.progressStep.value).toBe(3)
    expect(store.hasRecord.value).toBe(false)
    expect(store.isDirty.value).toBe(false)
    store.patch({ area: '31' })
    expect(store.form.value.area).toBe('31')
    expect(store.isDirty.value).toBe(true)
  })

  it('keeps filled progress when revisiting an earlier step', () => {
    const store = useLandDemandStore()
    store.initialize(enterprise, undefined, { form, currentStep: 4, savedAt: 1_000 })

    store.goToStep(2)
    expect(store.currentStep.value).toBe(2)
    expect(store.progressStep.value).toBe(4)

    store.goToStep(5)
    store.goToStep(3)
    expect(store.currentStep.value).toBe(3)
    expect(store.progressStep.value).toBe(5)
  })

  it('reasserts authenticated identity over a tampered local draft', () => {
    const store = useLandDemandStore()
    store.initialize(enterprise, undefined, {
      form: {
        ...form,
        businessname: '篡改企业',
        creditcode: '91330200TAMPERED001',
        county: '篡改区县',
        region: '篡改乡镇',
      },
      currentStep: 2,
      savedAt: 1_000,
    })

    expect(store.form.value).toMatchObject({
      businessname: enterprise.businessname,
      creditcode: enterprise.creditcode,
      county: enterprise.county,
      region: enterprise.region,
      area: form.area,
    })
  })

  it('does not allow patches to replace authenticated ownership identity', () => {
    const store = useLandDemandStore()
    store.initialize(enterprise)
    store.patch({
      businessname: '篡改企业',
      creditcode: '91330200TAMPERED001',
      county: '篡改区县',
      region: '篡改乡镇',
      area: '31',
    })

    expect(store.form.value).toMatchObject({
      businessname: enterprise.businessname,
      creditcode: enterprise.creditcode,
      county: enterprise.county,
      region: enterprise.region,
      area: '31',
    })
  })

  it('loads the persisted local draft through the Store boundary', () => {
    repository.setDraft(enterprise.creditcode, {
      form: { ...form, area: '42' },
      currentStep: 2,
      savedAt: 2_000,
    })

    const store = useLandDemandStore()
    store.initializeFromLocalDraft(enterprise)

    expect(store.form.value.area).toBe('42')
    expect(store.currentStep.value).toBe(2)
  })

  it('persists and discards only local draft metadata', () => {
    const store = useLandDemandStore()
    store.initialize(enterprise)
    store.patch({ area: '31' })
    store.goToStep(2)
    store.saveLocalDraft()

    expect(repository.getDraft(enterprise.creditcode)).toMatchObject({
      form: { area: '31' },
      currentStep: 2,
      progressStep: 2,
    })
    expect(store.isDirty.value).toBe(false)
    store.discardLocalDraft()
    expect(repository.getDraft(enterprise.creditcode)).toBeUndefined()
  })

  it('marks persistence without retaining a server record object', async () => {
    const store = useLandDemandStore()
    store.initialize(enterprise)
    store.patch({ area: '31' })
    const record = await repository.save({ ...form, deploy_park: '330203', landusedemand: '2' })

    store.markPersisted(record)
    expect(store.hasRecord.value).toBe(true)
    expect(store.isDirty.value).toBe(false)
    expect(store).not.toHaveProperty('record')
    expect(store.form.value.area).toBe('30')
  })
})

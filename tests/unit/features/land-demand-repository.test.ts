import type { LandDemandForm, SaveLandDemandPayload } from '@/features/land-demand/models'

import { describe, expect, it } from 'vitest'
import { createMockLandDemandRepository } from '@/features/land-demand/repository'
import { createMemoryStorage } from '../../helpers/memory-storage'

const form: LandDemandForm = {
  county: '鄞州区',
  region: '首南街道',
  businessname: '示例企业',
  creditcode: '91330200MA2DEMO001',
  area: '30',
  building_area: '10000',
  expect_park: '330203',
  expect_time: '2027-06',
  is_deploy: '是',
  deploy_park: ['330203'],
  is_specialuse: '否',
  deploy_landtype: '',
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
  projectdata: '项目建设内容',
  is_financing: '没有',
  financing_money: '',
  financing_time: '',
  contact: '张三',
  office: '总经理',
  phone: '13800000000',
}

const savePayload: SaveLandDemandPayload = {
  ...form,
  deploy_park: form.deploy_park.join(','),
  landusedemand: '2',
}

describe('mock land demand repository', () => {
  it('persists independent record and draft snapshots', async () => {
    const repository = createMockLandDemandRepository({
      storage: createMemoryStorage(),
      now: () => 1_000,
    })

    const saved = await repository.save(savePayload)
    saved.area = 'mutated'
    expect((await repository.get(form.creditcode))?.area).toBe('30')

    repository.setDraft(form.creditcode, { form, currentStep: 3, savedAt: 1_000 })
    form.deploy_park.push('330205')
    expect(repository.getDraft(form.creditcode)).toMatchObject({
      currentStep: 3,
      form: { deploy_park: ['330203'] },
    })
  })

  it('rejects duplicate saves and requires an existing record for updates', async () => {
    const repository = createMockLandDemandRepository({ storage: createMemoryStorage() })

    await repository.save(savePayload)
    await expect(repository.save(savePayload)).rejects.toThrow('填报记录已存在')
    await expect(repository.update({
      ...savePayload,
      creditcode: '91330200MA2MISSING1',
      newproject: '1',
    })).rejects.toThrow('填报记录不存在')
  })

  it('reports storage write failures and keeps the recoverable local draft', async () => {
    const memory = createMemoryStorage()
    memory.set('draft:land-demand:91330200MA2DEMO001', {
      form,
      currentStep: 3,
      savedAt: 1_000,
    })
    const repository = createMockLandDemandRepository({
      storage: {
        ...memory,
        set: (key, value) => {
          if (key.startsWith('mock:land-demand:')) {
            throw new Error('storage full')
          }
          memory.set(key, value)
        },
      },
    })

    await expect(repository.save(savePayload)).rejects.toThrow('storage full')
    expect(repository.getDraft(form.creditcode)).toMatchObject({ currentStep: 3 })
  })

  it('does not overwrite a record when storage cannot determine whether it exists', async () => {
    let writes = 0
    const repository = createMockLandDemandRepository({
      storage: {
        get: () => { throw new Error('storage unreadable') },
        set: () => { writes += 1 },
        remove: () => undefined,
      },
    })

    await expect(repository.save(savePayload)).rejects.toThrow('storage unreadable')
    expect(writes).toBe(0)
  })

  it('preserves hidden fields while updating the mutable record fields', async () => {
    const repository = createMockLandDemandRepository({
      storage: createMemoryStorage(),
      now: () => 2_000,
    })
    await repository.save({
      ...savePayload,
      industryCode: 'legacy-industry',
      is_energy: '是',
      energy: '4',
      energy_time: '2028-01',
      qyhydm: 'QY-001',
      registrationType: 2,
    })

    const updated = await repository.update({
      ...savePayload,
      area: '31',
      newproject: '1',
      industryCode: '',
      is_energy: '',
      energy: '',
      energy_time: '',
      qyhydm: '',
      registrationType: undefined,
    })

    expect(updated).toMatchObject({
      area: '31',
      industryCode: 'legacy-industry',
      is_energy: '是',
      energy: '4',
      energy_time: '2028-01',
      qyhydm: 'QY-001',
      registrationType: 2,
      updatetime: new Date(2_000).toISOString(),
    })
  })

  it('preserves the last successful submission time while saving a later draft', async () => {
    let time = 1_000
    const repository = createMockLandDemandRepository({
      storage: createMemoryStorage(),
      now: () => time,
    })

    const submitted = await repository.save({
      ...savePayload,
      landusedemand: '1',
    })
    const submittedAt = new Date(1_000).toISOString()
    expect(submitted.lastSubmittedAt).toBe(submittedAt)

    time = 2_000
    const draft = await repository.update({
      ...savePayload,
      landusedemand: '2',
      newproject: '1',
    })
    expect(draft.updatetime).toBe(new Date(2_000).toISOString())
    expect(draft.lastSubmittedAt).toBe(submittedAt)

    time = 3_000
    const resubmitted = await repository.update({
      ...savePayload,
      landusedemand: '1',
      newproject: '1',
    })
    expect(resubmitted.lastSubmittedAt).toBe(new Date(3_000).toISOString())
  })

  it('expires codes after five minutes and after five incorrect attempts', async () => {
    let time = 1_000
    const repository = createMockLandDemandRepository({
      storage: createMemoryStorage(),
      now: () => time,
      randomCode: () => '123456',
    })

    const challenge = await repository.sendCode('13800000000')
    expect(challenge).toMatchObject({
      phone: '13800000000',
      expiresAt: 301_000,
      retryAt: 61_000,
      mockCode: '123456',
    })
    await expect(repository.sendCode('13800000000')).resolves.toEqual(challenge)
    for (let index = 0; index < 5; index += 1) {
      await expect(repository.verifyCode('13800000000', '000000')).rejects.toThrow()
    }
    await expect(repository.verifyCode('13800000000', '123456')).rejects.toThrow('验证码已失效')

    time = 70_000
    await repository.sendCode('13800000000')
    time = 370_000
    await expect(repository.verifyCode('13800000000', '123456')).rejects.toThrow('验证码已失效')
  })

  it('invalidates a successfully verified code exactly once', async () => {
    const repository = createMockLandDemandRepository({
      storage: createMemoryStorage(),
      randomCode: () => '123456',
    })
    const challenge = await repository.sendCode('13800000000')

    await repository.verifyCode(challenge.phone, challenge.mockCode)
    await expect(repository.verifyCode(challenge.phone, challenge.mockCode)).rejects.toThrow('验证码已失效')
  })

  it('allows a new verification code immediately after successful verification', async () => {
    const time = 1_000
    const repository = createMockLandDemandRepository({
      storage: createMemoryStorage(),
      now: () => time,
      randomCode: () => '123456',
    })
    const challenge = await repository.sendCode('13800000000')

    await repository.verifyCode(challenge.phone, challenge.mockCode)
    await expect(repository.verifyCode(challenge.phone, challenge.mockCode)).rejects.toThrow('验证码已失效')
    const resent = await repository.sendCode(challenge.phone)
    expect(resent.retryAt).toBe(time + 60_000)
    await expect(repository.sendCode(challenge.phone)).resolves.toEqual(resent)
  })

  it('recovers a legacy successful challenge without blocking a new request', async () => {
    const storage = createMemoryStorage()
    storage.set('mock:verification:13800000000', {
      phone: '13800000000',
      expiresAt: 301_000,
      retryAt: 61_000,
      mockCode: '123456',
      attempts: 0,
      invalidated: true,
    })
    const repository = createMockLandDemandRepository({
      storage,
      now: () => 1_000,
      randomCode: () => '654321',
    })

    await expect(repository.sendCode('13800000000')).resolves.toMatchObject({
      mockCode: '654321',
    })
  })

  it('recovers a legacy exhausted challenge without clearing other storage', async () => {
    const storage = createMemoryStorage()
    storage.set('mock:verification:13800000000', {
      phone: '13800000000',
      expiresAt: 301_000,
      retryAt: 61_000,
      mockCode: '123456',
      attempts: 5,
      invalidated: true,
    })
    const repository = createMockLandDemandRepository({
      storage,
      now: () => 1_000,
      randomCode: () => '654321',
    })

    await expect(repository.sendCode('13800000000')).resolves.toMatchObject({
      mockCode: '654321',
    })
  })

  it('keeps the resend cooldown after the fifth incorrect attempt', async () => {
    let time = 1_000
    const repository = createMockLandDemandRepository({
      storage: createMemoryStorage(),
      now: () => time,
      randomCode: () => '123456',
    })
    const challenge = await repository.sendCode('13800000000')

    for (let index = 0; index < 5; index += 1) {
      await expect(repository.verifyCode(challenge.phone, '000000')).rejects.toThrow('验证码错误')
    }
    await expect(repository.verifyCode(challenge.phone, challenge.mockCode)).rejects.toThrow('验证码已失效')
    await expect(repository.sendCode(challenge.phone)).rejects.toThrow('请稍后再试')
    time = challenge.retryAt
    await expect(repository.sendCode(challenge.phone)).resolves.toBeDefined()
  })
})

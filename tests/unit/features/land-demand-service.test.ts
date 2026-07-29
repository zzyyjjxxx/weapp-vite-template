import type { EnterpriseProfile } from '@/features/auth/models'
import type { LandDemandForm } from '@/features/land-demand/models'

import { beforeEach, describe, expect, it } from 'vitest'
import { useLandDemandQuery, useSaveLandDemandMutation, useUpdateLandDemandMutation } from '@/features/land-demand/queries'
import { landDemandKeys } from '@/features/land-demand/query-keys'
import { createMockLandDemandRepository } from '@/features/land-demand/repository'
import { getLandDemandInfo, saveLandDemand, updateLandDemand } from '@/features/land-demand/service'
import { createQueryClient } from '@/shared/query/client'
import { configureQueryLifecycleAdapter, resetQueryLifecycleAdapter } from '@/shared/query/lifecycle'
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

function createLifecycle(): { dispose: () => void, onUnmounted: (callback: () => void) => void } {
  const callbacks: Array<() => void> = []
  return {
    onUnmounted: callback => callbacks.push(callback),
    dispose: () => callbacks.splice(0).forEach(callback => callback()),
  }
}

describe('land demand service and queries', () => {
  let repository: ReturnType<typeof createMockLandDemandRepository>

  beforeEach(() => {
    repository = createMockLandDemandRepository({
      storage: createMemoryStorage(),
      now: () => 1_000,
    })
  })

  it('switches from save to update after the first draft persistence', async () => {
    const saved = await saveLandDemand(form, '2', { repository, updateuser: enterprise.username })
    expect(saved).toMatchObject({ landusedemand: '2', updateuser: enterprise.username })
    expect(await getLandDemandInfo(form.creditcode, { repository })).toBeDefined()

    const updated = await updateLandDemand({ ...form, area: '31' }, saved, '2', {
      repository,
      updateuser: enterprise.username,
    })
    expect(updated.area).toBe('31')
  })

  it('writes save and update mutation results to the exact detail cache', async () => {
    const client = createQueryClient()
    const lifecycle = createLifecycle()
    configureQueryLifecycleAdapter(lifecycle)

    const saveMutation = useSaveLandDemandMutation({ client, repository })
    const saved = await saveMutation.mutateAsync({ form, status: '2', updateuser: enterprise.username })
    expect(client.getQueryData(landDemandKeys.detail(form.creditcode))).toEqual(saved)

    const updateMutation = useUpdateLandDemandMutation({ client, repository })
    const updated = await updateMutation.mutateAsync({
      form: { ...form, area: '31' },
      original: saved,
      status: '2',
      updateuser: enterprise.username,
    })
    expect(client.getQueryData(landDemandKeys.detail(form.creditcode))).toEqual(updated)

    lifecycle.dispose()
    resetQueryLifecycleAdapter()
    client.clear()
    client.unmount()
  })

  it('loads the server record through a private detail query', async () => {
    const client = createQueryClient()
    const lifecycle = createLifecycle()
    configureQueryLifecycleAdapter(lifecycle)
    await saveLandDemand(form, '2', { repository })

    const query = useLandDemandQuery(form.creditcode, { client, repository })
    await query.refetch()
    expect(query.data.value?.creditcode).toBe(form.creditcode)

    lifecycle.dispose()
    resetQueryLifecycleAdapter()
    client.clear()
    client.unmount()
  })
})

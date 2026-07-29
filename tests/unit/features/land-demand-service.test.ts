import type { EnterpriseProfile } from '@/features/auth/models'
import type { LandDemandForm } from '@/features/land-demand/models'

import { beforeEach, describe, expect, it } from 'vitest'
import { useLandDemandQuery, useSaveLandDemandMutation, useUpdateLandDemandMutation } from '@/features/land-demand/queries'
import { landDemandKeys } from '@/features/land-demand/query-keys'
import { configureLandDemandRepository, createMockLandDemandRepository } from '@/features/land-demand/repository'
import { getLandDemandInfo, saveLandDemand, updateLandDemand } from '@/features/land-demand/service'
import { createQueryClient } from '@/shared/query/client'
import { configureQueryLifecycleAdapter, resetQueryLifecycleAdapter } from '@/shared/query/lifecycle'
import { clearPrivateQueryCaches, PRIVATE_QUERY_SCOPE } from '@/shared/query/private-cache'
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

  it('uses the Query detail value as the original record on a second draft save', async () => {
    const client = createQueryClient()
    const lifecycle = createLifecycle()
    configureQueryLifecycleAdapter(lifecycle)
    const query = useLandDemandQuery(form.creditcode, { client, repository })
    await query.refetch()

    const saveMutation = useSaveLandDemandMutation({ client, repository })
    const saved = await saveMutation.mutateAsync({ form, status: '2' })
    expect(query.data.value).toEqual(saved)

    const original = query.data.value
    expect(original).toBeDefined()
    const updateMutation = useUpdateLandDemandMutation({ client, repository })
    const updated = await updateMutation.mutateAsync({
      form: { ...form, area: '32' },
      original: original!,
      status: '2',
    })

    expect(updated.area).toBe('32')
    expect(query.data.value).toEqual(updated)
    lifecycle.dispose()
    resetQueryLifecycleAdapter()
    client.clear()
    client.unmount()
  })

  it('marks mutation-created detail cache entries as private', async () => {
    const client = createQueryClient()
    const lifecycle = createLifecycle()
    configureQueryLifecycleAdapter(lifecycle)

    const mutation = useSaveLandDemandMutation({ client, repository })
    await mutation.mutateAsync({ form, status: '2', updateuser: enterprise.username })
    const queryKey = landDemandKeys.detail(form.creditcode)
    const scope = client.getQueryCache().find({ queryKey, exact: true })?.meta?.scope

    clearPrivateQueryCaches(client)
    const cached = client.getQueryData(queryKey)
    lifecycle.dispose()
    resetQueryLifecycleAdapter()
    client.clear()
    client.unmount()

    expect(scope).toBe(PRIVATE_QUERY_SCOPE)
    expect(cached).toBeUndefined()
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

  it('clears local draft metadata after saving and reloads the record through Query', async () => {
    const client = createQueryClient()
    const lifecycle = createLifecycle()
    configureQueryLifecycleAdapter(lifecycle)
    configureLandDemandRepository(repository)
    const store = useLandDemandStore()
    store.$reset()
    store.initialize(enterprise)
    store.patch({ area: '41' })
    store.goToStep(2)
    store.saveLocalDraft()
    expect(repository.getDraft(form.creditcode)).toBeDefined()

    const mutation = useSaveLandDemandMutation({ client, repository })
    const saved = await mutation.mutateAsync({
      form: store.form.value,
      status: '2',
      updateuser: enterprise.username,
    })
    store.markPersisted(saved)
    expect(repository.getDraft(form.creditcode)).toBeUndefined()

    client.removeQueries({ queryKey: landDemandKeys.detail(form.creditcode), exact: true })
    const query = useLandDemandQuery(form.creditcode, { client, repository })
    await query.refetch()
    store.$reset()
    store.initialize(enterprise, query.data.value)

    expect(store.form.value.area).toBe('41')
    expect(store.currentStep.value).toBe(1)

    configureLandDemandRepository()
    lifecycle.dispose()
    resetQueryLifecycleAdapter()
    client.clear()
    client.unmount()
  })
})

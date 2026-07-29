import type {
  LandDemandDraft,
  LandDemandRecord,
  SaveLandDemandPayload,
  UpdateLandDemandPayload,
} from './models'

import type { StorageAdapter } from '@/platform/storage'
import { createWpiStorageAdapter } from '@/platform/storage'

export interface VerificationChallenge {
  phone: string
  expiresAt: number
  retryAt: number
  mockCode: string
}

export interface LandDemandRepository {
  get: (creditcode: string) => Promise<LandDemandRecord | undefined>
  save: (payload: SaveLandDemandPayload) => Promise<LandDemandRecord>
  update: (payload: UpdateLandDemandPayload) => Promise<LandDemandRecord>
  getDraft: (creditcode: string) => LandDemandDraft | undefined
  setDraft: (creditcode: string, draft: LandDemandDraft) => void
  removeDraft: (creditcode: string) => void
  sendCode: (phone: string) => Promise<VerificationChallenge>
  verifyCode: (phone: string, code: string) => Promise<void>
}

interface StoredVerification extends VerificationChallenge {
  attempts: number
}

const CODE_EXPIRY_MS = 5 * 60 * 1_000
const RESEND_DELAY_MS = 60 * 1_000
const MAX_VERIFICATION_ATTEMPTS = 5

let configuredRepository: LandDemandRepository | undefined
let defaultRepository: LandDemandRepository | undefined

function recordKey(creditcode: string): string {
  return `mock:land-demand:${creditcode}`
}

function draftKey(creditcode: string): string {
  return `draft:land-demand:${creditcode}`
}

function verificationKey(phone: string): string {
  return `mock:verification:${phone}`
}

function cloneRecord(record: LandDemandRecord): LandDemandRecord {
  return { ...record }
}

function cloneDraft(draft: LandDemandDraft): LandDemandDraft {
  return {
    ...draft,
    form: {
      ...draft.form,
      deploy_park: [...draft.form.deploy_park],
    },
  }
}

function cloneChallenge(challenge: VerificationChallenge): VerificationChallenge {
  return { ...challenge }
}

function wait(delayMs: number): Promise<void> {
  return delayMs > 0
    ? new Promise(resolve => setTimeout(resolve, delayMs))
    : Promise.resolve()
}

function createRandomCode(): string {
  return String(Math.floor(Math.random() * 1_000_000)).padStart(6, '0')
}

function getUpdateUser(payload: object): string {
  const candidate = (payload as { updateuser?: unknown }).updateuser
  return typeof candidate === 'string' && candidate ? candidate : 'demo'
}

function preserved<T>(incoming: T | undefined, existing: T | undefined): T | undefined {
  return incoming === undefined || incoming === '' ? existing : incoming
}

export function createMockLandDemandRepository(options: {
  storage: StorageAdapter
  now?: () => number
  randomCode?: () => string
  delayMs?: number
}): LandDemandRepository {
  const now = options.now ?? Date.now
  const randomCode = options.randomCode ?? createRandomCode
  const delayMs = options.delayMs ?? 0
  const { storage } = options

  return {
    async get(creditcode) {
      await wait(delayMs)
      const record = storage.get<LandDemandRecord>(recordKey(creditcode))
      return record ? cloneRecord(record) : undefined
    },

    async save(payload) {
      await wait(delayMs)
      if (storage.get<LandDemandRecord>(recordKey(payload.creditcode))) {
        throw new Error('填报记录已存在')
      }

      const record: LandDemandRecord = {
        ...payload,
        updatetime: new Date(now()).toISOString(),
        updateuser: getUpdateUser(payload),
      }
      storage.set(recordKey(record.creditcode), cloneRecord(record))
      return cloneRecord(record)
    },

    async update(payload) {
      await wait(delayMs)
      const existing = storage.get<LandDemandRecord>(recordKey(payload.creditcode))
      if (!existing) {
        throw new Error('填报记录不存在')
      }

      const record: LandDemandRecord = {
        ...existing,
        ...payload,
        county: existing.county,
        region: existing.region,
        businessname: existing.businessname,
        industryCode: preserved(payload.industryCode, existing.industryCode),
        is_energy: preserved(payload.is_energy, existing.is_energy),
        energy: preserved(payload.energy, existing.energy),
        energy_time: preserved(payload.energy_time, existing.energy_time),
        qyhydm: preserved(payload.qyhydm, existing.qyhydm),
        registrationType: preserved(payload.registrationType, existing.registrationType),
        updatetime: new Date(now()).toISOString(),
        updateuser: getUpdateUser(payload),
      }
      storage.set(recordKey(record.creditcode), cloneRecord(record))
      return cloneRecord(record)
    },

    getDraft(creditcode) {
      const draft = storage.get<LandDemandDraft>(draftKey(creditcode))
      return draft ? cloneDraft(draft) : undefined
    },

    setDraft(creditcode, draft) {
      storage.set(draftKey(creditcode), cloneDraft(draft))
    },

    removeDraft(creditcode) {
      storage.remove(draftKey(creditcode))
    },

    async sendCode(phone) {
      await wait(delayMs)
      const timestamp = now()
      const previous = storage.get<StoredVerification>(verificationKey(phone))
      if (previous && timestamp < previous.retryAt) {
        throw new Error('请稍后再试')
      }

      const challenge: StoredVerification = {
        phone,
        expiresAt: timestamp + CODE_EXPIRY_MS,
        retryAt: timestamp + RESEND_DELAY_MS,
        mockCode: randomCode(),
        attempts: 0,
      }
      storage.set(verificationKey(phone), challenge)
      return cloneChallenge(challenge)
    },

    async verifyCode(phone, code) {
      await wait(delayMs)
      const stored = storage.get<StoredVerification>(verificationKey(phone))
      if (!stored || now() >= stored.expiresAt) {
        storage.remove(verificationKey(phone))
        throw new Error('验证码已失效')
      }

      if (code === stored.mockCode) {
        storage.remove(verificationKey(phone))
        return
      }

      const attempts = stored.attempts + 1
      if (attempts >= MAX_VERIFICATION_ATTEMPTS) {
        storage.remove(verificationKey(phone))
      }
      else {
        storage.set(verificationKey(phone), { ...stored, attempts })
      }
      throw new Error('验证码错误')
    },
  }
}

export function configureLandDemandRepository(repository?: LandDemandRepository): void {
  configuredRepository = repository
}

export function getLandDemandRepository(): LandDemandRepository {
  defaultRepository ??= createMockLandDemandRepository({
    storage: createWpiStorageAdapter(),
  })
  return configuredRepository ?? defaultRepository
}

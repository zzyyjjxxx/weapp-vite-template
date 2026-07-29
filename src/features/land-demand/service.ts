import type {
  LandDemandForm,
  LandDemandRecord,
  LandDemandStatus,
} from './models'
import type { LandDemandRepository, VerificationChallenge } from './repository'

import { buildSavePayload, buildUpdatePayload } from './payload'
import { getLandDemandRepository } from './repository'

interface ServiceOptions {
  repository?: LandDemandRepository
}

interface PersistOptions extends ServiceOptions {
  updateuser?: string
}

function resolveRepository(repository?: LandDemandRepository): LandDemandRepository {
  return repository ?? getLandDemandRepository()
}

export function getLandDemandInfo(
  creditcode: string,
  options: ServiceOptions = {},
): Promise<LandDemandRecord | undefined> {
  return resolveRepository(options.repository).get(creditcode)
}

export function saveLandDemand(
  form: LandDemandForm,
  status: LandDemandStatus,
  options: PersistOptions = {},
): Promise<LandDemandRecord> {
  const payload = {
    ...buildSavePayload(form, status),
    updateuser: options.updateuser ?? 'demo',
  }
  return resolveRepository(options.repository).save(payload)
}

export function updateLandDemand(
  form: LandDemandForm,
  original: LandDemandRecord,
  status: LandDemandStatus,
  options: PersistOptions = {},
): Promise<LandDemandRecord> {
  const payload = {
    ...buildUpdatePayload(form, original, status),
    updateuser: options.updateuser ?? 'demo',
  }
  return resolveRepository(options.repository).update(payload)
}

export function sendVerificationCode(
  phone: string,
  options: ServiceOptions = {},
): Promise<VerificationChallenge> {
  return resolveRepository(options.repository).sendCode(phone)
}

export function verifyVerificationCode(
  phone: string,
  code: string,
  options: ServiceOptions = {},
): Promise<void> {
  return resolveRepository(options.repository).verifyCode(phone, code)
}

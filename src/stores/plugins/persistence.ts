import type { StorageAdapter } from '@/platform/storage'
import type { AuthSession, EnterpriseProfile } from '@/features/auth/models'

export const AUTH_STORAGE_KEY = 'land-demand.auth'

export interface PersistedAuthStateV1 {
  version: 1
  session: AuthSession | null
}

interface PersistenceStore {
  $id: string
  $patch: (patch: Record<string, unknown>) => void
  $subscribe: (
    callback: (mutation: unknown, state: unknown) => void,
    options?: { detached?: boolean },
  ) => () => void
}

interface RecordLike {
  [key: string]: unknown
}

function isRecord(value: unknown): value is RecordLike {
  return typeof value === 'object' && value !== null
}

function unwrapRef(value: unknown): unknown {
  if (isRecord(value) && 'value' in value) {
    return value.value
  }
  return value
}

function isAuthSession(value: unknown): value is AuthSession {
  if (!isRecord(value)) {
    return false
  }

  return (
    typeof value.token === 'string'
    && typeof value.expiresAt === 'number'
    && isEnterpriseProfile(value.enterprise)
  )
}

function isEnterpriseProfile(value: unknown): value is EnterpriseProfile {
  if (!isRecord(value)) {
    return false
  }

  return (
    typeof value.id === 'string'
    && typeof value.username === 'string'
    && typeof value.businessname === 'string'
    && typeof value.creditcode === 'string'
    && typeof value.county === 'string'
    && typeof value.region === 'string'
    && typeof value.contact === 'string'
    && typeof value.office === 'string'
    && typeof value.phone === 'string'
  )
}

function isPersistedAuthState(value: unknown): value is PersistedAuthStateV1 {
  if (!isRecord(value) || value.version !== 1) {
    return false
  }

  return value.session === null || isAuthSession(value.session)
}

function readSession(state: unknown): AuthSession | null {
  if (!isRecord(state)) {
    return null
  }

  const session = unwrapRef(state.session)
  return isAuthSession(session) ? session : null
}

export function createPersistencePlugin(storage: StorageAdapter) {
  return ({ store }: { store: PersistenceStore }): void => {
    if (store.$id !== 'auth') {
      return
    }

    const persisted = storage.get<unknown>(AUTH_STORAGE_KEY)
    if (isPersistedAuthState(persisted)) {
      store.$patch({ session: persisted.session })
    }

    store.$subscribe((_mutation, state) => {
      storage.set<PersistedAuthStateV1>(AUTH_STORAGE_KEY, {
        version: 1,
        session: readSession(state),
      })
    }, { detached: true })
  }
}

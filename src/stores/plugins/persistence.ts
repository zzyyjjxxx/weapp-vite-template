import type { AuthSession, EnterpriseProfile } from '@/features/auth/models'
import type { StorageAdapter } from '@/platform/storage'

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

function isAuthSession(value: unknown): value is AuthSession {
  if (!isRecord(value)) {
    return false
  }

  const refreshToken = unwrapRef(value.refreshToken)
  const tokenType = unwrapRef(value.tokenType)
  const refreshExpiresAt = unwrapRef(value.refreshExpiresAt)

  return (
    typeof value.token === 'string'
    && typeof value.expiresAt === 'number'
    && (refreshToken === undefined || typeof refreshToken === 'string')
    && (tokenType === undefined || typeof tokenType === 'string')
    && (refreshExpiresAt === undefined || typeof refreshExpiresAt === 'number')
    && isEnterpriseProfile(value.enterprise)
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

function readSessionClearRevision(state: unknown): number {
  if (!isRecord(state)) {
    return 0
  }
  const revision = unwrapRef(state.sessionClearRevision)
  return typeof revision === 'number' ? revision : 0
}

export function readPersistedAuthSession(
  storage: StorageAdapter,
): AuthSession | null | undefined {
  const persisted = storage.get<unknown>(AUTH_STORAGE_KEY)
  return isPersistedAuthState(persisted) ? persisted.session : undefined
}

export function createPersistencePlugin(storage: StorageAdapter) {
  return ({ store }: { store: PersistenceStore }): void => {
    if (store.$id !== 'auth') {
      return
    }

    const persistedSession = readPersistedAuthSession(storage)
    if (persistedSession !== undefined) {
      store.$patch({ session: persistedSession })
    }

    let sessionClearRevision = 0
    store.$subscribe((_mutation, state) => {
      const session = readSession(state)
      const nextSessionClearRevision = readSessionClearRevision(state)
      if (!session && nextSessionClearRevision === sessionClearRevision) {
        return
      }
      sessionClearRevision = nextSessionClearRevision
      if (!session) {
        storage.remove(AUTH_STORAGE_KEY)
        return
      }
      storage.set<PersistedAuthStateV1>(AUTH_STORAGE_KEY, {
        version: 1,
        session,
      })
    }, { detached: true })
  }
}

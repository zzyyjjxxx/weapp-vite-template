import type { StorageAdapter } from '@/platform/storage'
import type { AuthSession } from '@/shared/http/session'

export const AUTH_STORAGE_KEY = 'weapp-vite-hono.auth'

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
    typeof value.accessToken === 'string'
    && typeof value.refreshToken === 'string'
    && typeof value.expiresAt === 'number'
    && typeof value.userId === 'string'
    && (value.tenantId === undefined || typeof value.tenantId === 'string')
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

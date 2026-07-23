export interface AuthSession {
  accessToken: string
  refreshToken: string
  expiresAt: number
  userId: string
  tenantId?: string
}

export interface AuthSessionStore {
  getAccessToken: () => string | undefined
  getRefreshToken: () => string | undefined
  setSession: (session: AuthSession) => void
  clearSession: () => void
}

const emptyStore: AuthSessionStore = {
  getAccessToken: () => undefined,
  getRefreshToken: () => undefined,
  setSession: () => undefined,
  clearSession: () => undefined,
}

let authSessionStore: AuthSessionStore = emptyStore

export function configureAuthSessionStore(store: AuthSessionStore): void {
  authSessionStore = store
}

export function resetAuthSessionStore(): void {
  authSessionStore = emptyStore
}

export function getAuthSessionStore(): AuthSessionStore {
  return authSessionStore
}

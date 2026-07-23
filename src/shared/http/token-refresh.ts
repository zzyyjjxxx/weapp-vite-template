import type { AuthSession } from './session'
import { ApiError } from './errors'
import { getAuthSessionStore } from './session'
import { transportRequest } from './transport'

type PrivateQueryCacheClearer = () => void | Promise<void>

let refreshPromise: Promise<AuthSession> | undefined
let privateQueryCacheClearer: PrivateQueryCacheClearer = () => undefined

export function configurePrivateQueryCacheClearer(
  clearer: PrivateQueryCacheClearer,
): void {
  privateQueryCacheClearer = clearer
}

async function clearPrivateState(): Promise<void> {
  const store = getAuthSessionStore()
  store.clearSession()

  try {
    await privateQueryCacheClearer()
  }
  catch {
    // Cache cleanup must not hide the authentication error that triggered it.
  }
}

async function performRefresh(): Promise<AuthSession> {
  const refreshToken = getAuthSessionStore().getRefreshToken()
  if (!refreshToken) {
    const error = new ApiError({
      kind: 'unauthorized',
      message: '缺少刷新凭据',
      code: 'MISSING_REFRESH_TOKEN',
      status: 401,
    })
    await clearPrivateState()
    throw error
  }

  try {
    const session = await transportRequest<AuthSession, { refreshToken: string }>({
      path: '/auth/refresh',
      method: 'POST',
      auth: 'none',
      skipTokenRefresh: true,
      body: { refreshToken },
    })
    getAuthSessionStore().setSession(session)
    return session
  }
  catch (error) {
    await clearPrivateState()
    throw error
  }
}

export function refreshAccessTokenSingleFlight(): Promise<AuthSession> {
  if (refreshPromise) {
    return refreshPromise
  }

  refreshPromise = performRefresh()
  void refreshPromise.then(() => {
    refreshPromise = undefined
  }, () => {
    refreshPromise = undefined
  })
  return refreshPromise
}

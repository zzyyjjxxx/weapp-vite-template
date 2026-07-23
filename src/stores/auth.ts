import type { AuthSession, AuthSessionStore } from '@/shared/http/session'

import { computed, defineStore, ref } from 'wevu'

import './manager'

export const useAuthStore = defineStore('auth', () => {
  const session = ref<AuthSession | null>(null)
  const initialized = ref(false)

  const isAuthenticated = computed(() => Boolean(
    session.value
    && session.value.accessToken
    && session.value.expiresAt > Date.now(),
  ))

  function setSession(nextSession: AuthSession): void {
    session.value = { ...nextSession }
  }

  function clearSession(): void {
    session.value = null
  }

  function getAccessToken(): string | undefined {
    return session.value?.accessToken
  }

  function getRefreshToken(): string | undefined {
    return session.value?.refreshToken
  }

  function markInitialized(): void {
    initialized.value = true
  }

  return {
    session,
    initialized,
    isAuthenticated,
    setSession,
    clearSession,
    getAccessToken,
    getRefreshToken,
    markInitialized,
  }
})

export function createAuthSessionStoreBridge(): AuthSessionStore {
  return {
    getAccessToken: () => useAuthStore().getAccessToken(),
    getRefreshToken: () => useAuthStore().getRefreshToken(),
    setSession: session => useAuthStore().setSession(session),
    clearSession: () => useAuthStore().clearSession(),
  }
}

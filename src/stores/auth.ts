import type { AuthSession, EnterpriseProfile } from '@/features/auth/models'

import { computed, defineStore, ref } from 'wevu'
import { clearPrivateQueryCaches } from '@/shared/query/private-cache'

import './manager'

export const useAuthStore = defineStore('auth', () => {
  const session = ref<AuthSession | null>(null)
  const initialized = ref(false)

  // Reactive presentation state only. Authorization decisions must call
  // ensureActiveSession(), which evaluates expiration against a fresh clock.
  const isAuthenticated = computed(() => Boolean(session.value?.token))
  const enterprise = computed<EnterpriseProfile | undefined>(() => (
    isAuthenticated.value ? session.value?.enterprise : undefined
  ))

  function setSession(nextSession: AuthSession): void {
    const currentEnterprise = session.value?.enterprise
    if (
      currentEnterprise
      && (
        currentEnterprise.id !== nextSession.enterprise.id
        || currentEnterprise.username !== nextSession.enterprise.username
        || currentEnterprise.creditcode !== nextSession.enterprise.creditcode
      )
    ) {
      clearPrivateQueryCaches()
    }

    session.value = {
      ...nextSession,
      enterprise: { ...nextSession.enterprise },
    }
  }

  function clearSession(): void {
    clearPrivateQueryCaches()
    session.value = null
  }

  function isSessionActive(now = Date.now()): boolean {
    const current = session.value
    return Boolean(current && current.token && current.expiresAt > now)
  }

  function ensureActiveSession(now = Date.now()): boolean {
    if (isSessionActive(now)) {
      return true
    }
    if (session.value) {
      clearSession()
    }
    return false
  }

  function markInitialized(): void {
    initialized.value = true
  }

  return {
    session,
    initialized,
    isAuthenticated,
    enterprise,
    isSessionActive,
    ensureActiveSession,
    setSession,
    clearSession,
    markInitialized,
  }
})

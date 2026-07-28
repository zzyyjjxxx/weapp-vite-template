import type { AuthSession, EnterpriseProfile } from '@/features/auth/models'

import { computed, defineStore, ref } from 'wevu'

import './manager'

export const useAuthStore = defineStore('auth', () => {
  const session = ref<AuthSession | null>(null)
  const initialized = ref(false)

  const isAuthenticated = computed(() => Boolean(
    session.value
    && session.value.token
    && session.value.expiresAt > Date.now(),
  ))
  const enterprise = computed<EnterpriseProfile | undefined>(() => (
    isAuthenticated.value ? session.value?.enterprise : undefined
  ))

  function setSession(nextSession: AuthSession): void {
    session.value = {
      ...nextSession,
      enterprise: { ...nextSession.enterprise },
    }
  }

  function clearSession(): void {
    session.value = null
  }

  function markInitialized(): void {
    initialized.value = true
  }

  return {
    session,
    initialized,
    isAuthenticated,
    enterprise,
    setSession,
    clearSession,
    markInitialized,
  }
})

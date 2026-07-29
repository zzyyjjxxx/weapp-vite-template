import type { AppRoutePath } from './types'

import { onLoad, onShow, ref } from 'wevu'
import { useAuthStore } from '@/stores/auth'
import { buildLoginRedirect, replaceUrl } from './navigation'

export interface ActiveSessionGate {
  ensureActiveSession: () => boolean
}

export type Redirect = (url: string) => Promise<void>

export async function guardProtectedPage(
  auth: ActiveSessionGate,
  returnTo: AppRoutePath,
  redirect: Redirect = replaceUrl,
): Promise<boolean> {
  if (auth.ensureActiveSession()) {
    return true
  }
  await redirect(buildLoginRedirect(returnTo))
  return false
}

export function useProtectedPage(path: AppRoutePath) {
  const auth = useAuthStore()
  const authorized = ref(auth.ensureActiveSession())

  async function checkAccess(): Promise<void> {
    const active = auth.ensureActiveSession()
    authorized.value = active
    if (!active) {
      await replaceUrl(buildLoginRedirect(path))
    }
  }

  onLoad(() => {
    void checkAccess()
  })
  onShow(() => {
    void checkAccess()
  })

  return { authorized, checkAccess }
}

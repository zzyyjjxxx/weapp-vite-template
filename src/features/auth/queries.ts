import type { AuthSession, LoginInput } from './models'

import type { UseMutationResult } from '@/shared/query/types'
import { useMutation } from '@/shared/query/use-mutation'
import { useAuthStore } from '@/stores/auth'
import { authKeys } from './query-keys'
import { login } from './service'

export function useLoginMutation(): UseMutationResult<
  AuthSession,
  Error,
  LoginInput,
  unknown
> {
  const auth = useAuthStore()

  return useMutation<AuthSession, Error, LoginInput, unknown>(() => ({
    mutationKey: [...authKeys.all, 'login'],
    mutationFn: input => login(input),
    onSuccess: session => auth.setSession(session),
  }))
}

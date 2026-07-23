import type { AuthSession, LoginInput, User } from './models'

import type { UseMutationResult, UseQueryResult } from '@/shared/query/types'
import { PRIVATE_QUERY_SCOPE } from '@/shared/query/private-cache'
import { useMutation } from '@/shared/query/use-mutation'
import { useQuery } from '@/shared/query/use-query'
import { useAuthStore } from '@/stores/auth'
import { authKeys } from './query-keys'
import { getProfile, login } from './service'

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

export function useProfileQuery(): UseQueryResult<User, Error> {
  const auth = useAuthStore()

  return useQuery<User, Error, User, ReturnType<typeof authKeys.profile>>(() => ({
    queryKey: authKeys.profile(),
    queryFn: ({ signal }) => getProfile({ signal }),
    enabled: () => auth.isAuthenticated.value,
    meta: {
      scope: PRIVATE_QUERY_SCOPE,
    },
  }))
}

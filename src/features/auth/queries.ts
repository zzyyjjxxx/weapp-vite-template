import type { User } from './models'

import type { UseQueryResult } from '@/shared/query/types'
import { PRIVATE_QUERY_SCOPE } from '@/shared/query/private-cache'
import { useQuery } from '@/shared/query/use-query'
import { useAuthStore } from '@/stores/auth'
import { authKeys } from './query-keys'
import { getProfile } from './service'

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

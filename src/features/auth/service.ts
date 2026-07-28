import type { AuthSession, LoginInput } from './models'

import { getAuthRepository } from './repository'

export function login(input: LoginInput): Promise<AuthSession> {
  return getAuthRepository().login(input)
}

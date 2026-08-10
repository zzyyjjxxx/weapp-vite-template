import type { AuthSession, EnterpriseInfo, LoginInput } from './models'

import { getAuthRepository } from './repository'

export function login(input: LoginInput): Promise<AuthSession> {
  return getAuthRepository().login(input)
}

export function refresh(session: AuthSession): Promise<AuthSession> {
  const repository = getAuthRepository()
  if (!repository.refresh) {
    return Promise.reject(new Error('当前认证适配器不支持刷新登录状态'))
  }
  return repository.refresh(session)
}

export function getEnterpriseInfo(token: string): Promise<EnterpriseInfo> {
  const repository = getAuthRepository()
  if (!repository.getInfo) {
    return Promise.reject(new Error('当前认证适配器不支持获取企业信息'))
  }
  return repository.getInfo(token)
}

import type { LoginInput, User } from './models'
import type { AuthSession } from '@/shared/http/session'

import type { RequestOptions } from '@/shared/http/types'
import { request as httpRequest } from '@/shared/http/client'

type Request = typeof httpRequest

export interface ServiceRequestOptions {
  request?: Request
  signal?: AbortSignal
}

export function login(
  input: LoginInput,
  options: ServiceRequestOptions = {},
): Promise<AuthSession> {
  const request = options.request ?? httpRequest
  const requestOptions: RequestOptions<LoginInput> = {
    path: '/auth/login',
    method: 'POST',
    body: input,
    auth: 'none',
    signal: options.signal,
  }
  return request<AuthSession, LoginInput>(requestOptions)
}

export function refresh(
  refreshToken: string,
  options: ServiceRequestOptions = {},
): Promise<AuthSession> {
  const request = options.request ?? httpRequest
  const requestOptions: RequestOptions<{ refreshToken: string }> = {
    path: '/auth/refresh',
    method: 'POST',
    body: { refreshToken },
    auth: 'none',
    signal: options.signal,
  }
  return request<AuthSession, { refreshToken: string }>(requestOptions)
}

export function getProfile(
  options: ServiceRequestOptions = {},
): Promise<User> {
  const request = options.request ?? httpRequest
  const requestOptions: RequestOptions = {
    path: '/profile',
    method: 'GET',
    auth: 'required',
    signal: options.signal,
  }
  return request<User>(requestOptions)
}

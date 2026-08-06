import type { QueryClient } from '@tanstack/query-core'
import type {
  LandDemandForm,
  LandDemandRecord,
  LandDemandStatus,
} from './models'
import type { LandDemandRepository, VerificationChallenge } from './repository'
import type { UseMutationResult, UseQueryResult } from '@/shared/query/types'

import { queryClient } from '@/shared/query/client'
import { PRIVATE_QUERY_SCOPE } from '@/shared/query/private-cache'
import { useMutation } from '@/shared/query/use-mutation'
import { useQuery } from '@/shared/query/use-query'
import { landDemandKeys } from './query-keys'
import {
  getLandDemandInfo,
  saveLandDemand,
  sendVerificationCode,
  updateLandDemand,
  verifyVerificationCode,
} from './service'

interface QueryOptions {
  client?: QueryClient
  repository?: LandDemandRepository
}

export interface SaveLandDemandVariables {
  form: LandDemandForm
  status: LandDemandStatus
  updateuser?: string
}

export interface UpdateLandDemandVariables extends SaveLandDemandVariables {
  original: LandDemandRecord
}

export interface VerificationCodeVariables {
  phone: string
  code: string
}

function cachePrivateRecord(client: QueryClient, record: LandDemandRecord): void {
  const queryKey = landDemandKeys.detail(record.creditcode)
  client.setQueryDefaults(queryKey, {
    meta: { scope: PRIVATE_QUERY_SCOPE },
  })
  client.setQueryData(queryKey, record)
}

export function useLandDemandQuery(
  creditcode: string,
  options: QueryOptions = {},
): UseQueryResult<LandDemandRecord | null, Error> {
  return useQuery(() => ({
    queryKey: landDemandKeys.detail(creditcode),
    queryFn: async ({ signal }) => {
      const record = await getLandDemandInfo(creditcode, {
        repository: options.repository,
        signal,
      })
      return record ?? null
    },
    enabled: Boolean(creditcode),
    meta: { scope: 'private' },
  }), options.client ?? queryClient)
}

export function useSaveLandDemandMutation(
  options: QueryOptions = {},
): UseMutationResult<LandDemandRecord, Error, SaveLandDemandVariables, unknown> {
  const client = options.client ?? queryClient
  return useMutation(() => ({
    mutationKey: [...landDemandKeys.all, 'save'],
    mutationFn: variables => saveLandDemand(variables.form, variables.status, {
      repository: options.repository,
      updateuser: variables.updateuser,
    }),
    onSuccess: record => cachePrivateRecord(client, record),
    retry: 0,
  }), client)
}

export function useUpdateLandDemandMutation(
  options: QueryOptions = {},
): UseMutationResult<LandDemandRecord, Error, UpdateLandDemandVariables, unknown> {
  const client = options.client ?? queryClient
  return useMutation(() => ({
    mutationKey: [...landDemandKeys.all, 'update'],
    mutationFn: variables => updateLandDemand(variables.form, variables.original, variables.status, {
      repository: options.repository,
      updateuser: variables.updateuser,
    }),
    onSuccess: record => cachePrivateRecord(client, record),
    retry: 0,
  }), client)
}

export function useSendVerificationCodeMutation(
  options: QueryOptions = {},
): UseMutationResult<VerificationChallenge, Error, string, unknown> {
  return useMutation(() => ({
    mutationKey: [...landDemandKeys.all, 'send-verification-code'],
    mutationFn: phone => sendVerificationCode(phone, { repository: options.repository }),
    retry: 0,
  }), options.client ?? queryClient)
}

export function useVerifyVerificationCodeMutation(
  options: QueryOptions = {},
): UseMutationResult<void, Error, VerificationCodeVariables, unknown> {
  return useMutation(() => ({
    mutationKey: [...landDemandKeys.all, 'verify-verification-code'],
    mutationFn: variables => verifyVerificationCode(variables.phone, variables.code, {
      repository: options.repository,
    }),
    retry: 0,
  }), options.client ?? queryClient)
}

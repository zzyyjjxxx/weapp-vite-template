import type {
  FieldError,
  LandDemandForm,
  LandDemandRecord,
  VerificationChallenge,
} from './models'

import { validateSubmission } from './validation'

export interface SubmitControllerDeps {
  sendCode: (phone: string) => Promise<VerificationChallenge>
  verifyCode: (phone: string, code: string) => Promise<void>
  persist: (status: '1') => Promise<LandDemandRecord>
}

export interface RequestCodeResult {
  errors: FieldError[]
  challenge?: VerificationChallenge
  acceptanceError?: string
}

export function createSubmitController(deps: SubmitControllerDeps): {
  requestCode: (form: LandDemandForm, accepted: boolean) => Promise<RequestCodeResult>
  submitCode: (phone: string, code: string) => Promise<LandDemandRecord>
} {
  let verified: { phone: string, code: string } | undefined
  let requestInFlight: Promise<RequestCodeResult> | undefined
  let submissionInFlight: {
    phone: string
    code: string
    promise: Promise<LandDemandRecord>
  } | undefined

  async function requestCode(form: LandDemandForm, accepted: boolean): Promise<RequestCodeResult> {
    const errors = validateSubmission(form)
    const acceptanceError = accepted
      ? undefined
      : '请阅读并同意信息真实性承诺'
    if (errors.length > 0 || acceptanceError) {
      return { errors, acceptanceError }
    }

    const challenge = await deps.sendCode(form.phone)
    verified = undefined
    return { errors: [], challenge }
  }

  async function submitCode(phone: string, code: string): Promise<LandDemandRecord> {
    if (verified?.phone !== phone || verified.code !== code) {
      await deps.verifyCode(phone, code)
      verified = { phone, code }
    }
    return deps.persist('1')
  }

  return {
    requestCode(form, accepted) {
      if (requestInFlight) {
        return requestInFlight
      }

      const promise = requestCode(form, accepted)
      requestInFlight = promise
      void promise.finally(() => {
        if (requestInFlight === promise) {
          requestInFlight = undefined
        }
      }).catch(() => undefined)
      return promise
    },
    submitCode(phone, code) {
      if (
        submissionInFlight?.phone === phone
        && submissionInFlight.code === code
      ) {
        return submissionInFlight.promise
      }

      const promise = submitCode(phone, code)
      submissionInFlight = { phone, code, promise }
      void promise.finally(() => {
        if (submissionInFlight?.promise === promise) {
          submissionInFlight = undefined
        }
      }).catch(() => undefined)
      return promise
    },
  }
}

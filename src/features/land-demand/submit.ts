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

export interface RequestCodeOptions {
  existingChallenge?: VerificationChallenge
  forceResend?: boolean
}

export function createSubmitController(deps: SubmitControllerDeps): {
  requestCode: (
    form: LandDemandForm,
    accepted: boolean,
    options?: RequestCodeOptions,
  ) => Promise<RequestCodeResult>
  submitCode: (phone: string, code: string) => Promise<LandDemandRecord>
} {
  let verified: { phone: string, code: string } | undefined

  return {
    async requestCode(form, accepted, options = {}) {
      const errors = validateSubmission(form)
      const acceptanceError = accepted
        ? undefined
        : '请阅读并同意信息真实性承诺'
      if (errors.length > 0 || acceptanceError) {
        return { errors, acceptanceError }
      }

      const existingChallenge = options.existingChallenge
      const isSamePhone = existingChallenge?.phone === form.phone
      const isActive = isSamePhone && Date.now() < (existingChallenge?.expiresAt ?? 0)
      const isCoolingDown = isActive && Date.now() < (existingChallenge?.retryAt ?? 0)
      if (existingChallenge && isActive && (!options.forceResend || isCoolingDown)) {
        return { errors: [], challenge: existingChallenge }
      }

      const challenge = await deps.sendCode(form.phone)
      verified = undefined
      return { errors: [], challenge }
    },
    async submitCode(phone, code) {
      if (!/^\d{6}$/.test(code)) {
        throw new Error('请输入6位验证码')
      }
      if (verified?.phone !== phone || verified.code !== code) {
        await deps.verifyCode(phone, code)
        verified = { phone, code }
      }
      return deps.persist('1')
    },
  }
}

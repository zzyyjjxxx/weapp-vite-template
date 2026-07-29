import type { FieldError } from './models'

export type LandDemandStep = 1 | 2 | 3 | 4 | 5

export function previousStep(step: LandDemandStep): LandDemandStep {
  return step === 1 ? 1 : (step - 1) as LandDemandStep
}

export function nextStep(step: LandDemandStep): LandDemandStep {
  return step === 5 ? 5 : (step + 1) as LandDemandStep
}

export function resolveSubmissionTarget(
  errors: readonly FieldError[],
): 1 | 2 | 3 | 4 | undefined {
  for (const step of [1, 2, 3, 4] as const) {
    if (errors.some(error => error.step === step)) {
      return step
    }
  }

  return undefined
}

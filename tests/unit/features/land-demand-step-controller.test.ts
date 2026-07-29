import type { FieldError } from '@/features/land-demand/models'

import { describe, expect, it } from 'vitest'
import {
  nextStep,
  previousStep,
  resolveSubmissionTarget,
} from '@/features/land-demand/step-controller'

describe('land demand step controller', () => {
  it('keeps previous and next navigation within the wizard bounds', () => {
    expect(previousStep(1)).toBe(1)
    expect(previousStep(4)).toBe(3)
    expect(nextStep(4)).toBe(5)
    expect(nextStep(5)).toBe(5)
  })

  it('navigates to the first step containing a submission error', () => {
    const result = resolveSubmissionTarget([{ field: 'pred_tax', step: 3, message: '必填' }])

    expect(result).toBe(3)
  })

  it('uses wizard order rather than error array order', () => {
    const errors: FieldError[] = [
      { field: 'phone', step: 4, message: '必填' },
      { field: 'area', step: 2, message: '必填' },
    ]

    expect(resolveSubmissionTarget(errors)).toBe(2)
    expect(resolveSubmissionTarget([])).toBeUndefined()
  })
})

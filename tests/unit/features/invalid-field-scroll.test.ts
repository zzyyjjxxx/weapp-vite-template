import type { FieldError } from '@/features/land-demand/models'

import { describe, expect, it } from 'vitest'
import { findFirstInvalidField } from '@/features/land-demand/invalid-field-scroll'

const fieldTestIds = {
  is_deploy: 'is-deploy-field',
  deploy_park: 'deploy-park-field',
  is_specialuse: 'is-specialuse-field',
} as const

function fieldError(field: FieldError['field']): FieldError {
  return { field, step: 2, message: '此项必填' }
}

describe('invalid field scroll selection', () => {
  it('selects the first invalid field in validation order', () => {
    expect(findFirstInvalidField(
      [fieldError('deploy_park'), fieldError('is_specialuse')],
      fieldTestIds,
    )?.field).toBe('deploy_park')
  })
})

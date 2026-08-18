import type { FieldError } from './models'

import { nextTick, watch } from 'wevu'
import {
  createPageScrollAdapter,
  scrollPageToField,
} from '@/platform/page-scroll'

async function scrollInvalidField(
  testId: string,
  componentId: string,
  pageScrollApi: ReturnType<typeof createPageScrollAdapter>,
): Promise<void> {
  await nextTick()
  await new Promise<void>(resolve => setTimeout(resolve, 0))
  await scrollPageToField(
    `#${componentId} >>> #${testId}`,
    pageScrollApi,
  )
}

export function findFirstInvalidField(
  currentErrors: readonly FieldError[],
  fieldTestIds: Partial<Record<FieldError['field'], string>>,
): FieldError | undefined {
  return currentErrors.find(error => Boolean(fieldTestIds[error.field]))
}

export function useInvalidFieldScroll(
  errors: () => readonly FieldError[],
  scrollRequest: () => number,
  fieldTestIds: Partial<Record<FieldError['field'], string>>,
  componentId: string,
  active: () => boolean = () => true,
): void {
  const pageScrollApi = createPageScrollAdapter()

  watch(scrollRequest, () => {
    if (!active()) {
      return
    }

    const firstInvalidField = findFirstInvalidField(errors(), fieldTestIds)
    const testId = firstInvalidField
      ? fieldTestIds[firstInvalidField.field]
      : undefined
    if (!testId) {
      return
    }

    void scrollInvalidField(testId, componentId, pageScrollApi)
  })
}

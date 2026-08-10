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

export function useInvalidFieldScroll(
  errors: () => readonly FieldError[],
  fieldTestIds: Partial<Record<FieldError['field'], string>>,
  componentId: string,
): void {
  const pageScrollApi = createPageScrollAdapter()

  watch(errors, (currentErrors) => {
    const firstInvalidField = currentErrors.find(error => fieldTestIds[error.field])
    const testId = firstInvalidField
      ? fieldTestIds[firstInvalidField.field]
      : undefined
    if (!testId) {
      return
    }

    void scrollInvalidField(testId, componentId, pageScrollApi)
  }, { immediate: true })
}

import { wpi } from 'wevu/api'

export interface PageScrollToOptions {
  duration?: number
  offsetTop?: number
  scrollTop?: number
  selector?: string
}

export interface PageScrollAdapter {
  pageScrollTo: (options: PageScrollToOptions) => unknown
}

export function createPageScrollAdapter(): PageScrollAdapter {
  return {
    pageScrollTo: wpi.pageScrollTo,
  }
}

export function scrollPageToTop(
  pageScrollApi: PageScrollAdapter = wpi,
): void {
  try {
    void Promise.resolve(pageScrollApi.pageScrollTo({
      scrollTop: 0,
      duration: 0,
    })).catch(() => undefined)
  }
  catch {
    // Some host runtimes do not expose page scrolling during startup.
  }
}

/**
 * Scroll the first invalid field into view after the validation message has rendered.
 * The selector is page-scoped and may use WeChat's `>>>` cross-component syntax.
 * The target is kept below the page header so the field and its error remain visible.
 */
export function scrollPageToField(
  selector: string,
  pageScrollApi: PageScrollAdapter = wpi,
  topOffset = 32,
): Promise<void> {
  if (!selector) {
    return Promise.resolve()
  }

  try {
    return Promise.resolve(pageScrollApi.pageScrollTo({
      selector,
      offsetTop: -Math.abs(topOffset),
      duration: 180,
    })).then(() => undefined).catch(() => undefined)
  }
  catch {
    // Some host runtimes do not expose page scrolling during startup.
    return Promise.resolve()
  }
}

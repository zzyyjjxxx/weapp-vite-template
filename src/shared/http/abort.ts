import { AbortControllerPolyfill } from 'wevu/web-apis'

interface MergedSignal {
  signal: AbortSignal
  cleanup: () => void
  didTimeout: () => boolean
}

function createAbortController(): AbortController {
  const RuntimeAbortController = globalThis.AbortController
  if (typeof RuntimeAbortController === 'function') {
    try {
      return new RuntimeAbortController()
    }
    catch {
      // Some mini-program hosts expose a placeholder before runtime globals initialize.
    }
  }

  return new AbortControllerPolyfill() as unknown as AbortController
}

export function mergeSignalWithTimeout(
  external: AbortSignal | undefined,
  timeoutMs: number,
): MergedSignal {
  const controller = createAbortController()
  let timedOut = false

  const abortFromExternal = (): void => {
    controller.abort()
  }

  if (external?.aborted) {
    controller.abort()
  }
  else {
    external?.addEventListener('abort', abortFromExternal, { once: true })
  }

  const timer = setTimeout(() => {
    timedOut = true
    controller.abort()
  }, timeoutMs)

  return {
    signal: controller.signal,
    cleanup() {
      clearTimeout(timer)
      external?.removeEventListener('abort', abortFromExternal)
    },
    didTimeout: () => timedOut,
  }
}

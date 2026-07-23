interface MergedSignal {
  signal: AbortSignal
  cleanup: () => void
  didTimeout: () => boolean
}

export function mergeSignalWithTimeout(
  external: AbortSignal | undefined,
  timeoutMs: number,
): MergedSignal {
  const controller = new AbortController()
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

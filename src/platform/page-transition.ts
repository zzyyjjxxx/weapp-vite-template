import { nextTick, ref } from 'wevu'

export function usePageTransitionLoading() {
  const pending = ref(false)

  async function run<T>(action: () => Promise<T>): Promise<T | undefined> {
    if (pending.value) {
      return undefined
    }

    pending.value = true
    try {
      await nextTick()
      return await action()
    }
    finally {
      pending.value = false
    }
  }

  return { pending, run }
}

import { onlineManager } from '@tanstack/query-core'
import { onUnmounted } from 'wevu'

export interface QueryLifecycleAdapter {
  onUnmounted: (callback: () => void) => void
}

const defaultQueryLifecycleAdapter: QueryLifecycleAdapter = {
  onUnmounted: callback => onUnmounted(callback),
}

let queryLifecycleAdapter = defaultQueryLifecycleAdapter

export function configureQueryLifecycleAdapter(adapter: QueryLifecycleAdapter): void {
  queryLifecycleAdapter = adapter
}

export function resetQueryLifecycleAdapter(): void {
  queryLifecycleAdapter = defaultQueryLifecycleAdapter
}

export function registerQueryCleanup(callback: () => void): void {
  queryLifecycleAdapter.onUnmounted(callback)
}

export interface QueryOnlineAdapter {
  isOnline: () => boolean
  subscribe: (listener: (online: boolean) => void) => () => void
}

const alwaysOnlineAdapter: QueryOnlineAdapter = {
  isOnline: () => true,
  subscribe: () => () => undefined,
}

export function setupQueryOnlineManager(
  adapter: QueryOnlineAdapter = alwaysOnlineAdapter,
): () => void {
  onlineManager.setEventListener((setOnline) => {
    setOnline(adapter.isOnline())
    return adapter.subscribe(setOnline)
  })

  return () => {
    onlineManager.setEventListener(() => undefined)
  }
}

import type { StorageAdapter } from '@/platform/storage'
import { createWpiStorageAdapter } from '@/platform/storage'

import { useAuthStore } from '../auth'
import { storeManager } from '../manager'
import { createStoreLoggingPlugin } from './logging'
import { createPersistencePlugin } from './persistence'

let initialized = false

export function setupStorePlugins(options: { storage?: StorageAdapter } = {}): void {
  if (initialized) {
    return
  }
  initialized = true

  const storage = options.storage ?? createWpiStorageAdapter()
  storeManager
    .use(createPersistencePlugin(storage))
    .use(createStoreLoggingPlugin())

  useAuthStore()
}

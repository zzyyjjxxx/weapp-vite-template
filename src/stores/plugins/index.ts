import type { StorageAdapter } from '@/platform/storage'
import { createWpiStorageAdapter } from '@/platform/storage'

import { useAuthStore } from '../auth'
import { storeManager } from '../manager'
import { createStoreLoggingPlugin } from './logging'
import {
  createPersistencePlugin,
  readPersistedAuthSession,
} from './persistence'

let initialized = false
let persistenceStorage: StorageAdapter | undefined

export function setupStorePlugins(options: { storage?: StorageAdapter } = {}): void {
  if (!initialized) {
    initialized = true

    const storage = options.storage ?? createWpiStorageAdapter()
    persistenceStorage = storage
    storeManager
      .use(createPersistencePlugin(storage))
      .use(createStoreLoggingPlugin())
  }

  // App instances can be recreated while the module graph stays alive in
  // DevTools. Always resolve the auth store so persistence can hydrate the
  // active store instance even though plugins are registered only once.
  const auth = useAuthStore()
  const persistedSession = persistenceStorage
    ? readPersistedAuthSession(persistenceStorage)
    : undefined
  if (!auth.session.value && persistedSession) {
    auth.setSession(persistedSession)
  }
}

import { wpi } from 'wevu/api'

export interface StorageAdapter {
  get: <T>(key: string) => T | undefined
  set: <T>(key: string, value: T) => void
  remove: (key: string) => void
}

export function createWpiStorageAdapter(): StorageAdapter {
  return {
    get<T>(key: string) {
      try {
        return wpi.getStorageSync(key) as T | undefined
      }
      catch {
        return undefined
      }
    },
    set<T>(key: string, value: T) {
      try {
        wpi.setStorageSync(key, value)
      }
      catch {
        // Storage is optional for the local test scaffold.
      }
    },
    remove(key) {
      try {
        wpi.removeStorageSync(key)
      }
      catch {
        // Storage is optional for the local test scaffold.
      }
    },
  }
}

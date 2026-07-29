import { wpi } from 'wevu/api'

export interface StorageAdapter {
  get: <T>(key: string) => T | undefined
  set: <T>(key: string, value: T) => void
  remove: (key: string) => void
}

export interface WpiStorageApi {
  getStorageSync: (key: string) => unknown
  setStorageSync: (key: string, value: unknown) => unknown
  removeStorageSync: (key: string) => unknown
}

export function createWpiStorageAdapter(
  storageApi: WpiStorageApi = wpi as unknown as WpiStorageApi,
): StorageAdapter {
  return {
    get<T>(key: string) {
      return storageApi.getStorageSync(key) as T | undefined
    },
    set<T>(key: string, value: T) {
      storageApi.setStorageSync(key, value)
    },
    remove(key) {
      storageApi.removeStorageSync(key)
    },
  }
}

export interface MemoryStorage {
  clear: () => void
  get: <T>(key: string) => T | undefined
  keys: () => string[]
  remove: (key: string) => void
  set: <T>(key: string, value: T) => void
}

export function createMemoryStorage(): MemoryStorage {
  const values = new Map<string, unknown>()

  return {
    clear: () => values.clear(),
    get: <T>(key: string) => values.get(key) as T | undefined,
    keys: () => [...values.keys()],
    remove: (key: string) => { values.delete(key) },
    set: <T>(key: string, value: T) => { values.set(key, value) },
  }
}

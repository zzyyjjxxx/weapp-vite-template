import { logger } from '@/shared/logger'

interface StoreActionContext {
  name: string
  store: { $id: string }
  after: (callback: (result: unknown) => void) => void
  onError: (callback: (error: unknown) => void) => void
}

interface LoggingStore {
  $id: string
  $onAction: (callback: (context: StoreActionContext) => void) => () => void
}

export function createStoreLoggingPlugin() {
  return ({ store }: { store: LoggingStore }): void => {
    store.$onAction(({ name, after, onError }) => {
      logger.debug('store.action.started', {
        route: store.$id,
        action: name,
      })
      after(() => {
        logger.debug('store.action.completed', {
          route: store.$id,
          action: name,
        })
      })
      onError((error) => {
        logger.warn('store.action.failed', {
          route: store.$id,
          action: name,
        }, error)
      })
    })
  }
}

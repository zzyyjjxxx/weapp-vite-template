import type { QueryOnlineAdapter } from '@/shared/query/lifecycle'

import { wpi } from 'wevu/api'

export function createNetworkStatusAdapter(): QueryOnlineAdapter {
  return {
    isOnline: () => true,
    subscribe(listener) {
      let active = true
      const handler: Parameters<typeof wpi.onNetworkStatusChange>[0] = (result) => {
        if (active) {
          listener(result.isConnected)
        }
      }

      try {
        wpi.onNetworkStatusChange(handler)
        void wpi.getNetworkType().then((result) => {
          if (active) {
            listener(result.networkType !== 'none')
          }
        }).catch(() => undefined)
      }
      catch {
        // The adapter remains online when the host does not expose network APIs.
      }

      return () => {
        active = false
        try {
          wpi.offNetworkStatusChange(
            handler as unknown as Parameters<typeof wpi.offNetworkStatusChange>[0],
          )
        }
        catch {
          // Some hosts expose the listener API without an off method.
        }
      }
    },
  }
}

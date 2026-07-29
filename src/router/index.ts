import type {
  RouteLocationNormalizedLoaded,
  RouterNavigation,
} from 'wevu/router'
import type { RouteMeta } from './types'

import { createRouter } from 'wevu/router'

import { useAuthStore } from '@/stores/auth'
import { buildLoginRedirect } from './navigation'
import { resolveRouteMeta } from './route-meta'

let router: RouterNavigation | undefined

export function setupRouter(): RouterNavigation {
  if (router) {
    return router
  }

  router = createRouter()
  router.beforeEach((to: RouteLocationNormalizedLoaded | undefined) => {
    if (!to) {
      return
    }

    const meta: RouteMeta | undefined = resolveRouteMeta(to.path)
    const auth = useAuthStore()
    if (meta?.auth && !auth.ensureActiveSession()) {
      return buildLoginRedirect(to.fullPath)
    }
  })
  return router
}

export function getRouter(): RouterNavigation {
  return setupRouter()
}

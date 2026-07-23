import type {
  RouteLocationNormalizedLoaded,
  RouterNavigation,
} from 'wevu/router'
import type { RouteMeta } from './types'

import { createRouter } from 'wevu/router'

import { useAuthStore } from '@/stores/auth'
import { buildLoginRedirect } from './navigation'
import { routeMeta } from './route-meta'

let router: RouterNavigation | undefined

export function setupRouter(): RouterNavigation {
  if (router) {
    return router
  }

  router = createRouter({
    tabBarEntries: ['/pages/home/index', '/pages/profile/index'],
  })
  router.beforeEach((to: RouteLocationNormalizedLoaded | undefined) => {
    if (!to) {
      return
    }

    const meta: RouteMeta | undefined = routeMeta[to.path as keyof typeof routeMeta]
    const auth = useAuthStore()
    if (meta?.auth && !auth.isAuthenticated.value) {
      return buildLoginRedirect(to.fullPath)
    }
  })
  return router
}

export function getRouter(): RouterNavigation {
  return setupRouter()
}

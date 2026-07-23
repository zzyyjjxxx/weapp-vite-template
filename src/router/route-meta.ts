import type { AppRoutePath, RouteMeta } from './types'

export const routeMeta = {
  '/pages/home/index': {
    tab: true,
    analyticsName: 'home',
  },
  '/pages/profile/index': {
    tab: true,
    analyticsName: 'profile',
  },
  '/pages/login/index': {
    analyticsName: 'login',
  },
  '/pages/error/index': {
    analyticsName: 'error',
  },
  '/subpackages/order/pages/list/index': {
    auth: true,
    analyticsName: 'order_list',
  },
  '/subpackages/order/pages/detail/index': {
    auth: true,
    analyticsName: 'order_detail',
  },
} satisfies Partial<Record<AppRoutePath, RouteMeta>>

export function resolveRouteMeta(path: string): RouteMeta | undefined {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return routeMeta[normalizedPath as keyof typeof routeMeta]
}

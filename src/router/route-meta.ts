import type { AppRoutePath, RouteMeta } from './types'

export const routeMeta = {
  '/pages/home/index': {
    tab: true,
    analyticsName: 'home',
  },
  '/pages/login/index': {
    analyticsName: 'login',
  },
  '/pages/error/index': {
    analyticsName: 'error',
  },
} satisfies Partial<Record<AppRoutePath, RouteMeta>>

export function resolveRouteMeta(path: string): RouteMeta | undefined {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return routeMeta[normalizedPath as keyof typeof routeMeta]
}

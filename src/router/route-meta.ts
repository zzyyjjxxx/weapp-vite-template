import type { AppRoutePath, RouteMeta } from './types'

export const routeMeta = {
  '/pages/home/index': {
    auth: true,
    analyticsName: 'home',
  },
  '/pages/login/index': {
    analyticsName: 'login',
  },
  '/pages/error/index': {
    analyticsName: 'error',
  },
  '/pages/land-demand/index': {
    auth: true,
    analyticsName: 'land-demand',
  },
  '/pages/land-demand/success': {
    auth: true,
    analyticsName: 'land-demand-success',
  },
} satisfies Partial<Record<AppRoutePath, RouteMeta>>

export function resolveRouteMeta(path: string): RouteMeta | undefined {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return routeMeta[normalizedPath as keyof typeof routeMeta]
}

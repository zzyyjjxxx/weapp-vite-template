import type { AutoRoutes } from 'weapp-vite/auto-routes'

export type GeneratedRoutePath = AutoRoutes['entries'][number]
export type AppRoutePath = GeneratedRoutePath
  | '/pages/home/index'
  | '/pages/login/index'
  | '/pages/error/index'
  | '/pages/land-demand/index'
  | '/pages/land-demand/success'

export interface RouteMeta {
  auth?: boolean
  tab?: boolean
  analyticsName?: string
  requiredFeature?: string
}

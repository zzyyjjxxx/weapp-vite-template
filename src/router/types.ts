import type { AutoRoutes } from 'weapp-vite/auto-routes'

export type GeneratedRoutePath = AutoRoutes['entries'][number]
export type AppRoutePath = GeneratedRoutePath
  | '/pages/home/index'
  | '/pages/profile/index'
  | '/pages/login/index'
  | '/pages/error/index'
  | '/subpackages/order/pages/list/index'
  | '/subpackages/order/pages/detail/index'

export interface RouteMeta {
  auth?: boolean
  tab?: boolean
  analyticsName?: string
  requiredFeature?: string
}

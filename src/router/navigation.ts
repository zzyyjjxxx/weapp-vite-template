import type { RouteQuery } from './query'
import type { AppRoutePath, RouteMeta } from './types'

import { useRouter } from 'wevu/router'
import { encodeQuery } from './query'
import { routeMeta } from './route-meta'

export interface NavigationAdapter {
  switchTab: (path: AppRoutePath) => Promise<void>
  push: (url: string) => Promise<void>
  replace: (url: string) => Promise<void>
}

let navigationAdapter: NavigationAdapter | undefined

function createDefaultNavigationAdapter(): NavigationAdapter {
  return {
    switchTab: async (path) => {
      await useRouter().replace(path)
    },
    push: async (url) => {
      await useRouter().push(url)
    },
    replace: async (url) => {
      await useRouter().replace(url)
    },
  }
}

function getNavigationAdapter(): NavigationAdapter {
  return navigationAdapter ?? createDefaultNavigationAdapter()
}

export function configureNavigationAdapter(adapter: NavigationAdapter | undefined): void {
  navigationAdapter = adapter
}

function isTabRoute(path: AppRoutePath): boolean {
  const meta: RouteMeta | undefined = routeMeta[path as keyof typeof routeMeta]
  return meta?.tab === true
}

export async function navigate(path: AppRoutePath, query?: RouteQuery): Promise<void> {
  const queryString = encodeQuery(query)
  if (isTabRoute(path)) {
    if (queryString) {
      throw new Error('Tab 路由不支持 Query 参数')
    }
    await getNavigationAdapter().switchTab(path)
    return
  }

  await getNavigationAdapter().push(`${path}${queryString}`)
}

export async function replace(path: AppRoutePath, query?: RouteQuery): Promise<void> {
  const queryString = encodeQuery(query)
  if (isTabRoute(path)) {
    if (queryString) {
      throw new Error('Tab 路由不支持 Query 参数')
    }
    await getNavigationAdapter().switchTab(path)
    return
  }
  await getNavigationAdapter().replace(`${path}${queryString}`)
}

export async function replaceUrl(url: string): Promise<void> {
  const path = url.split('?')[0] as AppRoutePath
  if (isTabRoute(path)) {
    if (url.includes('?')) {
      throw new Error('Tab 路由不支持 Query 参数')
    }
    await getNavigationAdapter().switchTab(path)
    return
  }
  await getNavigationAdapter().replace(url)
}

export function buildLoginRedirect(returnTo: string): string {
  const loginPath = '/pages/login/index'
  if (returnTo === loginPath || returnTo.startsWith(`${loginPath}?`)) {
    return loginPath
  }
  return `${loginPath}?returnTo=${encodeURIComponent(returnTo || '/pages/home/index')}`
}

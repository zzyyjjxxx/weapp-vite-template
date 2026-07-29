import type { RouteQuery } from './query'
import type { AppRoutePath } from './types'

import { useRouter } from 'wevu/router'
import { encodeQuery } from './query'

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

export async function navigate(path: AppRoutePath, query?: RouteQuery): Promise<void> {
  const queryString = encodeQuery(query)
  await getNavigationAdapter().push(`${path}${queryString}`)
}

export async function replace(path: AppRoutePath, query?: RouteQuery): Promise<void> {
  const queryString = encodeQuery(query)
  await getNavigationAdapter().replace(`${path}${queryString}`)
}

export async function replaceUrl(url: string): Promise<void> {
  await getNavigationAdapter().replace(url)
}

export function buildLoginRedirect(returnTo: string): string {
  const loginPath = '/pages/login/index'
  if (returnTo === loginPath || returnTo.startsWith(`${loginPath}?`)) {
    return loginPath
  }
  return `${loginPath}?returnTo=${encodeURIComponent(returnTo || '/pages/home/index')}`
}

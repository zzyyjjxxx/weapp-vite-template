import type { AppIconName } from '@/components/ui/app-icon/icons'

export type AppTabPath = '/pages/home/index' | '/pages/profile/index'

export interface AppTabItem {
  label: string
  path: AppTabPath
  icon: Extract<AppIconName, 'home' | 'user-circle'>
}

export const appTabItems: readonly AppTabItem[] = [
  {
    label: '首页',
    path: '/pages/home/index',
    icon: 'home',
  },
  {
    label: '我的',
    path: '/pages/profile/index',
    icon: 'user-circle',
  },
]

export function getAppTabItem(path: string): AppTabItem | undefined {
  return appTabItems.find(item => item.path === path)
}

export function isAppTabPath(path: string): boolean {
  return getAppTabItem(path) !== undefined
}

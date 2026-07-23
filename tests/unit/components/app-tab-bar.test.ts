import { describe, expect, it } from 'vitest'

import { appTabItems, getAppTabItem, isAppTabPath } from '@/components/ui/app-tab-bar/items'

describe('app tab bar items', () => {
  it('keeps the home and profile tabs in the product order', () => {
    expect(appTabItems).toEqual([
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
    ])
  })

  it('recognizes tab routes and hides itself on non-tab routes', () => {
    expect(getAppTabItem('/pages/home/index')?.label).toBe('首页')
    expect(isAppTabPath('/pages/profile/index')).toBe(true)
    expect(getAppTabItem('/subpackages/order/pages/list/index')).toBeUndefined()
    expect(isAppTabPath('/pages/login/index')).toBe(false)
  })
})

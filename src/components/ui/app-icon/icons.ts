export const appIconNames = [
  'home',
  'user-circle',
  'lock',
  'list-check',
  'login',
] as const

export type AppIconName = typeof appIconNames[number]
export type AppIconWeight = 'Outline' | 'Filled'

type AppIconSources = Record<AppIconName, Record<AppIconWeight, string>>

const appIconSources: AppIconSources = {
  'home': {
    Outline: '/assets/icons/reicon/home-outline.svg',
    Filled: '/assets/icons/reicon/home-filled.svg',
  },
  'user-circle': {
    Outline: '/assets/icons/reicon/user-circle-outline.svg',
    Filled: '/assets/icons/reicon/user-circle-filled.svg',
  },
  'lock': {
    Outline: '/assets/icons/reicon/lock-outline.svg',
    Filled: '/assets/icons/reicon/lock-filled.svg',
  },
  'list-check': {
    Outline: '/assets/icons/reicon/list-check-outline.svg',
    Filled: '/assets/icons/reicon/list-check-filled.svg',
  },
  'login': {
    Outline: '/assets/icons/reicon/login-outline.svg',
    Filled: '/assets/icons/reicon/login-filled.svg',
  },
}

export function getAppIconSource(name: AppIconName, weight: AppIconWeight): string {
  return appIconSources[name][weight]
}

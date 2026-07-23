import { defineStore, ref } from 'wevu'

import './manager'

export type ThemePreference = 'light' | 'dark'

export const usePreferencesStore = defineStore('preferences', () => {
  const theme = ref<ThemePreference>('light')

  function setTheme(nextTheme: ThemePreference): void {
    theme.value = nextTheme
  }

  return {
    theme,
    setTheme,
  }
})

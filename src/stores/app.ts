import { defineStore, ref } from 'wevu'

import './manager'

export const useAppStore = defineStore('app', () => {
  const ready = ref(false)
  const online = ref(true)

  function markReady(): void {
    ready.value = true
  }

  function setOnline(value: boolean): void {
    online.value = value
  }

  return {
    ready,
    online,
    markReady,
    setOnline,
  }
})

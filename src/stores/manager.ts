import { createStore } from 'wevu'

// The manager must exist before any defineStore() module is evaluated because
// Wevu binds each store definition to the current manager.
export const storeManager = createStore()

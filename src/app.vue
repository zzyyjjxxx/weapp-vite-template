<script setup lang="ts">
import { focusManager } from '@tanstack/query-core'
import autoRoutes from 'weapp-vite/auto-routes'
import { onHide, onShow } from 'wevu'
import { installAbortGlobals } from 'wevu/web-apis'

import { createNetworkStatusAdapter } from '@/platform/network-status'
import { setupRouter } from '@/router'
import { setupQueryOnlineManager } from '@/shared/query/lifecycle'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { setupStorePlugins } from '@/stores/plugins'
import '@/styles/tailwind.css'

installAbortGlobals()
setupStorePlugins()
setupRouter()
setupQueryOnlineManager(createNetworkStatusAdapter())

useAuthStore().markInitialized()
useAppStore().markReady()

focusManager.setEventListener((setFocused) => {
  onShow(() => setFocused(true))
  onHide(() => setFocused(false))
  return () => undefined
})

defineAppJson({
  entryPagePath: 'pages/login/index',
  pages: autoRoutes.pages,
  window: {
    navigationBarTitleText: '用地需求填报',
    navigationBarBackgroundColor: '#ffffff',
    navigationBarTextStyle: 'black',
    backgroundColor: '#f2f7ff',
  },
  style: 'v2',
  componentFramework: 'glass-easel',
  sitemapLocation: 'sitemap.json',
})
</script>

<style lang="scss">
@use '@/styles/reset';
@use '@/styles/utilities';
</style>

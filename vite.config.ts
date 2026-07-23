import { fileURLToPath, URL } from 'node:url'

import { TDesignResolver } from 'weapp-vite/auto-import-components/resolvers'
import { defineConfig } from 'weapp-vite/config'

export default defineConfig({
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  weapp: {
    srcRoot: 'src',
    autoRoutes: true,
    subPackages: {
      'subpackages/order': {},
    },
    autoImportComponents: {
      resolvers: [TDesignResolver()],
      typedComponents: true,
      vueComponents: true,
      vueComponentsModule: 'wevu',
    },
    mcp: {
      enabled: true,
      autoStart: false,
    },
  },
})

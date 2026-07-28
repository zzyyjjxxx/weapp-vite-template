import { fileURLToPath, URL } from 'node:url'

import { weappTailwindcss } from 'weapp-tailwindcss/vite'
import { TDesignResolver } from 'weapp-vite/auto-import-components/resolvers'
import { defineConfig } from 'weapp-vite/config'

const tailwindEntry = fileURLToPath(new URL('./src/styles/tailwind.css', import.meta.url))

export default defineConfig({
  plugins: [weappTailwindcss({ cssEntries: [tailwindEntry] })],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  weapp: {
    srcRoot: 'src',
    autoRoutes: true,
    autoImportComponents: {
      resolvers: [TDesignResolver()],
      typedComponents: true,
      vueComponents: true,
      vueComponentsModule: 'wevu',
    },
    // The app uses Wevu's explicit fetch/AbortController adapters. Avoid
    // the automatic Web Runtime prelude, which creates a circular vendor
    // dependency in WeChat DevTools' CommonJS loader.
    injectWebRuntimeGlobals: false,
    mcp: {
      enabled: true,
      autoStart: false,
    },
  },
})

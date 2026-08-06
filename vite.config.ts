import { existsSync, readFileSync, writeFileSync } from 'node:fs'
import { fileURLToPath, URL } from 'node:url'

import { weappTailwindcss } from 'weapp-tailwindcss/vite'
import { TDesignResolver } from 'weapp-vite/auto-import-components/resolvers'
import { defineConfig } from 'weapp-vite/config'

const tailwindEntry = fileURLToPath(new URL('./src/styles/tailwind.css', import.meta.url))

function patchTDesignDeprecatedSystemInfo() {
  return {
    name: 'app:patch-tdesign-deprecated-system-info',
    enforce: 'post' as const,
    closeBundle() {
      const commonPath = fileURLToPath(new URL(
        './dist/miniprogram_npm/tdesign-miniprogram/common/wechat.js',
        import.meta.url,
      ))
      const uploadPath = fileURLToPath(new URL(
        './dist/miniprogram_npm/tdesign-miniprogram/upload/upload.js',
        import.meta.url,
      ))

      if (existsSync(commonPath)) {
        const source = readFileSync(commonPath, 'utf8')
          .replace(
            'const getWindowInfo = () => wx.getWindowInfo && wx.getWindowInfo() || wx.getSystemInfoSync();',
            'const getWindowInfo = () => wx.getWindowInfo && wx.getWindowInfo() || { windowWidth: 375, screenWidth: 375 };',
          )
          .replace(
            'const getAppBaseInfo = () => wx.getAppBaseInfo && wx.getAppBaseInfo() || wx.getSystemInfoSync();',
            'const getAppBaseInfo = () => wx.getAppBaseInfo && wx.getAppBaseInfo() || { SDKVersion: "0.0.0" };',
          )
          .replace(
            'const getDeviceInfo = () => wx.getDeviceInfo && wx.getDeviceInfo() || wx.getSystemInfoSync();',
            'const getDeviceInfo = () => wx.getDeviceInfo && wx.getDeviceInfo() || {};',
          )
        writeFileSync(commonPath, source)
      }

      if (existsSync(uploadPath)) {
        const source = readFileSync(uploadPath, 'utf8').replace(
          'return wx.getSystemInfoSync().windowWidth / 750 * 24;',
          'return ((wx.getWindowInfo && wx.getWindowInfo().windowWidth) || 375) / 750 * 24;',
        )
        writeFileSync(uploadPath, source)
      }
    },
  }
}

export default defineConfig({
  plugins: [
    weappTailwindcss({ cssEntries: [tailwindEntry] }),
    patchTDesignDeprecatedSystemInfo(),
  ],
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
    vue: {
      template: {
        // Summer Compiler cannot resolve the generated root-level virtualHost
        // slot wrapper. A real view preserves forwarded slot content and keeps
        // app.json free of the incompatible root component dependency.
        slotFallbackWrapperStrategy: 'view',
      },
    },
    mcp: {
      enabled: true,
      autoStart: false,
    },
  },
})

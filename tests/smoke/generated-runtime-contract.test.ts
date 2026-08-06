import { readFileSync } from 'node:fs'

import { describe, expect, it } from 'vitest'

describe('generated mini-program runtime contract', () => {
  it('has a deterministic login entry page and no native tab bar', () => {
    const source = readFileSync('src/app.vue', 'utf8')

    expect(source).toContain('entryPagePath: \'pages/login/index\'')
    expect(source).not.toContain('tabBar:')
  })

  it('keeps all source handlers on the already-unwrapped detail contract', () => {
    const sources = [
      'src/pages/login/index.vue',
      'src/pages/land-demand/index.vue',
      'src/features/land-demand/components/basic-info-step.vue',
      'src/features/land-demand/components/land-info-step.vue',
      'src/features/land-demand/components/project-info-step.vue',
      'src/features/land-demand/components/finance-contact-step.vue',
      'src/features/land-demand/components/review-step.vue',
      'src/features/land-demand/components/verification-dialog.vue',
    ].map(file => readFileSync(file, 'utf8')).join('\n')

    expect(sources).not.toContain('event.detail')
  })

  it('keeps optional chaining out of generated WXML expressions', () => {
    const templates = [
      'src/pages/home/index.vue',
      'src/pages/land-demand/index.vue',
      'src/pages/land-demand/success.vue',
      'src/features/land-demand/components/verification-dialog.vue',
    ].map((file) => {
      const source = readFileSync(file, 'utf8')
      return source.match(/<template>[\s\S]*<\/template>/)?.[0] ?? ''
    }).join('\n')

    expect(templates).not.toContain('?.')
  })

  it('uses a real view for forwarded slot fallbacks in Summer Compiler', () => {
    const config = readFileSync('vite.config.ts', 'utf8')
    const verifier = readFileSync('scripts/verify-generated-runtime.mjs', 'utf8')

    expect(config).toContain('slotFallbackWrapperStrategy: \'view\'')
    expect(verifier).toContain('__weapp_vite_slot_wrapper')
  })

  it('keeps conditional page bodies in native slots that Automator can inspect', () => {
    const formPage = readFileSync('src/pages/land-demand/index.vue', 'utf8')
    const successPage = readFileSync('src/pages/land-demand/success.vue', 'utf8')
    const verifier = readFileSync('scripts/verify-generated-runtime.mjs', 'utf8')

    expect(formPage).toContain('<view class="land-demand-page__content">')
    expect(successPage).toContain('<view class="land-demand-success">')
    expect(verifier).toContain('scoped-slot-')
  })

  it('patches TDesign deprecated system-info fallbacks in generated npm code', () => {
    const config = readFileSync('vite.config.ts', 'utf8')
    const verifier = readFileSync('scripts/verify-generated-runtime.mjs', 'utf8')

    expect(config).toContain('patchTDesignDeprecatedSystemInfo')
    expect(config).toContain('wx.getWindowInfo')
    expect(verifier).toContain('tdesignWechat')
    expect(verifier).toContain('getSystemInfoSync')
  })
})

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
})

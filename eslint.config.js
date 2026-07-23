import { icebreaker } from '@icebreakers/eslint-config'

export default icebreaker({
  miniProgram: true,
  vue: true,
  ignores: [
    '**/*.md',
    'CHANGELOG.md',
    'README.md',
    '.turbo/**',
    'dist/**',
    '.weapp-vite/**',
  ],
})

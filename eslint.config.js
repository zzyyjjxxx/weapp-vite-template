import { icebreaker } from '@icebreakers/eslint-config'

export default icebreaker({
  // Formatting is enforced by Stylelint and review; disabling the formatter
  // bridge keeps ESLint semantic checks deterministic across Windows/CI EOLs.
  formatters: false,
  miniProgram: true,
  vue: true,
  ignores: [
    '**/*.md',
    'CHANGELOG.md',
    'README.md',
    '.turbo/**',
    'dist/**',
    '.weapp-vite/**',
    // Generated from the supplied SQL source; shape/content are covered by
    // industry-dictionary tests and the generator remains linted.
    'src/features/land-demand/dictionaries/industries.generated.ts',
  ],
})

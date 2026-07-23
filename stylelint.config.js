import { icebreaker } from '@icebreakers/stylelint-config'

export default icebreaker({
  miniProgram: true,
  ignores: {
    addAtRules: ['use'],
  },
  rules: {
    'tailwindcss/no-atomic-class': null,
    'unocss/no-atomic-class': null,
  },
})

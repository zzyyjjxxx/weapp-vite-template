import { strict as assert } from 'node:assert'
import { readFileSync } from 'node:fs'
import process from 'node:process'

const app = JSON.parse(readFileSync('dist/app.json', 'utf8'))
const dispatcher = readFileSync('dist/weapp-vendors/wevu-watch.js', 'utf8')
const loginTemplate = readFileSync('dist/pages/login/index.wxml', 'utf8')
const generatedScripts = [
  'dist/pages/login/index.js',
  'dist/pages/land-demand/index.js',
  'dist/features/land-demand/components/basic-info-step.js',
  'dist/features/land-demand/components/land-info-step.js',
  'dist/features/land-demand/components/project-info-step.js',
  'dist/features/land-demand/components/finance-contact-step.js',
  'dist/features/land-demand/components/review-step.js',
  'dist/features/land-demand/components/verification-dialog.js',
].map(file => readFileSync(file, 'utf8')).join('\n')

assert.equal(app.entryPagePath, 'pages/login/index')
assert.equal(Object.hasOwn(app, 'tabBar'), false)
assert.match(dispatcher, /return\s+\w+\.detail/)
assert.match(loginTemplate, /data-wd-change="1"/)
assert.doesNotMatch(generatedScripts, /\.detail/)

process.stdout.write('Generated runtime contract verified.\n')

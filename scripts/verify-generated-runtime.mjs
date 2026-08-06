import { strict as assert } from 'node:assert'
import { existsSync, readFileSync } from 'node:fs'
import process from 'node:process'

const app = JSON.parse(readFileSync('dist/app.json', 'utf8'))
const dispatcher = readFileSync('dist/weapp-vendors/wevu-watch.js', 'utf8')
const loginTemplate = readFileSync('dist/pages/login/index.wxml', 'utf8')
const tdesignWechat = readFileSync(
  'dist/miniprogram_npm/tdesign-miniprogram/common/wechat.js',
  'utf8',
)
const tdesignUpload = readFileSync(
  'dist/miniprogram_npm/tdesign-miniprogram/upload/upload.js',
  'utf8',
)
const nativeSlotPageConfigs = [
  'dist/pages/land-demand/index.json',
  'dist/pages/land-demand/success.json',
].map(file => JSON.parse(readFileSync(file, 'utf8')))
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
assert.equal(app.usingComponents?.['weapp-slot-wrapper'], undefined)
assert.equal(existsSync('dist/__weapp_vite_slot_wrapper.json'), false)
for (const config of nativeSlotPageConfigs) {
  assert.deepEqual(
    Object.keys(config.usingComponents ?? {}).filter(name => name.startsWith('scoped-slot-')),
    [],
  )
}
assert.match(dispatcher, /return\s+\w+\.detail/)
assert.match(loginTemplate, /data-wd-change="1"/)
assert.doesNotMatch(generatedScripts, /\.detail/)
assert.doesNotMatch(tdesignWechat, /getSystemInfoSync/)
assert.doesNotMatch(tdesignUpload, /getSystemInfoSync/)

process.stdout.write('Generated runtime contract verified.\n')

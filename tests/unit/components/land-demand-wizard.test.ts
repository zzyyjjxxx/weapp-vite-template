import { existsSync, readFileSync } from 'node:fs'

import { describe, expect, it } from 'vitest'

const componentRoot = 'src/features/land-demand/components'
const stepFiles = [
  'basic-info-step.vue',
  'land-info-step.vue',
  'project-info-step.vue',
  'finance-contact-step.vue',
].map(file => `${componentRoot}/${file}`)

describe('land demand wizard component contract', () => {
  it('exposes stable field hooks through TDesign form controls', () => {
    const sources = stepFiles.map(file => readFileSync(file, 'utf8')).join('\n')

    for (const id of [
      'area',
      'building-area',
      'expect-park',
      'expect-time',
      'is-deploy',
      'deploy-park',
      'is-specialuse',
      'deploy-landtype',
      'deploy-height',
      'deploy-weight',
      'investment',
      'project-hydm',
      'keyindustry',
      'futureindustry',
      'pred-ys',
      'pred-tax',
      'pred-rdex',
      'pred-unitenergy',
      'projectdata',
      'is-financing',
      'financing-money',
      'financing-time',
      'contact',
      'office',
      'phone',
    ]) {
      expect(sources).toContain(`data-testid="${id}"`)
    }
    expect(sources).toMatch(/<t-(input|radio-group|checkbox-group|cascader|picker)/)
    expect(sources).not.toContain('<t-radio')
    expect(readFileSync('src/components/ui/single-picker/index.vue', 'utf8')).toContain('<t-picker')
  })

  it('keeps the multi-park selector as a TDesign checkbox list', () => {
    const landInfo = readFileSync(`${componentRoot}/land-info-step.vue`, 'utf8')

    expect(landInfo).toContain('<t-checkbox-group')
    expect(landInfo).not.toContain('<MultiPicker')
  })

  it('uses TDesign title metrics for custom input labels', () => {
    const styles = readFileSync('src/styles/utilities.scss', 'utf8')

    expect(styles).toMatch(/\.field__label\s*\{[\s\S]*font-size:\s*32rpx;[\s\S]*font-weight:\s*400;[\s\S]*line-height:\s*48rpx;[\s\S]*color:\s*\$color-text;/)
    expect(styles).toMatch(/\.field__required\s*\{[\s\S]*font-size:\s*32rpx;[\s\S]*line-height:\s*48rpx;/)
  })

  it('does not layer an outer separator over TDesign field separators', () => {
    const styles = readFileSync('src/styles/utilities.scss', 'utf8')

    expect(styles).not.toMatch(/\.field\s*\{[^}]*border-bottom/)
  })

  it('uses the form typography for login field labels and controls', () => {
    const source = readFileSync('src/pages/login/index.vue', 'utf8')

    expect(source).toMatch(/\.login__field\s*\{[\s\S]*--td-input-vertical-padding:\s*16rpx 32rpx;[\s\S]*--td-input-default-text-color:\s*#\{\$color-text\};/)
    expect(source).toMatch(/\.login__field-label\s*\{[\s\S]*min-height:\s*48rpx;[\s\S]*padding-left:\s*32rpx;[\s\S]*font-size:\s*32rpx;[\s\S]*font-weight:\s*400;[\s\S]*line-height:\s*48rpx;[\s\S]*color:\s*\$color-text;/)
  })

  it('forces the login hero into real-device debug packages', () => {
    const projectConfig = JSON.parse(readFileSync('project.config.json', 'utf8')) as {
      setting?: { ignoreUploadUnusedFiles?: boolean }
      packOptions?: {
        include?: Array<{ type?: string, value?: string }>
      }
    }

    expect(projectConfig.setting?.ignoreUploadUnusedFiles).toBe(false)
    expect(projectConfig.packOptions?.include).toContainEqual({
      type: 'file',
      value: 'assets/land-planning-hero.png',
    })
  })

  it('uses an upload-whitelisted public path for the login hero on real devices', () => {
    const source = readFileSync('src/pages/login/index.vue', 'utf8')

    expect(source).not.toContain(`import landPlanningHero from '@/assets/land-planning-hero.webp'`)
    expect(source).not.toContain('land-planning-hero.webp')
    expect(source).toContain('src="/assets/land-planning-hero.png"')
    expect(existsSync('public/assets/land-planning-hero.png')).toBe(true)
  })

  it('keeps success actions equal, inset, and separated in mini-program WXSS', () => {
    const source = readFileSync('src/pages/land-demand/success.vue', 'utf8')

    expect(source.match(/t-class="land-demand-success__button"/g)).toHaveLength(2)
    expect(source).toContain('.land-demand-success__action + .land-demand-success__action')
    expect(source).toMatch(/\.land-demand-success__action\s*\{[\s\S]*width:\s*0;/)
    expect(source).toMatch(/\.land-demand-success__action\s*\{[\s\S]*flex:\s*1 1 0;/)
    expect(source).not.toMatch(/\.land-demand-success__action\s*\{[\s\S]*overflow:\s*hidden;/)
    expect(source).toMatch(/\.land-demand-success__button\s*\{[\s\S]*box-sizing:\s*border-box;[\s\S]*width:\s*100%;[\s\S]*min-width:\s*0;[\s\S]*max-width:\s*100%;/)
    expect(source).not.toContain('gap: $space-2;')
  })

  it('keeps height and weight outside special-use conditional markup', () => {
    const source = readFileSync(`${componentRoot}/land-info-step.vue`, 'utf8')

    expect(source).toMatch(/data-testid="deploy-height"/)
    expect(source).toMatch(/data-testid="deploy-weight"/)
    expect(source).not.toMatch(/v-if="[^"]*is_specialuse[^"]*"[\s\S]*data-testid="deploy-height"/)
  })

  it('consumes already-unwrapped event details and emits partial patches without mutating props', () => {
    const sources = stepFiles.map(file => readFileSync(file, 'utf8')).join('\n')

    expect(sources).toContain('readStringDetail')
    expect(sources).not.toContain('event.detail')
    expect(sources).toContain('emit(\'change\', {')
    expect(sources).not.toMatch(/props\.form\.\w+\s*=(?!=)/)
  })

  it('passes child component patches directly to the page controller', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).toContain('readPatchDetail<LandDemandForm>(detail)')
    expect(source).not.toContain('event.detail')
  })

  it('uses an explicit TDesign dialog for destructive clears', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).toContain('<t-dialog')
    expect(source).toContain('destructive-clear-dialog')
    expect(source).toContain('confirm-btn="继续"')
    expect(source.match(/button-layout="horizontal"/g)).toHaveLength(2)
    expect(source).toContain('@confirm="confirmDestructiveClear"')
  })

  it('saves before returning to the workbench and passes a success notice', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')
    const home = readFileSync('src/pages/home/index.vue', 'utf8')

    expect(source).toContain('data-testid="land-demand-back-home"')
    expect(source).toContain('@tap="handleReturnToHome"')
    expect(source).not.toContain(':loading="returningToHome"')
    expect(source).toContain(':disabled="saving || returningToHome"')
    expect(source).toContain('data-testid="return-home-loading"')
    expect(source).toContain('data-testid="verification-submit-loading"')
    expect(source).toContain('text="正在暂存"')
    expect(source).toContain('text="正在发送验证码"')
    expect(source).toContain(':transitioning="transitioning"')
    expect(source).toContain('await nextTick()')
    expect(source).toContain('const saved = await saveDraftWithNotice(false)')
    expect(source).toContain('data-testid="return-save-error-message"')
    expect(source).toContain('当前填报内容存在问题，请先修正后再返回工作台')
    expect(source).toContain('replace(\'/pages/home/index\', shouldPersist ? { notice: \'saved\' } : undefined)')
    expect(source).toContain('data-testid="required-return-dialog"')
    expect(source).toContain('validateStep(form.value, currentStep.value)')
    expect(source).toContain('async function saveAndBackToHome(): Promise<void>')
    expect(source).toContain('async function completeReturnToHome(): Promise<void>')
    expect(source).toContain('async function handleReturnToHome(): Promise<void>')
    expect(source).toContain('await completeReturnToHome()')
    expect(source).toContain('content="当前还有必填项未填写，是否确认返回？"')
    expect(source).toContain('cancel-btn="继续填写"')
    expect(source).toContain('confirm-btn="确认返回"')
    expect(source).toContain('@confirm="confirmRequiredReturn"')
    expect(source).not.toContain('leave-draft-dialog')
    expect(home).toContain('data-testid="home-save-success-message"')
    expect(home).toContain('query?.notice === \'saved\'')
  })

  it('keeps the workbench return action compact and pill-shaped', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).toContain('t-class="land-demand-page__back-home-button"')
    expect(source).toContain('size="extra-small"')
    expect(source).toContain('shape="round"')
    expect(source).toMatch(/\.land-demand-page__back-home-button\s*\{[\s\S]*width:\s*220rpx;[\s\S]*min-width:\s*0;[\s\S]*box-shadow:/)
  })

  it('scrolls to the first invalid field when advancing is blocked', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')
    const stepSources = stepFiles.map(file => readFileSync(file, 'utf8')).join('\n')

    expect(source).toContain('validateStep(form.value, currentStep.value)')
    expect(stepSources).toContain('useInvalidFieldScroll(() => props.errors')
    for (const id of ['area-field', 'investment-field', 'phone-field']) {
      expect(stepSources).toContain(`data-testid="${id}"`)
    }
    const invalidFieldScroll = readFileSync('src/features/land-demand/invalid-field-scroll.ts', 'utf8')
    expect(invalidFieldScroll).toContain('scrollPageToField')
    expect(invalidFieldScroll).toContain('>>> #')
    expect(invalidFieldScroll).toContain('testId')
    expect(invalidFieldScroll).toContain('setTimeout(resolve, 0)')
  })

  it('shows save success as a top TDesign message instead of page feedback', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).toContain('<t-message')
    expect(source).toContain('data-testid="save-success-message"')
    expect(source).toContain('theme="success"')
    expect(source).toContain(':duration="2000"')
    expect(source).toContain(':offset="[16, 16]"')
    expect(source).toContain('saveNotice.value = \'暂存成功\'')
    expect(source).not.toContain('feedback.value = \'已暂存\'')
  })

  it('does not show a duplicate step-validation feedback banner', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).not.toMatch(/feedback\.value = `请先完成第/)
    expect(source).toContain('feedback.value = \'\'')
  })

  it('never passes transient null values into typed mini-program component properties', () => {
    const page = readFileSync('src/pages/land-demand/index.vue', 'utf8')
    const verificationDialog = readFileSync(`${componentRoot}/verification-dialog.vue`, 'utf8')

    expect(page.match(/:current-step="currentStep \|\| 1"/g)).toHaveLength(2)
    expect(page).toContain(':acceptance-error="acceptanceError || \'\'"')
    expect(page).toContain(':content="clearDialogContent || \'\'"')
    expect(page).toContain(':code="verificationCode || \'\'"')
    expect(page).toContain(':error="verificationError || \'\'"')
    expect(verificationDialog).toContain(':content="description || \'\'"')
  })

  it('keeps every TDesign string and array property concrete on first render', () => {
    const sources = stepFiles.map(file => readFileSync(file, 'utf8')).join('\n')
    const project = readFileSync(`${componentRoot}/project-info-step.vue`, 'utf8')
    const basic = readFileSync(`${componentRoot}/basic-info-step.vue`, 'utf8')
    const verificationDialog = readFileSync(`${componentRoot}/verification-dialog.vue`, 'utf8')
    const singlePicker = readFileSync('src/components/ui/single-picker/index.vue', 'utf8')

    expect(sources).not.toMatch(/:tips="fieldError\([^)]*\)"/)
    expect(sources).toContain('const financingOptions')
    expect(project).toContain('const industryOptions = ref([...NATIONAL_INDUSTRY_OPTIONS])')
    expect(project).toContain('filter-placeholder="搜索行业"')
    expect(project).toContain('const industryNote = ref')
    expect(basic).toContain('status="default"')
    expect(basic).toContain('tips=""')
    expect(verificationDialog).toContain('status="default"')
    expect(verificationDialog).toContain('tips=""')
    expect(singlePicker).toContain(':value="pickerValue || []"')
    expect(singlePicker).toContain(':options="pickerOptions || []"')
  })

  it('uses a compact, viewport-safe progress rail and fixed action bar', () => {
    const progress = readFileSync(`${componentRoot}/wizard-progress.vue`, 'utf8')
    const actions = readFileSync(`${componentRoot}/wizard-actions.vue`, 'utf8')
    const page = readFileSync('src/pages/land-demand/index.vue', 'utf8')
    const home = readFileSync('src/pages/home/index.vue', 'utf8')

    expect(progress).not.toContain('<scroll-view')
    expect(progress).toContain('flex: 1 1 0')
    expect(progress).toContain('progressStep?: LandDemandStep')
    expect(progress).toContain('wizard-progress__step--complete')
    expect(progress).toContain('wizard-progress__step--incomplete')
    expect(progress).toContain('$color-error')
    expect(progress).toContain('$color-success')
    expect(progress).not.toContain('v-for="(label, index) in steps"')
    expect(progress).toContain('<text class="wizard-progress__number">1</text>')
    expect(progress).toContain('<text class="wizard-progress__number">5</text>')
    expect(progress).not.toContain('index + 1')
    expect(page).toContain(':incomplete-steps="progressIncompleteSteps"')
    expect(page).toContain(':progress-step="progressStep || 1"')
    expect(page).toContain('return incompleteSteps.value')
    expect(page).not.toContain('currentStep.value === 5 && !accepted.value')
    expect(home).toContain('home__step--complete')
    expect(home).toContain('home__step--incomplete')
    expect(home).toContain('resumeStep')
    expect(home).toContain('resumeStep.value} 步 / 共 5 步')
    expect(home).toContain('step: selectedStep.value ?? (record.value ? resumeStep.value : undefined)')
    expect(home).toContain('class="home__page-content"')
    expect(home).toContain('<AppLoading v-if="landDemandQuery.isPending" />')
    expect(home).toContain('title="工作台信息加载失败"')
    expect(home).toContain('return incompleteSteps.value')
    expect(home).not.toContain('currentEditingStep.value === 5 && !steps.includes(5)')
    expect(actions).toContain('position: fixed')
    expect(actions).toContain('function handlePrevious(): void')
    expect(actions).toContain('@tap="handlePrevious"')
    expect(actions.match(/\bblock\b/g)).toHaveLength(3)
    expect(actions).toContain('min-width: 0')
    expect(page).toContain('compact')
    expect(page).toContain('padding-bottom: calc(128rpx + env(safe-area-inset-bottom))')
    expect(page).toContain('land-demand-page--view')
  })

  it('allows shared card utilities to style isolated step components', () => {
    for (const file of [
      'basic-info-step.vue',
      'land-info-step.vue',
      'project-info-step.vue',
      'finance-contact-step.vue',
      'review-step.vue',
    ]) {
      expect(readFileSync(`${componentRoot}/${file}`, 'utf8')).toContain(
        'styleIsolation: \'apply-shared\'',
      )
    }
  })

  it('does not recreate a local draft after an explicit server save', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).toContain('store.markPersisted(record)')
    expect(source).toContain('unchangedSubmittedRecord')
    expect(source).toContain('shouldPersist')
    expect(source).not.toMatch(/store\.markPersisted\(record\)\s+store\.saveLocalDraft\(\)/)
  })

  it('exposes every Task 7 control used by the Task 9 runtime contract', () => {
    const sources = [
      ...stepFiles,
      `${componentRoot}/wizard-actions.vue`,
    ].map(file => readFileSync(file, 'utf8')).join('\n')

    for (const id of [
      'next-step',
      'save-draft',
      'area',
      'deploy-height',
      'financing-money-error',
      'financing-time-error',
    ]) {
      expect(sources).toContain(`data-testid="${id}"`)
    }
  })

  it('uses a searchable national-industry cascader instead of a free input', () => {
    const source = readFileSync(`${componentRoot}/project-info-step.vue`, 'utf8')

    expect(source).toContain('<t-cascader')
    expect(source).toContain(':filterable="true"')
    expect(source).toContain('NATIONAL_INDUSTRY_OPTIONS')
    expect(source).toContain('getIndustryDisplay')
    expect(source).not.toMatch(/<t-input[^>]+data-testid="project-hydm"/)
  })

  it('uses the confirmed investment and unit-energy labels', () => {
    const source = readFileSync(`${componentRoot}/project-info-step.vue`, 'utf8')

    expect(source).toContain('固定资产投资额（万元）')
    expect(source).toContain('项目单位能耗增加值（万元/吨标煤）')
    expect(source).not.toContain('项目总投资（万元）')
    expect(source).not.toContain('预计单位能耗')
  })

  it('loads local drafts through the Store boundary instead of the page repository', () => {
    const source = readFileSync('src/pages/land-demand/index.vue', 'utf8')

    expect(source).toContain('store.initializeFromLocalDraft')
    expect(source).toContain('const original = query.data.value')
    expect(source).not.toContain('@/features/land-demand/repository')
    expect(source).not.toContain('getLandDemandRepository')
    expect(source).not.toContain('originalRecord')
  })
})

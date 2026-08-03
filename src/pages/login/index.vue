<script setup lang="ts">
import type { LoginInput } from '@/features/auth/models'

import { computed, onLoad, ref } from 'wevu'
import landPlanningHero from '@/assets/land-planning-hero.webp'
import AppIcon from '@/components/ui/app-icon/index.vue'
import { useLoginMutation } from '@/features/auth/queries'
import { readStringDetail } from '@/platform/event-detail'
import { replaceUrl } from '@/router/navigation'
import { parseReturnTo } from '@/router/query'

definePageJson({
  navigationBarTitleText: '用地需求填报',
})

const username = ref('demo')
const password = ref('demo123')
const usernameError = ref('')
const passwordError = ref('')
const formError = ref('')
const returnTo = ref('/pages/home/index')
const loginMutation = useLoginMutation()
const isPending = loginMutation.isPending
const errorMessage = computed(() => formError.value || loginMutation.error.value?.message || '')

onLoad((query) => {
  returnTo.value = parseReturnTo(query?.returnTo)
})

function updateUsername(detail: unknown): void {
  username.value = readStringDetail(detail)
  usernameError.value = ''
}

function updatePassword(detail: unknown): void {
  password.value = readStringDetail(detail)
  passwordError.value = ''
}

function validate(input: LoginInput): boolean {
  usernameError.value = input.username ? '' : '请输入用户名'
  passwordError.value = input.password ? '' : '请输入密码'
  return !usernameError.value && !passwordError.value
}

async function submit(): Promise<void> {
  formError.value = ''
  const input: LoginInput = {
    username: username.value.trim(),
    password: password.value,
  }
  if (!validate(input)) {
    return
  }

  try {
    await loginMutation.mutateAsync(input)
    await replaceUrl(returnTo.value)
  }
  catch {
    // The mutation result exposes the sanitized API error to the template.
  }
}
</script>

<template>
  <view class="login">
    <view class="login__hero">
      <view class="login__hero-copy">
        <text class="login__eyebrow">ENTERPRISE LAND SERVICE</text>
        <text class="login__headline">欢迎登录</text>
        <view class="login__accent" />
        <text class="login__subline">企业用地需求在线填报服务</text>
      </view>
      <image
        class="login__illustration"
        :src="landPlanningHero"
        mode="aspectFill"
      />
    </view>

    <view class="login__panel">
      <view class="login__panel-heading">
        <view>
          <text class="login__panel-title">企业账号登录</text>
          <text class="login__panel-description">登录后可填报、暂存和查询用地需求</text>
        </view>
        <text class="login__demo">演示环境</text>
      </view>

      <view class="login__field">
        <text class="login__field-label">用户名</text>
        <t-input
          data-testid="username"
          :value="username"
          :maxlength="32"
          placeholder="请输入用户名"
          status="default"
          tips=""
          @change="updateUsername"
        >
          <template #prefix-icon>
            <AppIcon class="login__input-icon" name="user-circle" :size="34" />
          </template>
        </t-input>
      </view>
      <view class="login__field">
        <text class="login__field-label">密码</text>
        <t-input
          data-testid="password"
          type="password"
          :value="password"
          :maxlength="64"
          placeholder="请输入密码"
          status="default"
          tips=""
          @change="updatePassword"
        >
          <template #prefix-icon>
            <AppIcon class="login__input-icon" name="lock" :size="34" />
          </template>
        </t-input>
      </view>
      <view class="login__account-tip">
        <text class="login__account-tip-label">演示账号</text>
        <text class="login__account-tip-value">demo / demo123</text>
      </view>
      <text v-if="errorMessage" class="login__error">
        {{ errorMessage }}
      </text>
      <t-button
        data-testid="login-submit"
        class="login__submit"
        theme="primary"
        block
        :loading="isPending"
        :disabled="isPending"
        @tap="submit"
      >
        登录并进入填报
      </t-button>
      <view class="login__trust">
        <view class="login__trust-dot" />
        <text>仅用于企业用地需求服务，信息保存在本地演示环境</text>
      </view>
    </view>

    <view class="login__footer">
      <text class="login__footer-title">企业用地需求服务</text>
      <text class="login__footer-copy">规范 · 清晰 · 便捷</text>
    </view>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.login {
  min-height: 100vh;
  padding-bottom: $space-6;
  overflow: hidden;
  background: $gradient-page;
}

.login__hero {
  position: relative;
  height: 390rpx;
  overflow: hidden;
  background: $gradient-hero;
}

.login__hero::after {
  position: absolute;
  right: -120rpx;
  bottom: -180rpx;
  width: 460rpx;
  height: 460rpx;
  content: '';
  background: rgb(71 137 244 / 12%);
  border-radius: 50%;
}

.login__hero-copy {
  position: absolute;
  top: 42rpx;
  left: $space-4;
  z-index: 2;
}

.login__eyebrow,
.login__headline,
.login__subline {
  display: block;
}

.login__eyebrow {
  font-size: 20rpx;
  font-weight: 700;
  color: $color-primary;
  letter-spacing: 3rpx;
}

.login__headline {
  margin-top: $space-2;
  font-size: 58rpx;
  font-weight: 800;
  line-height: 1.2;
  color: #183a76;
}

.login__accent {
  width: 70rpx;
  height: 8rpx;
  margin-top: $space-3;
  background: $gradient-primary;
  border-radius: 999rpx;
}

.login__subline {
  margin-top: $space-3;
  font-size: 27rpx;
  color: $color-text-secondary;
}

.login__illustration {
  position: absolute;
  top: 0;
  left: 0;
  z-index: 1;
  width: 100%;
  height: 100%;
  opacity: 0.96;
}

.login__input-icon {
  margin-right: 8rpx;
  opacity: 0.72;
}

.login__panel {
  position: relative;
  z-index: 3;
  padding: $space-4 $space-4 $space-3;
  margin: -38rpx $space-4 0;
  background: rgb(255 255 255 / 97%);
  border: 1rpx solid rgb(220 230 245 / 86%);
  border-radius: $radius-xl;
  box-shadow: 0 24rpx 70rpx rgb(32 70 132 / 15%);
}

.login__panel-heading {
  display: flex;
  gap: $space-3;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: $space-4;
}

.login__panel-title,
.login__panel-description,
.login__field-label,
.login__footer-title,
.login__footer-copy {
  display: block;
}

.login__panel-title {
  font-size: 36rpx;
  font-weight: 700;
  color: $color-text;
}

.login__panel-description {
  margin-top: $space-1;
  font-size: 23rpx;
  line-height: 1.5;
  color: $color-text-secondary;
}

.login__demo {
  flex: 0 0 auto;
  padding: 6rpx 14rpx;
  font-size: 20rpx;
  color: $color-primary;
  background: $color-primary-soft;
  border-radius: 999rpx;
}

.login__field {
  padding: $space-2 0;
  border-bottom: 1rpx solid $color-border-soft;
}

.login__field + .login__field {
  margin-top: $space-1;
}

.login__field-label {
  padding-left: 68rpx;
  margin-bottom: 2rpx;
  font-size: 23rpx;
  font-weight: 600;
  color: $color-text-secondary;
}

.login__account-tip {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: $space-2 $space-3;
  margin-top: $space-3;
  background: $color-primary-faint;
  border: 1rpx solid #deebff;
  border-radius: $radius-md;
}

.login__account-tip-label {
  font-size: 23rpx;
  color: $color-text-secondary;
}

.login__account-tip-value {
  font-size: 24rpx;
  font-weight: 600;
  color: $color-primary;
}

.login__error {
  display: block;
  margin-top: $space-2;
  font-size: 24rpx;
  line-height: 1.6;
  color: $color-error;
}

.login__submit {
  margin-top: $space-4;
  overflow: hidden;
  border-radius: $radius-md;
  box-shadow: $shadow-button;
}

.login__trust {
  display: flex;
  align-items: flex-start;
  justify-content: center;
  margin-top: $space-3;
  font-size: 21rpx;
  line-height: 1.5;
  color: $color-text-placeholder;
  text-align: center;
}

.login__trust-dot {
  flex: 0 0 auto;
  width: 10rpx;
  height: 10rpx;
  margin: 10rpx 10rpx 0 0;
  background: $color-success;
  border-radius: 50%;
}

.login__footer {
  margin-top: $space-5;
  text-align: center;
}

.login__footer-title {
  font-size: 24rpx;
  font-weight: 600;
  color: $color-text-secondary;
}

.login__footer-copy {
  margin-top: $space-1;
  font-size: 21rpx;
  color: $color-text-placeholder;
  letter-spacing: 5rpx;
}
</style>

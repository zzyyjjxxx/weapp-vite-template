<script setup lang="ts">
import type { LoginInput } from '@/features/auth/models'

import { computed, onLoad, ref } from 'wevu'
import PageShell from '@/components/ui/page-shell/index.vue'
import { useLoginMutation } from '@/features/auth/queries'
import { replaceUrl } from '@/router/navigation'

definePageJson({
  navigationBarTitleText: '登录',
})

const username = ref('demo')
const password = ref('demo123')
const returnTo = ref('/pages/home/index')
const formError = ref('')
const loginMutation = useLoginMutation()
const isPending = loginMutation.isPending
const errorMessage = computed(() => formError.value || loginMutation.error.value?.message || '')

onLoad((query) => {
  const candidate = query?.returnTo
  if (typeof candidate === 'string' && candidate.startsWith('/') && !candidate.startsWith('//') && !candidate.startsWith('/pages/login')) {
    returnTo.value = candidate
  }
})

function readInputValue(event: unknown): string {
  if (typeof event !== 'object' || event === null || !('detail' in event)) {
    return ''
  }
  const detail = event.detail
  if (typeof detail !== 'object' || detail === null || !('value' in detail)) {
    return ''
  }
  return typeof detail.value === 'string' ? detail.value : ''
}

function updateUsername(event: unknown): void {
  username.value = readInputValue(event)
}

function updatePassword(event: unknown): void {
  password.value = readInputValue(event)
}

async function submit(): Promise<void> {
  formError.value = ''
  const input: LoginInput = {
    username: username.value.trim(),
    password: password.value,
  }
  if (!input.username || !input.password) {
    formError.value = '请输入用户名和密码。'
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
  <PageShell
    title="登录"
    subtitle="演示账号：demo / demo123"
  >
    <view class="login__card u-card">
      <text class="login__label">
        用户名
      </text>
      <input
        class="login__input"
        :value="username"
        :maxlength="32"
        placeholder="请输入用户名"
        @input="updateUsername"
      >
      <text class="login__label">
        密码
      </text>
      <input
        class="login__input"
        :value="password"
        password
        :maxlength="64"
        placeholder="请输入密码"
        @input="updatePassword"
      >
      <text v-if="errorMessage" class="login__error">
        {{ errorMessage }}
      </text>
      <button
        class="login__submit"
        :loading="isPending"
        :disabled="isPending"
        @tap="submit"
      >
        登录
      </button>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.login__card {
  padding: $space-4;
}

.login__label {
  display: block;
  margin-bottom: $space-1;
  font-size: 26rpx;
  color: $color-text;
}

.login__label + .login__label {
  margin-top: $space-3;
}

.login__input {
  width: 100%;
  height: 88rpx;
  padding: 0 $space-2;
  font-size: 28rpx;
  background: #f7f8fa;
  border: 2rpx solid $color-border;
  border-radius: $radius-sm;
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
  color: #fff;
  background: $color-primary;
  border: 0;
  border-radius: $radius-sm;
}
</style>

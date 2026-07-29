<script setup lang="ts">
import type { LoginInput } from '@/features/auth/models'

import { computed, onLoad, ref } from 'wevu'
import PageShell from '@/components/ui/page-shell/index.vue'
import { useLoginMutation } from '@/features/auth/queries'
import { replaceUrl } from '@/router/navigation'
import { parseReturnTo } from '@/router/query'

definePageJson({
  navigationBarTitleText: '登录',
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
  usernameError.value = ''
}

function updatePassword(event: unknown): void {
  password.value = readInputValue(event)
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
  <PageShell
    title="登录"
    subtitle="演示账号：demo / demo123"
    icon="login"
  >
    <view class="login__card u-card">
      <t-input
        data-testid="username"
        label="用户名"
        :value="username"
        :maxlength="32"
        placeholder="请输入用户名"
        :status="usernameError ? 'error' : 'default'"
        :tips="usernameError"
        @change="updateUsername"
      />
      <t-input
        data-testid="password"
        class="login__password"
        label="密码"
        type="password"
        :value="password"
        :maxlength="64"
        placeholder="请输入密码"
        :status="passwordError ? 'error' : 'default'"
        :tips="passwordError"
        @change="updatePassword"
      />
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
        登录
      </t-button>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.login__card {
  padding: $space-4;
}

.login__password {
  display: block;
  margin-top: $space-3;
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
}
</style>

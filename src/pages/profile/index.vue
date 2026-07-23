<script setup lang="ts">
import { computed, onShow } from 'wevu'

import AppError from '@/components/ui/app-error/index.vue'
import AppLoading from '@/components/ui/app-loading/index.vue'
import PageShell from '@/components/ui/page-shell/index.vue'
import { useProfileQuery } from '@/features/auth/queries'
import { navigate, replace } from '@/router/navigation'
import { clearPrivateQueryCaches } from '@/shared/query/private-cache'
import { useAuthStore } from '@/stores/auth'

definePageJson({
  navigationBarTitleText: '个人资料',
})

const auth = useAuthStore()
const profileQuery = useProfileQuery()
const profile = profileQuery.data
const isPending = profileQuery.isPending
const isError = profileQuery.isError
const errorMessage = computed(() => profileQuery.error.value?.message ?? '个人资料暂时不可用。')

onShow(() => {
  if (!auth.isAuthenticated.value) {
    void navigate('/pages/login/index', {
      returnTo: '/pages/profile/index',
    })
  }
})

async function logout(): Promise<void> {
  auth.clearSession()
  clearPrivateQueryCaches()
  await replace('/pages/home/index')
}

async function goLogin(): Promise<void> {
  await navigate('/pages/login/index', {
    returnTo: '/pages/profile/index',
  })
}
</script>

<template>
  <PageShell
    title="个人资料"
    subtitle="受保护接口的 Query Core 示例"
  >
    <AppLoading v-if="isPending" />
    <AppError
      v-else-if="isError"
      :message="errorMessage"
      @retry="() => { void profileQuery.refetch() }"
    />
    <view v-else-if="profile" class="profile__content">
      <view class="profile__card u-card">
        <view class="profile__avatar">
          {{ profile.displayName.slice(0, 1) }}
        </view>
        <text class="profile__name">
          {{ profile.displayName }}
        </text>
        <text class="profile__username">
          {{ profile.username }}
        </text>
      </view>

      <view class="profile__details u-card">
        <view class="profile__row">
          <text>用户 ID</text>
          <text>{{ profile.id }}</text>
        </view>
        <view class="profile__row">
          <text>租户</text>
          <text>{{ profile.tenantId }}</text>
        </view>
      </view>

      <button class="profile__logout" @tap="logout">
        退出登录
      </button>
    </view>
    <view v-else class="profile__guest u-card">
      <text class="profile__guest-title">
        尚未登录
      </text>
      <button class="profile__login" @tap="goLogin">
        前往登录
      </button>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.profile__card {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.profile__avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 112rpx;
  height: 112rpx;
  color: #fff;
  background: $color-primary;
  border-radius: 50%;
}

.profile__name {
  margin-top: $space-2;
  font-size: 34rpx;
  font-weight: 700;
  color: $color-text;
}

.profile__username {
  margin-top: $space-1;
  font-size: 24rpx;
  color: $color-text-secondary;
}

.profile__details,
.profile__guest {
  margin-top: $space-3;
}

.profile__row {
  display: flex;
  justify-content: space-between;
  padding: $space-2 0;
  font-size: 26rpx;
  color: $color-text-secondary;
  border-bottom: 2rpx solid $color-border;
}

.profile__row:last-child {
  border-bottom: 0;
}

.profile__row text:last-child {
  max-width: 60%;
  overflow: hidden;
  text-overflow: ellipsis;
  color: $color-text;
  white-space: nowrap;
}

.profile__logout,
.profile__login {
  margin-top: $space-4;
  color: $color-primary;
  background: $color-primary-soft;
  border: 0;
  border-radius: $radius-sm;
}

.profile__guest-title {
  display: block;
  font-size: 28rpx;
  font-weight: 600;
  color: $color-text;
}
</style>

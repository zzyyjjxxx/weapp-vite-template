<script setup lang="ts">
import type { AppIconName } from '@/components/ui/app-icon/icons'
import type { AppTabPath } from '@/components/ui/app-tab-bar/items'

import AppIcon from '@/components/ui/app-icon/index.vue'
import AppTabBar from '@/components/ui/app-tab-bar/index.vue'

const props = defineProps<{
  title: string
  subtitle?: string
  icon?: AppIconName
  tabBarPath?: AppTabPath
}>()

defineComponentJson({
  component: true,
})
</script>

<template>
  <view
    class="page-shell"
    :class="{ 'page-shell--with-tab-bar': props.tabBarPath }"
  >
    <view class="page-shell__header">
      <view class="page-shell__heading">
        <AppIcon
          v-if="props.icon"
          class="page-shell__icon"
          :name="props.icon"
          :size="48"
          weight="Filled"
        />
        <view class="page-shell__heading-copy">
          <text class="page-shell__title">
            {{ props.title }}
          </text>
          <text
            v-if="props.subtitle"
            class="page-shell__subtitle"
          >
            {{ props.subtitle }}
          </text>
        </view>
      </view>
    </view>
    <view class="page-shell__body">
      <slot />
    </view>
    <AppTabBar
      v-if="props.tabBarPath"
      :active-path="props.tabBarPath"
    />
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.page-shell {
  min-height: 100vh;
  padding: $space-5 $space-4 $space-5;
  background: $color-page;
}

.page-shell--with-tab-bar {
  padding-bottom: calc($space-5 + 132rpx);
}

.page-shell__header {
  padding: $space-2 0 $space-4;
}

.page-shell__heading {
  display: flex;
  align-items: center;
}

.page-shell__icon {
  margin-right: $space-2;
}

.page-shell__heading-copy {
  flex: 1;
  min-width: 0;
}

.page-shell__title {
  display: block;
  font-size: 44rpx;
  font-weight: 700;
  line-height: 1.25;
  color: $color-text;
}

.page-shell__subtitle {
  display: block;
  margin-top: $space-1;
  font-size: 24rpx;
  line-height: 1.6;
  color: $color-text-secondary;
}

.page-shell__body {
  min-height: 480rpx;
}
</style>

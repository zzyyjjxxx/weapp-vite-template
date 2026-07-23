<script setup lang="ts">
import type { AppTabPath } from './items'

import { computed } from 'wevu'

import AppIcon from '@/components/ui/app-icon/index.vue'
import { navigate } from '@/router/navigation'
import { appTabItems } from './items'

const props = defineProps<{
  activePath: AppTabPath
}>()

defineComponentJson({
  component: true,
})

const activeTabPath = computed(() => props.activePath)

async function selectTab(path: AppTabPath): Promise<void> {
  if (path === activeTabPath.value) {
    return
  }
  await navigate(path)
}
</script>

<template>
  <view
    class="app-tab-bar"
    aria-label="主导航"
  >
    <view
      v-for="item in appTabItems"
      :key="item.path"
      class="app-tab-bar__item"
      :class="{ 'app-tab-bar__item--active': item.path === activeTabPath }"
      :data-path="item.path"
      @tap="selectTab(item.path)"
    >
      <AppIcon
        :name="item.icon"
        :weight="item.path === activeTabPath ? 'Filled' : 'Outline'"
        :size="44"
      />
      <text class="app-tab-bar__label">
        {{ item.label }}
      </text>
    </view>
  </view>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.app-tab-bar {
  position: fixed;
  right: 0;
  bottom: 0;
  left: 0;
  z-index: 100;
  display: flex;
  padding: 12rpx 24rpx calc(12rpx + env(safe-area-inset-bottom));
  background: $color-card;
  border-top: 1rpx solid $color-border;
  box-shadow: 0 -8rpx 28rpx rgb(29 33 41 / 5%);
}

.app-tab-bar__item {
  display: flex;
  flex: 1;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 84rpx;
  color: $color-text-secondary;
  opacity: 0.72;
}

.app-tab-bar__item--active {
  color: $color-primary;
  opacity: 1;
}

.app-tab-bar__label {
  margin-top: 4rpx;
  font-size: 20rpx;
  line-height: 1.4;
}
</style>

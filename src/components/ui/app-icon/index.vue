<script setup lang="ts">
import type { AppIconName, AppIconWeight } from './icons'

import { computed } from 'wevu'
import { getAppIconSource } from './icons'

const props = withDefaults(defineProps<{
  name: AppIconName
  size?: number | string
  weight?: AppIconWeight
}>(), {
  size: 40,
  weight: 'Outline',
})

defineComponentJson({
  component: true,
})

const iconSource = computed(() => getAppIconSource(props.name, props.weight))
const iconStyle = computed(() => {
  const size = typeof props.size === 'number' ? `${props.size}rpx` : props.size
  return `width: ${size}; height: ${size};`
})
</script>

<template>
  <image
    class="app-icon"
    :src="iconSource"
    :style="iconStyle"
    mode="aspectFit"
    aria-hidden="true"
  />
</template>

<style lang="scss">
.app-icon {
  display: block;
  flex: 0 0 auto;
}
</style>

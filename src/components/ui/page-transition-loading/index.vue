<script setup lang="ts">
const props = withDefaults(defineProps<{
  visible: boolean
  text?: string
}>(), {
  text: '正在加载',
})

defineComponentJson({ component: true })
</script>

<template>
  <t-loading
    v-if="props.visible"
    data-testid="page-transition-loading"
    fullscreen
    size="56rpx"
    :text="props.text"
  />
</template>

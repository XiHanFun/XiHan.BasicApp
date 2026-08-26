<script setup lang="ts">
import { ref, shallowRef, watch } from 'vue'
import { scrollRootRef } from '~/composables/useScrollRoot'
import { Icon } from '~/iconify'

defineOptions({ name: 'XihanBackTop' })

// 真正在滚的那个容器由布局登记，交给组件库当滚动源
const target = shallowRef<HTMLElement | null>(scrollRootRef.value)
// 组件库的滚动观察在挂载那一刻认定容器，容器换人就换 key 让它重挂
const trackKey = ref(0)

watch(scrollRootRef, (next) => {
  if (next === target.value)
    return
  target.value = next
  trackKey.value += 1
}, { immediate: true })
</script>

<template>
  <XhBackTopRoot :key="trackKey" :target="target">
    <XhBackTopTrigger>
      <Icon icon="lucide:chevron-up" width="18" height="18" />
    </XhBackTopTrigger>
  </XhBackTopRoot>
</template>

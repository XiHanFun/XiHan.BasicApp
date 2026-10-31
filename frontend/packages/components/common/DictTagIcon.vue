<!-- 字典图标：先装载已安装的离线图标集再挂载，保证非预载图标刷新后仍可见。 -->
<script setup lang="ts">
import { ref, watch } from 'vue'
import { Icon } from '~/iconify'
import { ensureIconLoaded } from '~/iconify/offline'

const props = defineProps<{ icon: string }>()
const ready = ref(false)
// 本地图标包不能取消import；切换与卸载时使旧回调失效，不新增网络轮询或计时器。
watch(() => props.icon, async (icon, _previous, onCleanup) => {
  let active = true
  onCleanup(() => {
    active = false
  })
  ready.value = false
  const loaded = await ensureIconLoaded(icon)
  if (active)
    ready.value = loaded
}, { immediate: true })
</script>

<template>
  <Icon v-if="ready" :key="icon" :icon="icon" aria-hidden="true" />
</template>

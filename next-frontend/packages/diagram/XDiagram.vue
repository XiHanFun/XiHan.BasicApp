<script setup lang="ts">
import type { DiagramApi, DiagramOptions } from './types'
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { createDiagram } from './create-diagram'
import { DiagramTeleport } from './vue-shape'

/**
 * 通用图编辑器容器组件
 *
 * 挂载后创建画布并 emit ready(api)，消费方持 DiagramApi 完成装载/编辑/导出；
 * 高度由外层布局给定（容器需有确定高度）。
 */

const props = defineProps<{
  options?: DiagramOptions
}>()

const emit = defineEmits<{
  ready: [api: DiagramApi]
}>()

const containerRef = ref<HTMLElement | null>(null)
const minimapRef = ref<HTMLElement | null>(null)
let api: DiagramApi | null = null

onMounted(() => {
  if (!containerRef.value)
    return
  const options = { ...props.options }
  if (options.minimap && minimapRef.value)
    options.minimapContainer = minimapRef.value
  api = createDiagram(containerRef.value, options)
  emit('ready', api)
})

onBeforeUnmount(() => {
  api?.dispose()
  api = null
})
</script>

<template>
  <div class="relative h-full w-full">
    <div ref="containerRef" class="h-full w-full" />
    <div
      v-show="props.options?.minimap"
      ref="minimapRef"
      class="absolute bottom-2 right-2 overflow-hidden rounded border border-gray-200 bg-white/90 shadow-sm dark:border-gray-700 dark:bg-gray-800/90"
    />
    <DiagramTeleport />
  </div>
</template>

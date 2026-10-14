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
      class="x-diagram__minimap absolute bottom-2 right-2 overflow-hidden"
    />
    <DiagramTeleport />
  </div>
</template>

<style scoped>
/* 缩略图的面板与 X6 注入的部件样式都换成语义令牌：X6 自带白底、浅灰投影和青色视口框，暗色下会是一块白板 */
.x-diagram__minimap {
  border: var(--xh-stroke-thin) solid var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
  background-color: var(--xh-bg-surface);
  box-shadow: var(--xh-shadow-sm);
}

.x-diagram__minimap :deep(.x6-widget-minimap) {
  background-color: transparent;
}

.x-diagram__minimap :deep(.x6-widget-minimap .x6-graph) {
  box-shadow: none;
}

/* 视口框的线宽与负外边距是 X6 定位用的，只换颜色 */
.x-diagram__minimap :deep(.x6-widget-minimap-viewport) {
  border-color: var(--xh-fg-brand);
}

.x-diagram__minimap :deep(.x6-widget-minimap-viewport-zoom) {
  border-color: var(--xh-fg-brand);
  background-color: var(--xh-bg-surface);
}
</style>

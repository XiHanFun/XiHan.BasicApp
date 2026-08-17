<script setup lang="ts">
import { useSortable } from '@dnd-kit/vue/sortable'
import { ref } from 'vue'

defineOptions({ name: 'XSortableItem', inheritAttrs: true })

const props = defineProps<{
  /** 排序项唯一标识（需与数据源一致） */
  id: string | number
  /** 当前在列表中的索引 */
  index: number
  /** 拖拽手柄的 CSS 选择器；留空则整体可拖 */
  handle?: string
  /** 是否禁用拖拽（同时禁用作为放置目标） */
  disabled?: boolean
}>()

const el = ref<HTMLElement | null>(null)

const { isDragging } = useSortable({
  id: () => props.id,
  index: () => props.index,
  element: el,
  handle: () =>
    props.handle ? (el.value?.querySelector<HTMLElement>(props.handle) ?? undefined) : undefined,
  disabled: () => props.disabled ?? false,
})
</script>

<template>
  <div ref="el" :data-dragging="isDragging || undefined">
    <slot :is-dragging="isDragging" />
  </div>
</template>

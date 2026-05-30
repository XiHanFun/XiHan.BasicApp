<script setup lang="ts">
import type { SearchFieldSetting } from './useSearchSettings'
import { NButton, NDivider, NIcon, NPopover, NSwitch, NTooltip } from 'naive-ui'
import Sortable from 'sortablejs'
import { nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { Icon } from '~/iconify'

defineOptions({ name: 'SchemaSearchSettings' })

defineProps<{
  /** 搜索字段设置（来自 useSearchSettings.settings） */
  settings: SearchFieldSetting[]
}>()

const emit = defineEmits<{
  'togglePin': [key: string, value: boolean]
  'move': [fromIndex: number, toIndex: number]
  'reset': []
}>()

// ── 拖拽排序（sortablejs，仅手柄可拖） ──────────────────────────
const listRef = ref<HTMLElement | null>(null)
const popoverShow = ref(false)
let sortable: Sortable | null = null

function initSortable() {
  if (!listRef.value || sortable) {
    return
  }
  sortable = Sortable.create(listRef.value, {
    handle: '.xh-search-drag-handle',
    animation: 150,
    ghostClass: 'xh-search-drag-ghost',
    onEnd(evt) {
      const { oldIndex, newIndex } = evt
      if (oldIndex == null || newIndex == null || oldIndex === newIndex) {
        return
      }
      // 撤销 Sortable 的真实 DOM 移动，交回 Vue 数据驱动重渲染
      const parent = evt.from
      const movedNode = evt.item
      movedNode.remove()
      const reference = parent.children[oldIndex] ?? null
      parent.insertBefore(movedNode, reference)
      emit('move', oldIndex, newIndex)
    },
  })
}

watch(popoverShow, async (visible) => {
  if (visible) {
    await nextTick()
    initSortable()
  }
})

onBeforeUnmount(() => {
  sortable?.destroy()
  sortable = null
})
</script>

<template>
  <NPopover v-model:show="popoverShow" trigger="click" placement="bottom-end" :width="280" display-directive="show">
    <template #trigger>
      <NTooltip>
        <template #trigger>
          <NButton size="small" quaternary circle aria-label="搜索设置">
            <template #icon>
              <NIcon><Icon icon="lucide:settings-2" /></NIcon>
            </template>
          </NButton>
        </template>
        搜索设置
      </NTooltip>
    </template>

    <div class="flex flex-col gap-2">
      <div class="flex items-center justify-between">
        <span class="text-xs text-foreground/60">搜索条件（固定=常用区，拖拽排序）</span>
        <NButton size="tiny" quaternary @click="emit('reset')">
          恢复默认
        </NButton>
      </div>

      <div ref="listRef" class="flex flex-col max-h-72 overflow-auto">
        <div
          v-for="item in settings"
          :key="item.key"
          class="xh-search-set-row flex gap-2 items-center py-1"
        >
          <span class="xh-search-drag-handle flex items-center cursor-grab text-foreground/40" title="拖拽排序">
            <NIcon><Icon icon="lucide:grip-vertical" /></NIcon>
          </span>
          <span class="flex-1 text-sm truncate">{{ item.title }}</span>
          <span class="text-xs text-foreground/45">{{ item.pinned ? '常用' : '高级' }}</span>
          <NSwitch
            :value="item.pinned"
            size="small"
            @update:value="(value) => emit('togglePin', item.key, value as boolean)"
          />
        </div>
      </div>

      <NDivider class="!my-1" />
      <span class="text-xs text-foreground/40">关闭「固定」的条件将收入高级搜索</span>
    </div>
  </NPopover>
</template>

<style scoped>
.xh-search-drag-handle:active {
  cursor: grabbing;
}

.xh-search-drag-ghost {
  opacity: 0.5;
  background: rgb(var(--primary) / 0.08);
}
</style>

<script setup lang="ts">
import type { SearchFieldSetting } from './useSearchSettings'
import { NButton, NCheckbox, NDivider, NIcon, NPopover, NSwitch, NTooltip } from 'naive-ui'
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
  'toggleVisible': [key: string, value: boolean]
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
    handle: '.xh-set-drag-handle',
    animation: 150,
    ghostClass: 'xh-set-drag-ghost',
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
  <NPopover v-model:show="popoverShow" trigger="click" placement="bottom-end" :width="300" display-directive="show">
    <template #trigger>
      <NTooltip>
        <template #trigger>
          <NButton circle size="small" quaternary aria-label="搜索设置">
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
        <span class="text-xs text-foreground/60">搜索条件（勾选=显示，固定=常用区）</span>
        <NButton size="tiny" quaternary @click="emit('reset')">
          恢复默认
        </NButton>
      </div>

      <div ref="listRef" class="flex flex-col max-h-72 overflow-auto">
        <div
          v-for="item in settings"
          :key="item.key"
          class="xh-set-row flex gap-2 items-center"
        >
          <span class="xh-set-drag-handle flex items-center cursor-grab text-foreground/40" title="拖拽排序">
            <NIcon><Icon icon="lucide:grip-vertical" /></NIcon>
          </span>
          <NCheckbox
            :checked="item.visible"
            class="xh-set-row__check flex-1"
            @update:checked="(value) => emit('toggleVisible', item.key, value)"
          >
            {{ item.title }}
          </NCheckbox>
          <NTooltip>
            <template #trigger>
              <NSwitch
                :value="item.pinned"
                :disabled="!item.visible"
                size="small"
                @update:value="(value) => emit('togglePin', item.key, value as boolean)"
              />
            </template>
            {{ item.pinned ? '常用搜索区' : '高级搜索区' }}
          </NTooltip>
        </div>
      </div>

      <NDivider class="!my-1" />
      <span class="text-xs text-foreground/40">取消勾选=隐藏该条件；固定=常用区，否则收入高级搜索</span>
    </div>
  </NPopover>
</template>

<style scoped>
/* 统一设置弹窗行样式（与列设置一致） */
.xh-set-row {
  padding: 4px 6px;
  border-radius: 6px;
}

/* 复选框标题钉死 14px，与列设置行标题字号一致 */
.xh-set-row__check :deep(.n-checkbox__label) {
  font-size: 14px;
}

.xh-set-row:hover {
  background: rgb(var(--primary) / 0.06);
}

.xh-set-drag-handle:active {
  cursor: grabbing;
}

.xh-set-drag-ghost {
  opacity: 0.5;
  background: rgb(var(--primary) / 0.08);
}
</style>

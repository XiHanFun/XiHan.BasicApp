<script setup lang="ts">
import type { DragEndEvent } from '@dnd-kit/vue'
import type { SearchFieldSetting } from './useSearchSettings'
import { DragDropProvider } from '@dnd-kit/vue'
import { NButton, NCheckbox, NDivider, NIcon, NPopover, NSwitch, NTooltip } from 'naive-ui'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'
import { resolveSortMove } from '../common/sortable'
import SortableItem from '../common/SortableItem.vue'
import SyncStatusBadge from '../common/SyncStatusBadge.vue'

defineOptions({ name: 'SchemaSearchSettings' })

const appStore = useAppStore()

const props = defineProps<{
  /** 搜索字段设置（来自 useSearchSettings.settings） */
  settings: SearchFieldSetting[]
}>()

const emit = defineEmits<{
  togglePin: [key: string, value: boolean]
  toggleVisible: [key: string, value: boolean]
  move: [fromIndex: number, toIndex: number]
  reset: []
  save: []
}>()

// ── 拖拽排序（@dnd-kit/vue，仅手柄可拖） ──────────────────────────
function onDragEnd(event: DragEndEvent) {
  const move = resolveSortMove(event, props.settings.map(s => s.key))
  if (move) {
    emit('move', move.from, move.to)
  }
}
</script>

<template>
  <NPopover trigger="click" placement="bottom-end" :width="300" display-directive="show">
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
        <div class="flex items-center gap-2">
          <span class="text-base font-semibold text-foreground">搜索设置</span>
          <SyncStatusBadge :synced="appStore.searchSyncEnabled" />
        </div>
        <div class="flex gap-2">
          <NButton size="small" secondary @click="emit('reset')">
            恢复默认
          </NButton>
          <NButton size="small" type="primary" @click="emit('save')">
            保存
          </NButton>
        </div>
      </div>

      <NDivider class="!my-1" />

      <!-- 表头 -->
      <div class="xh-set-head flex gap-2 items-center">
        <span class="xh-set-head__handle" />
        <span class="flex-1">搜索条件</span>
        <span class="xh-set-head__col">常用 / 高级</span>
      </div>

      <DragDropProvider @drag-end="onDragEnd">
        <div class="flex flex-col max-h-72 overflow-auto">
          <SortableItem
            v-for="(item, index) in settings"
            :id="item.key"
            :key="item.key"
            :index="index"
            handle=".xh-set-drag-handle"
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
                <span class="xh-set-row__switch">
                  <NSwitch
                    :value="item.pinned"
                    :disabled="!item.visible"
                    size="small"
                    @update:value="(value) => emit('togglePin', item.key, value as boolean)"
                  />
                </span>
              </template>
              {{ item.pinned ? '常用搜索区（始终展示）' : '高级搜索区（展开后展示）' }}
            </NTooltip>
          </SortableItem>
        </div>
      </DragDropProvider>

      <NDivider class="!my-1" />
      <span class="text-xs text-foreground/40">勾选=显示该条件；开关切换「常用 / 高级」搜索区；拖拽手柄可排序</span>
    </div>
  </NPopover>
</template>

<style scoped>
/* 表头 */
.xh-set-head {
  padding: 2px 6px 6px;
  font-size: 12px;
  color: var(--n-text-color-3, rgb(148 163 184));
  border-bottom: 1px solid rgb(var(--primary) / 0.08);
}

.xh-set-head__handle {
  width: 14px;
  flex-shrink: 0;
}

.xh-set-head__col {
  width: 64px;
  text-align: center;
  flex-shrink: 0;
}

/* 开关列：与表头「常用/高级」列等宽居中对齐 */
.xh-set-row__switch {
  width: 64px;
  display: flex;
  justify-content: center;
  flex-shrink: 0;
}

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

/* 拖拽中的行（dnd-kit 通过 SortableItem 写入 data-dragging） */
.xh-set-row[data-dragging] {
  opacity: 0.5;
  background: rgb(var(--primary) / 0.08);
}
</style>

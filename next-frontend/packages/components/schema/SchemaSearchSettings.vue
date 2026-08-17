<script setup lang="ts">
import type { DragEndEvent } from '@dnd-kit/vue'
import type { SearchFieldSetting } from './useSearchSettings'
import { DragDropProvider } from '@dnd-kit/vue'
import {
  XhButton,
  XhCheckbox,
  XhPopoverContent,
  XhPopoverPositioner,
  XhPopoverRoot,
  XhPopoverTrigger,
  XhSeparator,
  XhSwitch,
} from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'
import { resolveSortMove } from '../common/sortable'
import SortableItem from '../common/SortableItem.vue'
import SyncStatusBadge from '../common/SyncStatusBadge.vue'
import XTooltip from '../common/XTooltip.vue'

defineOptions({ name: 'SchemaSearchSettings' })

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

const { t } = useI18n()
const appStore = useAppStore()

// ── 拖拽排序（@dnd-kit/vue，仅手柄可拖） ──────────────────────────
function onDragEnd(event: DragEndEvent) {
  const move = resolveSortMove(event, props.settings.map(s => s.key))
  if (move) {
    emit('move', move.from, move.to)
  }
}
</script>

<template>
  <XhPopoverRoot placement="bottom-end">
    <!-- 浮层触发器本身就是那颗图标钮；它是 button，不能再往里套一颗 -->
    <XhPopoverTrigger
      class="xh-set-trigger"
      :aria-label="t('component.search_settings.title')"
      :title="t('component.search_settings.title')"
    >
      <Icon icon="lucide:settings-2" />
    </XhPopoverTrigger>
    <XhPopoverPositioner>
      <XhPopoverContent class="xh-set-panel">
        <div class="flex flex-col gap-2">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <span class="text-base font-semibold text-foreground">{{ t('component.search_settings.title') }}</span>
              <SyncStatusBadge :synced="appStore.searchSyncEnabled" />
            </div>
            <div class="flex gap-2">
              <XhButton size="sm" variant="outline" @click="emit('reset')">
                {{ t('component.search_settings.reset') }}
              </XhButton>
              <XhButton size="sm" variant="solid" @click="emit('save')">
                {{ t('component.search_settings.save') }}
              </XhButton>
            </div>
          </div>

          <XhSeparator class="my-1" />

          <!-- 表头 -->
          <div class="xh-set-head flex gap-2 items-center">
            <span class="xh-set-head__handle" />
            <span class="flex-1">{{ t('component.search_settings.column_header') }}</span>
            <span class="xh-set-head__col">{{ t('component.search_settings.mode_header') }}</span>
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
                <span class="xh-set-drag-handle flex items-center cursor-grab text-foreground/40" :title="t('component.search_settings.drag_sort')">
                  <Icon icon="lucide:grip-vertical" />
                </span>
                <!-- 勾选框只有框本身，标签是并排的一段文字，点它也切换 -->
                <XhCheckbox
                  :checked="item.visible"
                  size="sm"
                  :aria-label="item.title"
                  @update:checked="(value: boolean) => emit('toggleVisible', item.key, value)"
                />
                <span
                  class="xh-set-row__label flex-1"
                  @click="emit('toggleVisible', item.key, !item.visible)"
                >
                  {{ item.title }}
                </span>
                <XTooltip :content="item.pinned ? t('component.search_settings.tip_pinned') : t('component.search_settings.tip_advanced')">
                  <span
                    class="xh-set-row__switch"
                  >
                    <XhSwitch
                      :checked="item.pinned"
                      :disabled="!item.visible"
                      size="sm"
                      @update:checked="(value: boolean) => emit('togglePin', item.key, value)"
                    />
                  </span>
                </XTooltip>
              </SortableItem>
            </div>
          </DragDropProvider>

          <XhSeparator class="my-1" />
          <span class="text-xs text-foreground/40">{{ t('component.search_settings.hint') }}</span>
        </div>
      </XhPopoverContent>
    </XhPopoverPositioner>
  </XhPopoverRoot>
</template>

<style scoped>
/* 设置浮层的触发器：与工具栏其它图标钮同款 */
.xh-set-trigger {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  inline-size: 28px;
  block-size: 28px;
  border: 0;
  border-radius: var(--xh-radius-full);
  background: transparent;
  color: var(--xh-fg-muted);
  cursor: pointer;
}

.xh-set-trigger:hover {
  background: var(--xh-bg-subtle-hover);
  color: var(--xh-fg-default);
}

.xh-set-panel {
  inline-size: 340px;
}

/* 表头 */
.xh-set-head {
  padding: 2px 6px 6px;
  font-size: 12px;
  color: var(--xh-fg-subtle);
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

/* 行标题钉死 14px，与列设置行标题字号一致 */
.xh-set-row__label {
  font-size: 14px;
  cursor: pointer;
  min-width: 0;
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

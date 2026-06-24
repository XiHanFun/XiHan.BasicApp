<script setup lang="ts">
import type { DragEndEvent } from '@dnd-kit/vue'
import type { ColumnSetting, TableDensity, TableStyle } from './useTableSettings'
import { DragDropProvider } from '@dnd-kit/vue'
import { NButton, NCheckbox, NDivider, NIcon, NInputNumber, NPopover, NTooltip } from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'
import { resolveSortMove } from '../common/sortable'
import SortableItem from '../common/SortableItem.vue'
import SyncStatusBadge from '../common/SyncStatusBadge.vue'

defineOptions({ name: 'SchemaTableSettings' })

const props = defineProps<{
  /** 列设置（来自 useTableSettings.columns） */
  columns: ColumnSetting[]
  /** 当前密度 */
  density: TableDensity
  /** 表格风格 */
  tableStyle: TableStyle
  /** 是否允许多选 */
  selectable: boolean
  /** 是否显示序号列 */
  showIndex: boolean
}>()

const emit = defineEmits<{
  toggleVisible: [key: string, value: boolean]
  setFixed: [key: string, fixed: 'left' | 'right' | undefined]
  setWidth: [key: string, width: number | undefined]
  move: [fromIndex: number, toIndex: number]
  setDensity: [value: TableDensity]
  setStyle: [key: keyof TableStyle, value: boolean]
  setSelectable: [value: boolean]
  setShowIndex: [value: boolean]
  cycleSort: [key: string]
  reset: []
  save: []
}>()

const { t } = useI18n()
const appStore = useAppStore()

const densityOptions = computed<Array<{ label: string, value: TableDensity }>>(() => [
  { label: t('component.schema_table_settings.density_small'), value: 'small' },
  { label: t('component.schema_table_settings.density_medium'), value: 'medium' },
  { label: t('component.schema_table_settings.density_large'), value: 'large' },
])

const styleOptions = computed<Array<{ label: string, key: keyof TableStyle, invert?: boolean }>>(() => [
  { label: t('component.schema_table_settings.striped'), key: 'striped' },
  { label: t('component.schema_table_settings.bordered'), key: 'bordered' },
  // Naive single-line=true 表示「无竖线」，与按钮直觉相反，故反向显示：选中=有竖线
  { label: t('component.schema_table_settings.single_line'), key: 'singleLine', invert: true },
])

/** 排序图标（无 → 升 → 降，与「固定」同款单图标循环切换；优先级由列在列表中的顺序决定） */
function sortIcon(sort?: 'asc' | 'desc'): string {
  if (sort === 'asc') {
    return 'lucide:arrow-up-narrow-wide'
  }
  if (sort === 'desc') {
    return 'lucide:arrow-down-wide-narrow'
  }
  return 'lucide:arrow-up-down'
}

function sortLabel(sort?: 'asc' | 'desc'): string {
  if (sort === 'asc') {
    return t('component.schema_table_settings.sort_asc')
  }
  if (sort === 'desc') {
    return t('component.schema_table_settings.sort_desc')
  }
  return t('component.schema_table_settings.sort_none')
}

/** 固定循环切换：无 → 左 → 右 → 无 */
function nextFixed(current?: 'left' | 'right'): 'left' | 'right' | undefined {
  if (current === undefined) {
    return 'left'
  }
  if (current === 'left') {
    return 'right'
  }
  return undefined
}

/** 固定图标（单图标循环，与排序同款；方向用「钉到左/右边」隐喻，无文字） */
function fixedIcon(fixed?: 'left' | 'right'): string {
  if (fixed === 'left') {
    return 'lucide:arrow-left-to-line'
  }
  if (fixed === 'right') {
    return 'lucide:arrow-right-to-line'
  }
  return 'lucide:pin'
}

function fixedLabel(fixed?: 'left' | 'right'): string {
  if (fixed === 'left') {
    return t('component.schema_table_settings.fixed_left')
  }
  if (fixed === 'right') {
    return t('component.schema_table_settings.fixed_right')
  }
  return t('component.schema_table_settings.fixed_none')
}

// ── 拖拽排序（@dnd-kit/vue，仅手柄可拖） ──────────────────────────
function onDragEnd(event: DragEndEvent) {
  const move = resolveSortMove(event, props.columns.map(c => c.key))
  if (move) {
    emit('move', move.from, move.to)
  }
}
</script>

<template>
  <NPopover trigger="click" placement="bottom-end" :width="340" display-directive="show">
    <template #trigger>
      <NTooltip>
        <template #trigger>
          <NButton circle quaternary size="small" :aria-label="t('component.schema_table_settings.title')">
            <template #icon>
              <NIcon><Icon icon="lucide:settings-2" /></NIcon>
            </template>
          </NButton>
        </template>
        {{ t('component.schema_table_settings.title') }}
      </NTooltip>
    </template>

    <div class="flex flex-col gap-2">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <span class="text-base font-semibold text-foreground">{{ t('component.schema_table_settings.title') }}</span>
          <SyncStatusBadge :synced="appStore.tableSyncEnabled" />
        </div>
        <div class="flex gap-2">
          <NButton size="small" secondary @click="emit('reset')">
            {{ t('component.schema_table_settings.reset') }}
          </NButton>
          <NButton size="small" type="primary" @click="emit('save')">
            {{ t('component.schema_table_settings.save') }}
          </NButton>
        </div>
      </div>

      <NDivider class="!my-1" />

      <!-- 密度 -->
      <div class="flex gap-2 items-center justify-between">
        <span class="text-xs text-foreground/60">{{ t('component.schema_table_settings.density_label') }}</span>
        <div class="flex gap-1">
          <NButton
            v-for="opt in densityOptions"
            :key="opt.value"
            size="tiny"
            :type="density === opt.value ? 'primary' : 'default'"
            @click="emit('setDensity', opt.value)"
          >
            {{ opt.label }}
          </NButton>
        </div>
      </div>

      <!-- 表格风格 -->
      <div class="flex gap-2 items-center justify-between">
        <span class="text-xs text-foreground/60">{{ t('component.schema_table_settings.style_label') }}</span>
        <div class="flex gap-1">
          <NButton
            v-for="opt in styleOptions"
            :key="opt.key"
            size="tiny"
            :type="(opt.invert ? !tableStyle[opt.key] : tableStyle[opt.key]) ? 'primary' : 'default'"
            @click="emit('setStyle', opt.key, !tableStyle[opt.key])"
          >
            {{ opt.label }}
          </NButton>
        </div>
      </div>

      <!-- 功能 -->
      <div class="flex gap-2 items-center justify-between">
        <span class="text-xs text-foreground/60">{{ t('component.schema_table_settings.feature_label') }}</span>
        <div class="flex gap-1">
          <NButton
            size="tiny"
            :type="selectable ? 'primary' : 'default'"
            @click="emit('setSelectable', !selectable)"
          >
            {{ t('component.schema_table_settings.multi_select') }}
          </NButton>
          <NButton
            size="tiny"
            :type="showIndex ? 'primary' : 'default'"
            @click="emit('setShowIndex', !showIndex)"
          >
            {{ t('component.schema_table_settings.index') }}
          </NButton>
        </div>
      </div>

      <NDivider class="!my-1" />

      <!-- 表头 -->
      <div class="xh-set-head flex gap-2 items-center">
        <span class="xh-set-head__handle" />
        <span class="flex-1">{{ t('component.schema_table_settings.column_name') }}</span>
        <span class="xh-set-head__width">{{ t('component.schema_table_settings.column_width') }}</span>
        <span class="xh-set-head__col">{{ t('component.schema_table_settings.sort') }}</span>
        <span class="xh-set-head__col">{{ t('component.schema_table_settings.fixed') }}</span>
      </div>

      <DragDropProvider @drag-end="onDragEnd">
        <div class="flex flex-col max-h-72 overflow-auto">
          <SortableItem
            v-for="(col, index) in columns"
            :id="col.key"
            :key="col.key"
            :index="index"
            handle=".xh-set-drag-handle"
            class="xh-set-row flex gap-2 items-center"
          >
            <span class="xh-set-drag-handle flex items-center cursor-grab text-foreground/40" :title="t('component.schema_table_settings.drag_sort')">
              <NIcon><Icon icon="lucide:grip-vertical" /></NIcon>
            </span>
            <NCheckbox
              :checked="col.visible"
              class="xh-set-row__name flex-1 min-w-0"
              :title="col.title"
              @update:checked="(value) => emit('toggleVisible', col.key, value)"
            >
              {{ col.title }}
            </NCheckbox>
            <span class="xh-set-row__width">
              <NInputNumber
                :value="col.width ?? null"
                size="tiny"
                :show-button="false"
                :update-value-on-input="false"
                :min="60"
                :max="800"
                :placeholder="t('component.schema_table_settings.auto')"
                @update:value="(value: number | null) => emit('setWidth', col.key, value ?? undefined)"
              />
            </span>
            <span class="xh-set-row__sort">
              <NButton
                v-if="col.sortable"
                size="tiny"
                quaternary
                :type="col.sort ? 'primary' : 'default'"
                :title="t('component.schema_table_settings.sort_tip', { label: sortLabel(col.sort) })"
                @click="emit('cycleSort', col.key)"
              >
                <template #icon>
                  <NIcon><Icon :icon="sortIcon(col.sort)" /></NIcon>
                </template>
              </NButton>
              <span v-else class="text-foreground/30">-</span>
            </span>
            <span class="xh-set-row__fixed">
              <NButton
                size="tiny"
                quaternary
                :type="col.fixed ? 'primary' : 'default'"
                :title="t('component.schema_table_settings.fixed_tip', { label: fixedLabel(col.fixed) })"
                @click="emit('setFixed', col.key, nextFixed(col.fixed))"
              >
                <template #icon>
                  <NIcon><Icon :icon="fixedIcon(col.fixed)" /></NIcon>
                </template>
              </NButton>
            </span>
          </SortableItem>
        </div>
      </DragDropProvider>

      <NDivider class="!my-1" />
      <span class="text-xs text-foreground/40">{{ t('component.schema_table_settings.hint') }}</span>
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

.xh-set-head__width {
  width: 58px;
  text-align: center;
  flex-shrink: 0;
}

/* 排序/固定表头：文字单行不换行（兼容中英文 排序/固定 · Sort/Fixed），与行内单图标按钮对齐 */
.xh-set-head__col {
  width: 40px;
  text-align: center;
  white-space: nowrap;
  flex-shrink: 0;
}

/* 列宽输入：与表头「列宽」列等宽（收窄给列名让位，「自动」/三位数仍可容纳） */
.xh-set-row__width {
  width: 58px;
  flex-shrink: 0;
}

/* 排序列：单图标按钮，居中（与表头「排序」列等宽对齐） */
.xh-set-row__sort {
  width: 40px;
  display: flex;
  justify-content: center;
  flex-shrink: 0;
}

/* 固定列：单图标按钮，居中（与表头「固定」列等宽对齐） */
.xh-set-row__fixed {
  width: 40px;
  display: flex;
  justify-content: center;
  flex-shrink: 0;
}

/* 统一设置弹窗行样式（与搜索设置一致） */
.xh-set-row {
  padding: 4px 6px;
  border-radius: 6px;
}

/* 复选框标题钉死 14px，与搜索设置行标题字号一致；超长列名单行省略（不换行、不撑高），完整名见悬停 title */
.xh-set-row :deep(.n-checkbox__label) {
  font-size: 14px;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
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

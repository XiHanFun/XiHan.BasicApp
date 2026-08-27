<script setup lang="ts">
import type { ColumnSetting, TableDensity, TableStyle } from './useTableSettings'
import { XhButton, XhCheckbox, XhPopoverContent, XhPopoverPositioner, XhPopoverRoot, XhPopoverTrigger, XhSeparator, XhSortableItem, XhSortableItemHandle, XhSortableLiveRegion, XhSortableRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'
import SyncStatusBadge from '../common/SyncStatusBadge.vue'
import XNumberInput from '../common/XNumberInput.vue'

defineOptions({ name: 'SchemaTableSettings' })

defineProps<{
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
  // 存的是 single-line：true 表示「无竖线」，与按钮直觉相反，故反向显示：选中=有竖线
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

// ── 拖拽排序（仅手柄可拖） ──────────────────────────────────────
function onSort(details: { from: number, to: number }) {
  emit('move', details.from, details.to)
}
</script>

<template>
  <XhPopoverRoot placement="bottom-end">
    <!-- 浮层触发器本身就是那颗图标钮；它是 button，不能再往里套一颗 -->
    <XhPopoverTrigger
      class="xh-set-trigger"
      :aria-label="t('component.schema_table_settings.title')"
    >
      <Icon icon="lucide:settings-2" />
    </XhPopoverTrigger>
    <XhPopoverPositioner>
      <XhPopoverContent class="xh-set-panel">
        <div class="flex flex-col gap-2">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <span class="text-base font-semibold text-foreground">{{ t('component.schema_table_settings.title') }}</span>
              <SyncStatusBadge :synced="appStore.tableSyncEnabled" />
            </div>
            <div class="flex gap-2">
              <XhButton size="sm" variant="outline" @click="emit('reset')">
                {{ t('component.schema_table_settings.reset') }}
              </XhButton>
              <XhButton size="sm" variant="solid" @click="emit('save')">
                {{ t('component.schema_table_settings.save') }}
              </XhButton>
            </div>
          </div>

          <XhSeparator class="my-1" />

          <!-- 密度 -->
          <div class="flex gap-2 items-center justify-between">
            <span class="text-xs text-foreground/60">{{ t('component.schema_table_settings.density_label') }}</span>
            <div class="flex gap-1">
              <XhButton
                v-for="opt in densityOptions"
                :key="opt.value"
                size="sm"
                class="xh-set-chip"
                :variant="density === opt.value ? 'solid' : 'outline'"
                @click="emit('setDensity', opt.value)"
              >
                {{ opt.label }}
              </XhButton>
            </div>
          </div>

          <!-- 表格风格 -->
          <div class="flex gap-2 items-center justify-between">
            <span class="text-xs text-foreground/60">{{ t('component.schema_table_settings.style_label') }}</span>
            <div class="flex gap-1">
              <XhButton
                v-for="opt in styleOptions"
                :key="opt.key"
                size="sm"
                class="xh-set-chip"
                :variant="(opt.invert ? !tableStyle[opt.key] : tableStyle[opt.key]) ? 'solid' : 'outline'"
                @click="emit('setStyle', opt.key, !tableStyle[opt.key])"
              >
                {{ opt.label }}
              </XhButton>
            </div>
          </div>

          <!-- 功能 -->
          <div class="flex gap-2 items-center justify-between">
            <span class="text-xs text-foreground/60">{{ t('component.schema_table_settings.feature_label') }}</span>
            <div class="flex gap-1">
              <XhButton
                size="sm"
                class="xh-set-chip"
                :variant="selectable ? 'solid' : 'outline'"
                @click="emit('setSelectable', !selectable)"
              >
                {{ t('component.schema_table_settings.multi_select') }}
              </XhButton>
              <XhButton
                size="sm"
                class="xh-set-chip"
                :variant="showIndex ? 'solid' : 'outline'"
                @click="emit('setShowIndex', !showIndex)"
              >
                {{ t('component.schema_table_settings.index') }}
              </XhButton>
            </div>
          </div>

          <XhSeparator class="my-1" />

          <!-- 表头 -->
          <div class="xh-set-head flex gap-2 items-center">
            <span class="xh-set-head__handle" />
            <span class="flex-1">{{ t('component.schema_table_settings.column_name') }}</span>
            <span class="xh-set-head__width">{{ t('component.schema_table_settings.column_width') }}</span>
            <span class="xh-set-head__col">{{ t('component.schema_table_settings.sort') }}</span>
            <span class="xh-set-head__col">{{ t('component.schema_table_settings.fixed') }}</span>
          </div>

          <XhSortableRoot
            :ids="columns.map(x => x.key)"
            class="flex flex-col max-h-72 overflow-auto"
            style="--xh-sortable-gap: 0"
            @sort="onSort"
          >
            <XhSortableItem
              v-for="col in columns"
              :key="col.key"
              :item-id="col.key"
              class="xh-set-row flex gap-2 items-center"
            >
              <XhSortableItemHandle
                :item-id="col.key"
                class="xh-set-drag-handle flex items-center text-foreground/40"
                :title="t('component.schema_table_settings.drag_sort')"
              >
                <Icon icon="lucide:grip-vertical" />
              </XhSortableItemHandle>
              <!-- 勾选框只有框本身，列名是并排的一段文字，点它也切换 -->
              <XhCheckbox
                :checked="col.visible"
                size="sm"
                :aria-label="col.title"
                @update:checked="(value: boolean) => emit('toggleVisible', col.key, value)"
              />
              <span
                class="xh-set-row__name flex-1 min-w-0"
                :title="col.title"
                @click="emit('toggleVisible', col.key, !col.visible)"
              >
                {{ col.title }}
              </span>
              <span class="xh-set-row__width">
                <XNumberInput
                  :value="col.width ?? null"
                  size="sm"
                  :show-button="false"
                  :min="60"
                  :max="800"
                  :placeholder="t('component.schema_table_settings.auto')"
                  @update:value="(raw: string | number | (string | number)[] | null) => { const value = raw as number | null; emit('setWidth', col.key, value ?? undefined) }"
                />
              </span>
              <span class="xh-set-row__sort">
                <XhButton
                  v-if="col.sortable"
                  size="sm"
                  class="xh-set-chip"
                  variant="ghost"
                  :tone="col.sort ? 'brand' : 'neutral'"
                  :title="t('component.schema_table_settings.sort_tip', { label: sortLabel(col.sort) })"
                  @click="emit('cycleSort', col.key)"
                >
                  <Icon :icon="sortIcon(col.sort)" />
                </XhButton>
                <span v-else class="text-foreground/30">-</span>
              </span>
              <span class="xh-set-row__fixed">
                <XhButton
                  size="sm"
                  class="xh-set-chip"
                  variant="ghost"
                  :tone="col.fixed ? 'brand' : 'neutral'"
                  :title="t('component.schema_table_settings.fixed_tip', { label: fixedLabel(col.fixed) })"
                  @click="emit('setFixed', col.key, nextFixed(col.fixed))"
                >
                  <Icon :icon="fixedIcon(col.fixed)" />
                </XhButton>
              </span>
            </XhSortableItem>
            <XhSortableLiveRegion />
          </XhSortableRoot>

          <XhSeparator class="my-1" />
          <span class="text-xs text-foreground/40">{{ t('component.schema_table_settings.hint') }}</span>
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

/* 选项按钮比头部动作按钮低一档：组件库最小档是 sm(28px)，没有更小的一档 */
.xh-set-chip {
  --xh-button-h: 22px;
  --xh-button-px: 6px;
  --xh-button-font-size: 12px;
}

.xh-set-panel {
  /* 皮肤给浮层的宽高上限是 rem 值，本应用根字号 14px 会把它们压到 280px / 224px，
     不显式盖掉的话这个面板会被夹窄、列清单也露不出来 */
  inline-size: 340px;
  max-inline-size: 340px;
  --xh-popover-max-h: 560px;
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

/* 输入控件自带一道 12rem 的固有最小宽，不收就会顶穿这个格子、
   盖住右侧的排序与固定两栏，并给列表挤出一条横向滚动条 */
.xh-set-row__width :deep([data-scope='number-field'][data-part='control']) {
  inline-size: 100%;
  min-inline-size: 0;
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

/* 列名钉死 14px，与搜索设置行标题字号一致；超长列名单行省略（不换行、不撑高），完整名见悬停 title。
   选本组件自己的类名——复选框没写默认插槽时只渲染一颗裸 button，没有 text 那个部件 */
.xh-set-row__name {
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

/* 拖拽中的行（sortable 在被拖那一项上写 data-dragging） */
.xh-set-row[data-dragging] {
  opacity: 0.5;
  background: rgb(var(--primary) / 0.08);
}
</style>

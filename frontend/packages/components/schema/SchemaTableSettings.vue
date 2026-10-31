<!-- 表格设置面板：分别编辑数据列和序号列对齐，所有变更由父级显式保存。 -->
<script setup lang="ts">
import type { ColumnSetting, TableAlign, TableDensity, TableStyle } from './useTableSettings'
import { XhButton, XhCheckbox, XhPopoverContent, XhPopoverPositioner, XhPopoverRoot, XhPopoverTitle, XhPopoverTrigger, XhSeparator, XhSortableItem, XhSortableItemDragTrigger, XhSortableLiveRegion, XhSortableRoot, XhSwitch, XhToggleGroupItem, XhToggleGroupRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'
import SyncStatusBadge from '../common/SyncStatusBadge.vue'
import XNumberInput from '../common/XNumberInput.vue'
import XSelect from '../common/XSelect.vue'

defineOptions({ name: 'SchemaTableSettings' })

const props = defineProps<{
  /** 列设置（来自 useTableSettings.columns） */
  columns: ColumnSetting[]
  /** 当前密度 */
  density: TableDensity
  /** 表格风格 */
  tableStyle: TableStyle
  /** 表格对齐 */
  align: TableAlign
  /** 序号列独立对齐；旧调用方不传时保持居左。 */
  indexAlign?: TableAlign
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
  setAlign: [value: TableAlign]
  setIndexAlign: [value: TableAlign]
  setColumnAlign: [key: string, value: TableAlign | undefined]
  setSelectable: [value: boolean]
  setShowIndex: [value: boolean]
  cycleSort: [key: string]
  reset: []
  save: []
}>()

const { t } = useI18n()
const appStore = useAppStore()

const densityOptions = computed<Array<{ label: string, value: TableDensity }>>(() => [
  { label: t('component.schema_table_settings.density_small'), value: 'sm' },
  { label: t('component.schema_table_settings.density_medium'), value: 'md' },
  { label: t('component.schema_table_settings.density_large'), value: 'lg' },
])

const alignOptions = computed<Array<{ label: string, value: TableAlign }>>(() => [
  { label: t('component.schema_table_settings.align_left'), value: 'left' },
  { label: t('component.schema_table_settings.align_center'), value: 'center' },
  { label: t('component.schema_table_settings.align_right'), value: 'right' },
])

const INHERIT_ALIGNMENT = 'inherit'
const columnAlignOptions = computed(() => [
  { label: t('component.schema_table_settings.align_default'), value: INHERIT_ALIGNMENT },
  ...alignOptions.value,
])

const styleOptions = computed<Array<{ label: string, key: keyof TableStyle }>>(() => [
  { label: t('component.schema_table_settings.striped'), key: 'striped' },
  { label: t('component.schema_table_settings.bordered'), key: 'bordered' },
  { label: t('component.schema_table_settings.ruled'), key: 'ruled' },
])

/** 风格是一组可同时开的开关：选中集合即开着的那几项 */
const styleValue = computed(() => styleOptions.value
  .filter(opt => props.tableStyle[opt.key])
  .map(opt => opt.key))

function onDensityChange(value: string | string[] | null) {
  // 密度单选且不许清空，这里只会收到一个值
  if (typeof value === 'string')
    emit('setDensity', value as TableDensity)
}

function onAlignChange(value: string | string[] | null) {
  // 对齐单选且不许清空，这里只会收到一个值
  if (typeof value === 'string')
    emit('setAlign', value as TableAlign)
}

/**
 * 仅接受显示中的序号列的合法对齐选项，隐藏时保留原草稿。
 * @param value 单选控件提交的方向。
 * @returns {void} 无返回值；由父级更新偏好。
 */
function onIndexAlignChange(value: unknown): void {
  if (props.showIndex && (value === 'left' || value === 'center' || value === 'right'))
    emit('setIndexAlign', value)
}

/**
 * 下拉只提交已知对齐值；默认项移除覆盖而非写死当前表格方向。
 * @param key 列字段键。
 * @param value 下拉选中值。
 * @returns {void} 只更新对应列的草稿。
 */
function onColumnAlignChange(key: string, value: unknown): void {
  if (value === INHERIT_ALIGNMENT)
    emit('setColumnAlign', key, undefined)
  else if (value === 'left' || value === 'center' || value === 'right')
    emit('setColumnAlign', key, value)
}

function onStyleChange(value: string | string[] | null) {
  const next = new Set(Array.isArray(value) ? value : value == null ? [] : [value])
  for (const opt of styleOptions.value) {
    const shown = next.has(opt.key)
    if (shown !== props.tableStyle[opt.key])
      emit('setStyle', opt.key, shown)
  }
}

/** 多选功能沿用原开关；序号列在独立行直接控制。 */
const FEATURE_SELECTABLE = 'selectable'
const featureValue = computed(() => props.selectable ? [FEATURE_SELECTABLE] : [])

function onFeatureChange(value: string | string[] | null) {
  const next = new Set(Array.isArray(value) ? value : value == null ? [] : [value])
  if (next.has(FEATURE_SELECTABLE) !== props.selectable)
    emit('setSelectable', next.has(FEATURE_SELECTABLE))
}

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
    <!-- 浮层触发器合到组件库的纯图标按钮上，与同排的 XIconButton 同为 sm 档、同高 -->
    <XhPopoverTrigger as-child>
      <XhButton
        class="x-icon-btn"
        variant="ghost"
        size="sm"
        icon-only
        :aria-label="t('component.schema_table_settings.title')"
      >
        <Icon icon="lucide:settings-2" />
      </XhButton>
    </XhPopoverTrigger>
    <XhPopoverPositioner>
      <XhPopoverContent class="xh-set-panel">
        <div class="flex flex-col gap-2">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <!-- 标题部件给浮层起名（content 的 aria-labelledby 指向它） -->
              <XhPopoverTitle class="text-base font-semibold text-foreground">
                {{ t('component.schema_table_settings.title') }}
              </XhPopoverTitle>
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

          <!-- 密度：三档单选、不许清空 -->
          <div class="flex gap-2 items-center justify-between">
            <span class="text-xs text-foreground/60">{{ t('component.schema_table_settings.density_label') }}</span>
            <XhToggleGroupRoot
              size="sm"
              :value="density"
              disallow-empty
              :aria-label="t('component.schema_table_settings.density_label')"
              @update:value="onDensityChange"
            >
              <XhToggleGroupItem v-for="opt in densityOptions" :key="opt.value" :value="opt.value">
                {{ opt.label }}
              </XhToggleGroupItem>
            </XhToggleGroupRoot>
          </div>

          <!-- 表格风格：三个可同时开的开关 -->
          <div class="flex gap-2 items-center justify-between">
            <span class="text-xs text-foreground/60">{{ t('component.schema_table_settings.style_label') }}</span>
            <XhToggleGroupRoot
              size="sm"
              multiple
              :value="styleValue"
              :aria-label="t('component.schema_table_settings.style_label')"
              @update:value="onStyleChange"
            >
              <XhToggleGroupItem v-for="opt in styleOptions" :key="opt.key" :value="opt.key">
                {{ opt.label }}
              </XhToggleGroupItem>
            </XhToggleGroupRoot>
          </div>

          <!-- 对齐：数据列一起换，三档单选、不许清空 -->
          <div class="flex gap-2 items-center justify-between">
            <span class="text-xs text-foreground/60">{{ t('component.schema_table_settings.align_label') }}</span>
            <XhToggleGroupRoot
              size="sm"
              :value="align"
              disallow-empty
              :aria-label="t('component.schema_table_settings.align_label')"
              @update:value="onAlignChange"
            >
              <XhToggleGroupItem v-for="opt in alignOptions" :key="opt.value" :value="opt.value">
                {{ opt.label }}
              </XhToggleGroupItem>
            </XhToggleGroupRoot>
          </div>

          <!-- 复用原 showIndex 偏好，显式开关默认开启；关闭仅隐藏，不丢弃对齐设置。 -->
          <div class="flex gap-2 items-center justify-between">
            <span class="text-xs text-foreground/60">{{ t('component.schema_table_settings.show_index') }}</span>
            <div class="flex items-center gap-2">
              <XhSwitch :checked="showIndex" :aria-label="t('component.schema_table_settings.show_index')" @update:checked="(value: boolean) => emit('setShowIndex', value)" />
              <span class="xh-set-row__align">
                <XSelect :value="indexAlign ?? 'left'" :options="alignOptions" :disabled="!showIndex" :aria-label="t('component.schema_table_settings.index_align_label')" @update:value="onIndexAlignChange" />
              </span>
            </div>
          </div>

          <!-- 功能：多选列开关 -->
          <div class="flex gap-2 items-center justify-between">
            <span class="text-xs text-foreground/60">{{ t('component.schema_table_settings.feature_label') }}</span>
            <XhToggleGroupRoot
              size="sm"
              multiple
              :value="featureValue"
              :aria-label="t('component.schema_table_settings.feature_label')"
              @update:value="onFeatureChange"
            >
              <XhToggleGroupItem :value="FEATURE_SELECTABLE">
                {{ t('component.schema_table_settings.multi_select') }}
              </XhToggleGroupItem>
            </XhToggleGroupRoot>
          </div>

          <XhSeparator class="my-1" />

          <!-- 表头 -->
          <div class="xh-set-head flex gap-2 items-center">
            <span class="xh-set-head__handle" />
            <span class="flex-1">{{ t('component.schema_table_settings.column_name') }}</span>
            <span class="xh-set-head__width">{{ t('component.schema_table_settings.column_width') }}</span>
            <span class="xh-set-row__align text-center">{{ t('component.schema_table_settings.column_align') }}</span>
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
              <XhSortableItemDragTrigger
                :item-id="col.key"
                class="xh-set-drag-handle flex items-center text-foreground/40"
                :title="t('component.schema_table_settings.drag_sort')"
              >
                <Icon icon="lucide:grip-vertical" />
              </XhSortableItemDragTrigger>
              <!-- 勾选框只有框本身，列名是并排的一段文字，点它也切换 -->
              <XhCheckbox
                class="xh-set-visible"
                :checked="col.visible"
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
              <span class="xh-set-row__width" :data-label="t('component.schema_table_settings.column_width')">
                <XNumberInput
                  :value="col.width ?? null"
                  size="sm"
                  :show-button="false"
                  :min="60"
                  :max="800"
                  :placeholder="t('component.schema_table_settings.auto')"
                  :aria-label="`${col.title} · ${t('component.schema_table_settings.column_width')}`"
                  @update:value="(raw: string | number | (string | number)[] | null) => { const value = raw as number | null; emit('setWidth', col.key, value ?? undefined) }"
                />
              </span>
              <span class="xh-set-row__align" :data-label="t('component.schema_table_settings.column_align')">
                <XSelect
                  :value="col.align ?? INHERIT_ALIGNMENT"
                  :options="columnAlignOptions"
                  :aria-label="`${col.title} · ${t('component.schema_table_settings.column_align')}`"
                  :title="t('component.schema_table_settings.align_default_hint')"
                  @update:value="value => onColumnAlignChange(col.key, value)"
                />
              </span>
              <span class="xh-set-row__sort">
                <XhButton
                  v-if="col.sortable"
                  size="sm"
                  class="xh-set-chip"
                  variant="ghost"
                  icon-only
                  :tone="col.sort ? 'brand' : 'neutral'"
                  :aria-label="`${col.title} · ${t('component.schema_table_settings.sort')}: ${sortLabel(col.sort)}`"
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
                  icon-only
                  :tone="col.fixed ? 'brand' : 'neutral'"
                  :aria-label="`${col.title} · ${t('component.schema_table_settings.fixed')}: ${fixedLabel(col.fixed)}`"
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
/* 选项按钮比头部动作按钮低一档：组件库最小档是 sm(28px)，没有更小的一档 */
.xh-set-chip {
  --xh-button-h: 22px;
  --xh-button-px: 6px;
  --xh-button-font-size: 12px;
}

.xh-set-panel {
  /* 皮肤给浮层的宽高上限是 rem 值，本应用根字号 14px 会把它们压到 280px / 224px，
     不显式盖掉的话这个面板会被夹窄、列清单也露不出来 */
  inline-size: min(34rem, calc(100vw - 2rem));
  max-inline-size: calc(100vw - 2rem);
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

/* 下拉只占一格，移动端允许列名省略；控制部件不使用组件的默认最小宽度。 */
.xh-set-row__align {
  inline-size: 6rem;
  flex-shrink: 0;
  min-inline-size: 0;
}

.xh-set-row__align :deep([data-scope='select'][data-part='root']),
.xh-set-row__align :deep([data-scope='select'][data-part='control']),
.xh-set-row__align :deep([data-scope='select'][data-part='trigger']) {
  inline-size: 100%;
  min-inline-size: 0;
}
/* 窄屏把数值/下拉放到下一行，保留完整列名，避免新增控件把名称挤成单字。 */
@media (max-width: 32rem) {
  .xh-set-head {
    display: none;
  }

  .xh-set-row {
    display: grid;
    grid-template-columns: 1rem 1.25rem minmax(0, 1fr) 3rem 3rem;
    align-items: center;
  }

  .xh-set-drag-handle {
    grid-area: 1 / 1;
  }
  .xh-set-visible {
    grid-area: 1 / 2;
  }
  .xh-set-row__name {
    grid-area: 1 / 3;
  }
  .xh-set-row__sort {
    grid-area: 1 / 4;
  }
  .xh-set-row__fixed {
    grid-area: 1 / 5;
  }
  .xh-set-row__width {
    grid-area: 2 / 3;
  }
  .xh-set-row .xh-set-row__align {
    grid-area: 2 / 4 / 3 / 6;
  }

  .xh-set-row__width,
  .xh-set-row .xh-set-row__align {
    display: grid;
    inline-size: 100%;
    gap: 0.25rem;
  }

  .xh-set-row__width::before,
  .xh-set-row .xh-set-row__align::before {
    content: attr(data-label);
    color: var(--xh-fg-subtle);
    font-size: 0.75rem;
  }
}
</style>

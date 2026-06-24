<script setup lang="ts">
import type { DragEndEvent } from '@dnd-kit/vue'
import type { ColumnSetting, TableDefaultSort, TableDensity, TableStyle } from './useTableSettings'
import { DragDropProvider } from '@dnd-kit/vue'
import { NButton, NCheckbox, NDivider, NIcon, NInputNumber, NPopover, NSelect, NTooltip } from 'naive-ui'
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
  /** 可排序列（field.sortable 的列，作为默认排序候选） */
  sortableColumns: Array<{ key: string, title: string }>
  /** 当前默认多字段排序（数组顺序即优先级；空表示无） */
  defaultSorts: TableDefaultSort[]
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
  setDefaultSorts: [rules: TableDefaultSort[]]
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

/** 某行的可选列：全部可排序列，但排除已被其它行占用的字段（每字段至多一条排序规则） */
function rowFieldOptions(index: number) {
  const usedByOthers = new Set(props.defaultSorts.filter((_, i) => i !== index).map(r => r.field))
  return props.sortableColumns
    .filter(c => !usedByOthers.has(c.key))
    .map(c => ({ label: c.title, value: c.key }))
}

/** 尚未被任一行占用的可排序列（用于「添加排序」是否可用） */
const unusedSortableColumns = computed(() => {
  const used = new Set(props.defaultSorts.map(r => r.field))
  return props.sortableColumns.filter(c => !used.has(c.key))
})

function emitSorts(rules: TableDefaultSort[]) {
  emit('setDefaultSorts', rules)
}

/** 追加一条排序规则（默认取首个未占用列、升序） */
function addSortRule() {
  const next = unusedSortableColumns.value[0]
  if (!next) {
    return
  }
  emitSorts([...props.defaultSorts, { field: next.key, order: 'asc' }])
}

function setRuleField(index: number, field: string) {
  emitSorts(props.defaultSorts.map((r, i) => (i === index ? { ...r, field } : r)))
}

function setRuleOrder(index: number, order: 'asc' | 'desc') {
  emitSorts(props.defaultSorts.map((r, i) => (i === index ? { ...r, order } : r)))
}

function removeRule(index: number) {
  emitSorts(props.defaultSorts.filter((_, i) => i !== index))
}

/** 调整优先级：与相邻行互换（offset = -1 上移 / +1 下移） */
function moveRule(index: number, offset: number) {
  const target = index + offset
  if (target < 0 || target >= props.defaultSorts.length) {
    return
  }
  const next = [...props.defaultSorts]
  const moved = next[index]
  if (!moved) {
    return
  }
  next.splice(index, 1)
  next.splice(target, 0, moved)
  emitSorts(next)
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

      <!-- 默认多字段排序（列表打开时的初始排序；自上而下为优先级；仅可排序列可选） -->
      <div v-if="sortableColumns.length" class="flex flex-col gap-1">
        <div class="flex items-center justify-between">
          <span class="text-xs text-foreground/60">{{ t('component.schema_table_settings.default_sort_label') }}</span>
          <NButton
            size="tiny"
            quaternary
            :disabled="!unusedSortableColumns.length"
            @click="addSortRule"
          >
            <template #icon>
              <NIcon><Icon icon="lucide:plus" /></NIcon>
            </template>
            {{ t('component.schema_table_settings.default_sort_add') }}
          </NButton>
        </div>
        <div
          v-for="(rule, index) in defaultSorts"
          :key="rule.field"
          class="flex gap-1 items-center"
        >
          <span class="xh-sort-prio text-xs text-foreground/40">{{ index + 1 }}</span>
          <NSelect
            :value="rule.field"
            :options="rowFieldOptions(index)"
            size="tiny"
            class="flex-1"
            @update:value="(v: string) => setRuleField(index, v)"
          />
          <NButton size="tiny" :type="rule.order === 'asc' ? 'primary' : 'default'" @click="setRuleOrder(index, 'asc')">
            {{ t('component.schema_table_settings.sort_asc') }}
          </NButton>
          <NButton size="tiny" :type="rule.order === 'desc' ? 'primary' : 'default'" @click="setRuleOrder(index, 'desc')">
            {{ t('component.schema_table_settings.sort_desc') }}
          </NButton>
          <NButton size="tiny" quaternary :disabled="index === 0" :title="t('component.schema_table_settings.sort_move_up')" @click="moveRule(index, -1)">
            <template #icon>
              <NIcon><Icon icon="lucide:chevron-up" /></NIcon>
            </template>
          </NButton>
          <NButton size="tiny" quaternary :disabled="index === defaultSorts.length - 1" :title="t('component.schema_table_settings.sort_move_down')" @click="moveRule(index, 1)">
            <template #icon>
              <NIcon><Icon icon="lucide:chevron-down" /></NIcon>
            </template>
          </NButton>
          <NButton size="tiny" quaternary :title="t('common.actions.delete')" @click="removeRule(index)">
            <template #icon>
              <NIcon><Icon icon="lucide:x" /></NIcon>
            </template>
          </NButton>
        </div>
      </div>

      <NDivider class="!my-1" />

      <!-- 表头 -->
      <div class="xh-set-head flex gap-2 items-center">
        <span class="xh-set-head__handle" />
        <span class="flex-1">{{ t('component.schema_table_settings.column_name') }}</span>
        <span class="xh-set-head__width">{{ t('component.schema_table_settings.column_width') }}</span>
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
              class="flex-1 min-w-0"
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
            <span class="xh-set-row__fixed">
              <NButton
                size="tiny"
                quaternary
                :title="t('component.schema_table_settings.fixed_tip', { label: fixedLabel(col.fixed) })"
                @click="emit('setFixed', col.key, nextFixed(col.fixed))"
              >
                <template #icon>
                  <NIcon><Icon icon="lucide:pin" /></NIcon>
                </template>
                {{ fixedLabel(col.fixed) }}
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

/* 默认排序：优先级序号（与各行对齐） */
.xh-sort-prio {
  width: 12px;
  text-align: center;
  flex-shrink: 0;
}

.xh-set-head__width {
  width: 72px;
  text-align: center;
  flex-shrink: 0;
}

.xh-set-head__col {
  width: 56px;
  text-align: center;
  flex-shrink: 0;
}

/* 列宽输入：与表头「列宽」列等宽 */
.xh-set-row__width {
  width: 72px;
  flex-shrink: 0;
}

/* 固定列：与表头「固定」列等宽居中对齐 */
.xh-set-row__fixed {
  width: 56px;
  display: flex;
  justify-content: center;
  flex-shrink: 0;
}

/* 统一设置弹窗行样式（与搜索设置一致） */
.xh-set-row {
  padding: 4px 6px;
  border-radius: 6px;
}

/* 复选框标题钉死 14px，与搜索设置行标题字号一致 */
.xh-set-row :deep(.n-checkbox__label) {
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

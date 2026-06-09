<script setup lang="ts">
import type { DragEndEvent } from '@dnd-kit/vue'
import type { ColumnSetting, TableDensity, TableStyle } from './useTableSettings'
import { DragDropProvider } from '@dnd-kit/vue'
import { NButton, NCheckbox, NDivider, NIcon, NInputNumber, NPopover, NTooltip } from 'naive-ui'
import { Icon } from '~/iconify'
import { resolveSortMove } from '../common/sortable'
import SortableItem from '../common/SortableItem.vue'

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
  reset: []
  save: []
}>()

const densityOptions: Array<{ label: string, value: TableDensity }> = [
  { label: '紧凑', value: 'small' },
  { label: '默认', value: 'medium' },
  { label: '宽松', value: 'large' },
]

const styleOptions: Array<{ label: string, key: keyof TableStyle, invert?: boolean }> = [
  { label: '斑马纹', key: 'striped' },
  { label: '边框', key: 'bordered' },
  // Naive single-line=true 表示「无竖线」，与按钮直觉相反，故反向显示：选中=有竖线
  { label: '单线', key: 'singleLine', invert: true },
]

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
    return '左'
  }
  if (fixed === 'right') {
    return '右'
  }
  return '－'
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
          <NButton circle quaternary size="small" aria-label="表格设置">
            <template #icon>
              <NIcon><Icon icon="lucide:settings-2" /></NIcon>
            </template>
          </NButton>
        </template>
        表格设置
      </NTooltip>
    </template>

    <div class="flex flex-col gap-2">
      <div class="flex items-center justify-between">
        <span class="text-base font-semibold text-foreground">表格设置</span>
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

      <!-- 密度 -->
      <div class="flex gap-2 items-center justify-between">
        <span class="text-xs text-foreground/60">表格密度</span>
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
        <span class="text-xs text-foreground/60">表格风格</span>
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
        <span class="text-xs text-foreground/60">功能</span>
        <div class="flex gap-1">
          <NButton
            size="tiny"
            :type="selectable ? 'primary' : 'default'"
            @click="emit('setSelectable', !selectable)"
          >
            多选
          </NButton>
          <NButton
            size="tiny"
            :type="showIndex ? 'primary' : 'default'"
            @click="emit('setShowIndex', !showIndex)"
          >
            序号
          </NButton>
        </div>
      </div>

      <NDivider class="!my-1" />

      <!-- 表头 -->
      <div class="xh-set-head flex gap-2 items-center">
        <span class="xh-set-head__handle" />
        <span class="flex-1">列名</span>
        <span class="xh-set-head__width">列宽</span>
        <span class="xh-set-head__col">固定</span>
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
            <span class="xh-set-drag-handle flex items-center cursor-grab text-foreground/40" title="拖拽排序">
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
                placeholder="自动"
                @update:value="(value: number | null) => emit('setWidth', col.key, value ?? undefined)"
              />
            </span>
            <span class="xh-set-row__fixed">
              <NButton
                size="tiny"
                quaternary
                :title="`固定：${fixedLabel(col.fixed)}`"
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
      <span class="text-xs text-foreground/40">勾选=显示该列；列宽可在此输入或拖动表头右边框调整（留空为自动）；点钉选图标在「左 / 右 / 不固定」间循环；拖拽手柄可排序</span>
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

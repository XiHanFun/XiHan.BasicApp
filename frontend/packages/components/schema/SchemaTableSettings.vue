<script setup lang="ts">
import type { ColumnSetting, TableDensity } from './useTableSettings'
import { NButton, NCheckbox, NDivider, NIcon, NPopover, NTooltip } from 'naive-ui'
import Sortable from 'sortablejs'
import { nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { Icon } from '~/iconify'

defineOptions({ name: 'SchemaTableSettings' })

defineProps<{
  /** 列设置（来自 useTableSettings.columns） */
  columns: ColumnSetting[]
  /** 当前密度 */
  density: TableDensity
}>()

const emit = defineEmits<{
  'toggleVisible': [key: string, value: boolean]
  'setFixed': [key: string, fixed: 'left' | 'right' | undefined]
  'move': [fromIndex: number, toIndex: number]
  'setDensity': [value: TableDensity]
  'reset': []
}>()

const densityOptions: Array<{ label: string, value: TableDensity }> = [
  { label: '紧凑', value: 'small' },
  { label: '默认', value: 'medium' },
  { label: '宽松', value: 'large' },
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
      // 撤销 Sortable 对真实 DOM 的移动，交回 Vue 以数据驱动重渲染（避免 DOM 与 vdom 不一致）
      const parent = evt.from
      const movedNode = evt.item
      movedNode.remove()
      const reference = parent.children[oldIndex] ?? null
      parent.insertBefore(movedNode, reference)
      emit('move', oldIndex, newIndex)
    },
  })
}

// 首次展开 Popover 时初始化拖拽（内容延迟挂载，需等 DOM 就绪）
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
          <NButton circle quaternary size="small" aria-label="列设置">
            <template #icon>
              <NIcon><Icon icon="lucide:settings-2" /></NIcon>
            </template>
          </NButton>
        </template>
        列设置
      </NTooltip>
    </template>

    <div class="flex flex-col gap-2">
      <div class="flex items-center justify-between">
        <span class="text-base font-semibold text-foreground">列设置</span>
        <NButton size="small" type="primary" secondary @click="emit('reset')">
          恢复默认
        </NButton>
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

      <NDivider class="!my-1" />

      <!-- 表头 -->
      <div class="xh-set-head flex gap-2 items-center">
        <span class="xh-set-head__handle" />
        <span class="flex-1">列名</span>
        <span class="xh-set-head__col">固定</span>
      </div>

      <div ref="listRef" class="flex flex-col max-h-72 overflow-auto">
        <div
          v-for="col in columns"
          :key="col.key"
          class="xh-set-row flex gap-2 items-center"
        >
          <span class="xh-set-drag-handle flex items-center cursor-grab text-foreground/40" title="拖拽排序">
            <NIcon><Icon icon="lucide:grip-vertical" /></NIcon>
          </span>
          <NCheckbox
            :checked="col.visible"
            class="flex-1"
            @update:checked="(value) => emit('toggleVisible', col.key, value)"
          >
            {{ col.title }}
          </NCheckbox>
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
        </div>
      </div>

      <NDivider class="!my-1" />
      <span class="text-xs text-foreground/40">勾选=显示该列；点钉选图标在「左 / 右 / 不固定」间循环；拖拽手柄可排序</span>
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
  width: 56px;
  text-align: center;
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

.xh-set-drag-ghost {
  opacity: 0.5;
  background: rgb(var(--primary) / 0.08);
}
</style>

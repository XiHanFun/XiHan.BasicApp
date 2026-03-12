<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import { NButton, NCheckbox, NPopover } from 'naive-ui'
import { Icon } from '~/iconify'
import Sortable from 'sortablejs'
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import type {
  ColumnPropertySettings,
  ColumnSettingItem,
  GlobalSettings,
  TableLayout,
} from './types'

const LAYOUT_OPTIONS: { value: TableLayout; label: string }[] = [
  { value: 'compact', label: '紧凑' },
  { value: 'default', label: '默认' },
  { value: 'loose', label: '宽松' },
]

const FIXED_CYCLE: ('left' | 'right' | false)[] = [false, 'left', 'right']

const props = withDefaults(
  defineProps<{
    columns: DataTableColumns<any>
    modelValue?: ColumnPropertySettings
    storageKey?: string
  }>(),
  {
    modelValue: undefined,
    storageKey: undefined,
  },
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: ColumnPropertySettings): void
}>()

const visible = ref(false)
const listRef = ref<HTMLElement | null>(null)
let sortableInstance: Sortable | null = null

function resolveColumnKey(column: DataTableColumns<any>[number], index: number) {
  const key = (column as any)?.key
  if (key !== undefined && key !== null) {
    return String(key)
  }
  const type = (column as any)?.type
  if (type) {
    return `__type__${String(type)}`
  }
  return `__index__${index}`
}

function resolveColumnTitle(column: DataTableColumns<any>[number], index: number) {
  const title = (column as any)?.title
  if (typeof title === 'string' && title.trim().length > 0) {
    return title
  }
  const key = (column as any)?.key
  if (typeof key === 'string' && key.trim().length > 0) {
    return key
  }
  return `列${index + 1}`
}

function getDefaultFixed(column: DataTableColumns<any>[number]): 'left' | 'right' | false {
  const fixed = (column as any)?.fixed
  if (fixed === 'left' || fixed === 'right') {
    return fixed
  }
  return false
}

function getColumnDisplayTitle(column: DataTableColumns<any>[number], index: number): string {
  const key = resolveColumnKey(column, index)
  if (key === '__type__selection') {
    return '多选'
  }
  if (key === '__type__index') {
    return '序号'
  }
  return resolveColumnTitle(column, index)
}

function buildDefaultSettings(): ColumnPropertySettings {
  const defaultGlobal: GlobalSettings = {
    showCheckbox: true,
    showIndex: true,
    layout: 'default',
  }
  const columns = props.columns.map((column, index) => ({
    key: resolveColumnKey(column, index),
    title: getColumnDisplayTitle(column, index),
    visible: true,
    locked: Boolean((column as any)?.type),
    fixed: getDefaultFixed(column),
  }))
  return { global: defaultGlobal, columns }
}

const settings = computed({
  get: () => props.modelValue ?? buildDefaultSettings(),
  set: (v) => emit('update:modelValue', v),
})

const columnItems = computed({
  get: () => settings.value.columns,
  set: (v) => {
    settings.value = { ...settings.value, columns: v }
  },
})

const globalSettings = computed({
  get: () => settings.value.global,
  set: (v) => {
    settings.value = { ...settings.value, global: v }
  },
})

const selectableColumns = computed(() => columnItems.value.filter((item) => !item.locked))

const allSelected = computed({
  get: () => selectableColumns.value.length > 0 && selectableColumns.value.every((c) => c.visible),
  set: (checked: boolean) => {
    columnItems.value = columnItems.value.map((item) =>
      item.locked ? item : { ...item, visible: checked },
    )
  },
})

const indeterminate = computed(() => {
  const visibleCount = selectableColumns.value.filter((c) => c.visible).length
  return visibleCount > 0 && visibleCount < selectableColumns.value.length
})

function updateColumnVisible(key: string, checked: boolean) {
  columnItems.value = columnItems.value.map((item) =>
    item.key === key ? { ...item, visible: item.locked ? true : checked } : item,
  )
}

function cycleColumnFixed(key: string) {
  columnItems.value = columnItems.value.map((item) => {
    if (item.key !== key) {
      return item
    }
    const current = item.fixed ?? false
    const idx = FIXED_CYCLE.indexOf(current)
    const next = FIXED_CYCLE[(idx + 1) % FIXED_CYCLE.length]
    return { ...item, fixed: next }
  })
}

function updateGlobal<K extends keyof GlobalSettings>(key: K, value: GlobalSettings[K]) {
  globalSettings.value = { ...globalSettings.value, [key]: value }
}

function resetAll() {
  settings.value = buildDefaultSettings()
}

function destroySortable() {
  if (sortableInstance) {
    sortableInstance.destroy()
    sortableInstance = null
  }
}

function initSortable() {
  if (!listRef.value) {
    return
  }
  destroySortable()
  sortableInstance = Sortable.create(listRef.value, {
    animation: 150,
    handle: '.x-column-setter__drag',
    draggable: '.x-column-setter__item',
    onEnd(event) {
      if (
        event.oldIndex === undefined ||
        event.newIndex === undefined ||
        event.oldIndex === event.newIndex
      ) {
        return
      }
      const next = [...columnItems.value]
      const moved = next.splice(event.oldIndex, 1)[0]
      if (!moved) {
        return
      }
      next.splice(event.newIndex, 0, moved)
      columnItems.value = next
    },
  })
}

watch(visible, async (v) => {
  if (v) {
    await nextTick()
    initSortable()
    return
  }
  destroySortable()
})

watch(
  () => props.columns,
  () => {
    if (!props.modelValue) {
      settings.value = buildDefaultSettings()
    }
  },
  { deep: true },
)

onBeforeUnmount(() => {
  destroySortable()
})

defineExpose({
  resetAll,
})
</script>

<template>
  <NPopover
    v-model:show="visible"
    trigger="click"
    placement="bottom-end"
    :content-style="{ overflow: 'hidden' }"
  >
    <template #trigger>
      <slot name="trigger">
        <NButton size="small">
          <template #icon>
            <Icon icon="lucide:settings-2" width="16" />
          </template>
          列设置
        </NButton>
      </slot>
    </template>
    <div class="x-column-setter">
      <!-- 总设置 -->
      <section class="x-column-setter__block">
        <div class="x-column-setter__block-head">
          <Icon icon="lucide:sliders-horizontal" class="x-column-setter__block-icon" />
          <span>总设置</span>
        </div>
        <div class="x-column-setter__block-body">
          <div class="x-column-setter__switches">
            <NCheckbox
              :checked="globalSettings.showCheckbox"
              size="small"
              @update:checked="(v) => updateGlobal('showCheckbox', !!v)"
            >
              多选框
            </NCheckbox>
            <NCheckbox
              :checked="globalSettings.showIndex"
              size="small"
              @update:checked="(v) => updateGlobal('showIndex', !!v)"
            >
              序号
            </NCheckbox>
          </div>
          <div class="x-column-setter__layout">
            <span class="x-column-setter__layout-label">布局</span>
            <div class="x-column-setter__layout-options">
              <button
                v-for="opt in LAYOUT_OPTIONS"
                :key="opt.value"
                type="button"
                class="x-column-setter__layout-btn"
                :class="{ 'is-active': globalSettings.layout === opt.value }"
                @click="updateGlobal('layout', opt.value)"
              >
                {{ opt.label }}
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- 列设置 -->
      <section class="x-column-setter__block">
        <div class="x-column-setter__block-head x-column-setter__block-head--row">
          <div class="x-column-setter__block-title">
            <Icon icon="lucide:columns-3" class="x-column-setter__block-icon" />
            <span>列设置</span>
          </div>
          <div class="x-column-setter__block-actions">
            <NCheckbox
              :checked="allSelected"
              :indeterminate="indeterminate"
              size="small"
              @update:checked="(v) => (allSelected = !!v)"
            >
              全选
            </NCheckbox>
            <button type="button" class="x-column-setter__reset" @click="resetAll">
              <Icon icon="lucide:rotate-ccw" width="12" />
              重置
            </button>
          </div>
        </div>
        <div ref="listRef" class="x-column-setter__list">
          <div v-for="item in columnItems" :key="item.key" class="x-column-setter__item">
            <span class="x-column-setter__drag" title="拖拽排序">
              <Icon icon="lucide:grip-vertical" width="14" />
            </span>
            <NCheckbox
              :checked="item.visible"
              :disabled="item.locked"
              size="small"
              class="x-column-setter__col-check"
              @update:checked="(v) => updateColumnVisible(item.key, !!v)"
            >
              {{ item.title }}
            </NCheckbox>
            <button
              type="button"
              class="x-column-setter__fixed-btn"
              :title="
                (item.fixed ?? false) === false
                  ? '不固定'
                  : item.fixed === 'left'
                    ? '左固定'
                    : '右固定'
              "
              @click.stop="cycleColumnFixed(item.key)"
            >
              <Icon v-if="(item.fixed ?? false) === false" icon="lucide:pin-off" width="12" />
              <Icon v-else-if="item.fixed === 'left'" icon="lucide:panel-left" width="12" />
              <Icon v-else icon="lucide:panel-right" width="12" />
            </button>
          </div>
        </div>
      </section>
    </div>
  </NPopover>
</template>

<style scoped>
.x-column-setter {
  width: 300px;
  max-height: min(440px, 80vh);
  overflow: hidden;
  padding: 4px;
  display: flex;
  flex-direction: column;
}

.x-column-setter__block {
  margin-bottom: 16px;
  flex-shrink: 0;
}

.x-column-setter__block:last-child {
  margin-bottom: 0;
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.x-column-setter__block-head {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 4px 10px;
  font-size: 12px;
  font-weight: 600;
  color: hsl(var(--muted-foreground));
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.x-column-setter__block-head--row {
  justify-content: space-between;
  align-items: center;
}

.x-column-setter__block-title {
  display: flex;
  align-items: center;
  gap: 6px;
}

.x-column-setter__block-icon {
  width: 14px;
  height: 14px;
  opacity: 0.7;
}

.x-column-setter__block-body {
  padding: 12px;
  background: hsl(var(--muted) / 0.35);
  border-radius: 8px;
}

.x-column-setter__switches {
  display: flex;
  gap: 20px;
  margin-bottom: 12px;
}

.x-column-setter__layout {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.x-column-setter__layout-label {
  font-size: 11px;
  color: hsl(var(--muted-foreground));
}

.x-column-setter__layout-options {
  display: flex;
  gap: 4px;
}

.x-column-setter__layout-btn {
  flex: 1;
  padding: 6px 10px;
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  border-radius: 6px;
  cursor: pointer;
  transition:
    color 0.15s ease,
    border-color 0.15s ease,
    background 0.15s ease;
}

.x-column-setter__layout-btn:hover {
  color: hsl(var(--foreground));
  border-color: hsl(var(--muted-foreground) / 0.5);
}

.x-column-setter__layout-btn.is-active {
  color: hsl(var(--primary));
  background: hsl(var(--primary) / 0.1);
  border-color: hsl(var(--primary) / 0.4);
}

.x-column-setter__block-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.x-column-setter__reset {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  font-size: 11px;
  color: hsl(var(--muted-foreground));
  background: transparent;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  transition:
    color 0.15s,
    background 0.15s;
}

.x-column-setter__reset:hover {
  color: hsl(var(--primary));
  background: hsl(var(--primary) / 0.08);
}

.x-column-setter__list {
  display: flex;
  flex-direction: column;
  gap: 4px;
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 8px;
  background: hsl(var(--muted) / 0.35);
  border-radius: 8px;
}

.x-column-setter__item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border) / 0.6);
  border-radius: 6px;
  transition:
    border-color 0.15s,
    box-shadow 0.15s;
}

.x-column-setter__item:hover {
  border-color: hsl(var(--border));
  box-shadow: 0 1px 3px hsl(var(--foreground) / 0.04);
}

.x-column-setter__drag {
  cursor: move;
  color: hsl(var(--muted-foreground) / 0.8);
  user-select: none;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  padding: 2px;
  border-radius: 4px;
  transition:
    color 0.15s,
    background 0.15s;
}

.x-column-setter__drag:hover {
  color: hsl(var(--primary));
  background: hsl(var(--primary) / 0.08);
}

.x-column-setter__col-check {
  flex: 1;
  min-width: 0;
}

.x-column-setter__col-check :deep(.n-checkbox__label) {
  font-size: 13px;
}

.x-column-setter__fixed-btn {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  color: hsl(var(--muted-foreground) / 0.8);
  background: transparent;
  border: none;
  border-radius: 5px;
  cursor: pointer;
  transition:
    color 0.15s,
    background 0.15s;
}

.x-column-setter__fixed-btn:hover {
  color: hsl(var(--primary));
  background: hsl(var(--primary) / 0.1);
}
</style>

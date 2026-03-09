<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import { NButton, NCard, NCheckbox, NDataTable, NPagination, NPopover } from 'naive-ui'
import Sortable from 'sortablejs'
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { LocalStorage } from '~/utils'

interface PaginationConfig {
  page: number
  pageSize: number
  total: number
  pageSizes?: number[]
  showQuickJumper?: boolean
}

interface ColumnSettingItem {
  key: string
  title: string
  visible: boolean
  locked: boolean
}

const props = withDefaults(defineProps<{
  columns: DataTableColumns<any>
  data: Record<string, any>[]
  pagination?: PaginationConfig
  loading?: boolean
  rowKey?: (row: any) => number | string
  scrollX?: number
  storageKey?: string
  striped?: boolean
  size?: 'small' | 'medium' | 'large'
  defaultExpandAll?: boolean
  showToolbar?: boolean
  showRefresh?: boolean
  showPagination?: boolean
}>(), {
  loading: false,
  rowKey: undefined,
  scrollX: undefined,
  storageKey: undefined,
  striped: true,
  size: 'small',
  defaultExpandAll: false,
  showToolbar: true,
  showRefresh: true,
  showPagination: true,
})

const emit = defineEmits<{
  (e: 'update:page', value: number): void
  (e: 'update:pageSize', value: number): void
  (e: 'refresh'): void
}>()

const columnSettings = ref<ColumnSettingItem[]>([])
const settingsVisible = ref(false)
const settingsListRef = ref<HTMLElement | null>(null)
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

function buildDefaultSettings() {
  return props.columns.map((column, index) => ({
    key: resolveColumnKey(column, index),
    title: resolveColumnTitle(column, index),
    visible: true,
    locked: Boolean((column as any)?.type),
  }))
}

function applyPersistedSettings(defaultSettings: ColumnSettingItem[]) {
  if (!props.storageKey) {
    return defaultSettings
  }
  const stored = LocalStorage.get<Array<{ key: string, visible: boolean }>>(props.storageKey)
  if (!stored || stored.length === 0) {
    return defaultSettings
  }

  const defaultMap = new Map(defaultSettings.map(item => [item.key, item]))
  const merged: ColumnSettingItem[] = []

  for (const savedItem of stored) {
    const base = defaultMap.get(savedItem.key)
    if (!base) {
      continue
    }
    merged.push({
      ...base,
      visible: base.locked ? true : savedItem.visible !== false,
    })
  }

  for (const defaultItem of defaultSettings) {
    if (!merged.some(item => item.key === defaultItem.key)) {
      merged.push(defaultItem)
    }
  }
  return merged
}

function persistSettings() {
  if (!props.storageKey) {
    return
  }
  LocalStorage.set(
    props.storageKey,
    columnSettings.value.map(item => ({
      key: item.key,
      visible: item.visible,
    })),
  )
}

function syncSettings() {
  const defaults = buildDefaultSettings()
  columnSettings.value = applyPersistedSettings(defaults)
}

const columnMap = computed(() => {
  return new Map(props.columns.map((column, index) => [resolveColumnKey(column, index), column]))
})

const tableColumns = computed<DataTableColumns<any>>(() => {
  return columnSettings.value
    .filter(item => item.visible)
    .map(item => columnMap.value.get(item.key))
    .filter(Boolean) as DataTableColumns<any>
})

const paginationConfig = computed<PaginationConfig>(() => {
  return props.pagination ?? {
    page: 1,
    pageSize: 20,
    total: 0,
  }
})

function updateColumnVisible(key: string, checked: boolean) {
  columnSettings.value = columnSettings.value.map(item =>
    item.key === key
      ? {
          ...item,
          visible: item.locked ? true : checked,
        }
      : item,
  )
  persistSettings()
}

function resetColumnSettings() {
  columnSettings.value = buildDefaultSettings()
  persistSettings()
}

function destroySortable() {
  if (sortableInstance) {
    sortableInstance.destroy()
    sortableInstance = null
  }
}

function initSortable() {
  if (!settingsListRef.value) {
    return
  }
  destroySortable()
  sortableInstance = Sortable.create(settingsListRef.value, {
    animation: 150,
    handle: '.x-pro-table__drag',
    draggable: '.x-pro-table__item',
    onEnd(event) {
      if (event.oldIndex === undefined || event.newIndex === undefined || event.oldIndex === event.newIndex) {
        return
      }
      const next = [...columnSettings.value]
      const moved = next.splice(event.oldIndex, 1)[0]
      if (!moved) {
        return
      }
      next.splice(event.newIndex, 0, moved)
      columnSettings.value = next
      persistSettings()
    },
  })
}

watch(
  () => props.columns,
  () => {
    syncSettings()
  },
  { deep: true, immediate: true },
)

watch(settingsVisible, async (visible) => {
  if (visible) {
    await nextTick()
    initSortable()
    return
  }
  destroySortable()
})

onBeforeUnmount(() => {
  destroySortable()
})
</script>

<template>
  <NCard :bordered="false">
    <div v-if="props.showToolbar" class="mb-3 flex items-center justify-between gap-3">
      <div class="flex items-center gap-2">
        <slot name="toolbar-left" />
      </div>
      <div class="flex items-center gap-2">
        <slot name="toolbar-right" />
        <NButton v-if="props.showRefresh" size="small" @click="emit('refresh')">
          刷新
        </NButton>
        <NPopover v-model:show="settingsVisible" trigger="click" placement="bottom-end">
          <template #trigger>
            <NButton size="small">
              列设置
            </NButton>
          </template>
          <div class="x-pro-table__panel">
            <div class="x-pro-table__panel-header">
              <span class="text-xs text-gray-400">拖拽排序 / 勾选显示</span>
              <NButton text size="tiny" type="primary" @click="resetColumnSettings">
                重置
              </NButton>
            </div>
            <div ref="settingsListRef" class="x-pro-table__list">
              <div
                v-for="item in columnSettings"
                :key="item.key"
                class="x-pro-table__item"
              >
                <span class="x-pro-table__drag">::</span>
                <NCheckbox
                  :checked="item.visible"
                  :disabled="item.locked"
                  @update:checked="(checked) => updateColumnVisible(item.key, checked)"
                >
                  {{ item.title }}
                </NCheckbox>
              </div>
            </div>
          </div>
        </NPopover>
      </div>
    </div>

    <NDataTable
      :columns="tableColumns"
      :data="props.data"
      :loading="props.loading"
      :row-key="props.rowKey"
      :pagination="false"
      :scroll-x="props.scrollX"
      :default-expand-all="props.defaultExpandAll"
      :striped="props.striped"
      :size="props.size"
    />

    <div v-if="props.showPagination" class="mt-4 flex justify-end">
      <NPagination
        :page="paginationConfig.page"
        :page-size="paginationConfig.pageSize"
        :item-count="paginationConfig.total"
        :page-sizes="paginationConfig.pageSizes ?? [10, 20, 50, 100]"
        :show-quick-jumper="paginationConfig.showQuickJumper ?? true"
        show-size-picker
        @update:page="(page) => emit('update:page', page)"
        @update:page-size="(size) => emit('update:pageSize', size)"
      />
    </div>
  </NCard>
</template>

<style scoped>
.x-pro-table__panel {
  width: 260px;
}

.x-pro-table__panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.x-pro-table__list {
  max-height: 320px;
  overflow: auto;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.x-pro-table__item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 8px;
  border-radius: 6px;
  border: 1px solid hsl(var(--border));
}

.x-pro-table__drag {
  cursor: move;
  color: hsl(var(--muted-foreground));
  user-select: none;
  letter-spacing: -1px;
  font-size: 12px;
}
</style>

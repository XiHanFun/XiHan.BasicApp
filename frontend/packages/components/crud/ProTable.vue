<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import { NButton, NCard, NDataTable, NPagination } from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { LocalStorage } from '~/utils'
import CrudColumnPropertySetter from './ColumnPropertySetter.vue'
import type { ColumnPropertySettings, ColumnSettingItem, TableLayout } from './types'

interface PaginationConfig {
  page: number
  pageSize: number
  total: number
  pageSizes?: number[]
  showQuickJumper?: boolean
}

const props = withDefaults(
  defineProps<{
    columns: DataTableColumns<any>
    data: Record<string, any>[]
    pagination?: PaginationConfig
    loading?: boolean
    rowKey?: (row: any) => number | string
    scrollX?: number
    maxHeight?: number | string
    storageKey?: string
    striped?: boolean
    defaultExpandAll?: boolean
    showToolbar?: boolean
    showRefresh?: boolean
    showPagination?: boolean
    compact?: boolean
    stickyPagination?: boolean
    paginationBottomOffset?: number | string
  }>(),
  {
    loading: false,
    rowKey: undefined,
    scrollX: undefined,
    maxHeight: undefined,
    storageKey: undefined,
    striped: true,
    defaultExpandAll: false,
    showToolbar: true,
    showRefresh: true,
    showPagination: true,
    compact: true,
    stickyPagination: false,
    paginationBottomOffset: 0,
  },
)

const emit = defineEmits<{
  (e: 'update:page', value: number): void
  (e: 'update:pageSize', value: number): void
  (e: 'refresh'): void
}>()

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

function resolveColumnTitle(column: DataTableColumns<any>[number], index: number): string {
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

const effectiveColumns = computed<DataTableColumns<any>>(() => {
  const hasSelection = props.columns.some((col: any) => col?.type === 'selection')
  const hasIndex = props.columns.some((col: any) => col?.type === 'index')
  const result: DataTableColumns<any> = []
  if (!hasSelection) {
    result.push({ type: 'selection', fixed: 'left' } as any)
  }
  if (!hasIndex) {
    result.push({ type: 'index', title: '序号', width: 60 } as any)
  }
  return [...result, ...props.columns]
})

function buildDefaultSettings(): ColumnPropertySettings {
  const defaultGlobal = {
    showCheckbox: true,
    showIndex: true,
    layout: 'default' as TableLayout,
  }
  const effectiveCols = effectiveColumns.value
  const columns = effectiveCols.map((column, index) => ({
    key: resolveColumnKey(column, index),
    title: resolveColumnTitle(column, index),
    visible: true,
    locked: Boolean((column as any)?.type),
    fixed: getDefaultFixed(column),
  }))
  return { global: defaultGlobal, columns }
}

function applyPersistedSettings(defaultSettings: ColumnPropertySettings): ColumnPropertySettings {
  if (!props.storageKey) {
    return defaultSettings
  }
  const stored = LocalStorage.get<ColumnPropertySettings>(`${props.storageKey}_column_settings`)
  if (!stored?.columns?.length) {
    return defaultSettings
  }
  const savedMap = new Map(stored.columns.map(c => [c.key, c]))
  const mergedColumns = defaultSettings.columns.map(def => {
    const saved = savedMap.get(def.key)
    if (!saved) {
      return def
    }
    return {
      ...def,
      visible: def.locked ? true : saved.visible !== false,
      fixed: saved.fixed ?? def.fixed,
    }
  })
  return {
    global: { ...defaultSettings.global, ...stored.global },
    columns: mergedColumns,
  }
}

function persistSettings() {
  if (!props.storageKey) {
    return
  }
  LocalStorage.set(`${props.storageKey}_column_settings`, columnSettings.value)
}

const columnSettings = ref<ColumnPropertySettings>(buildDefaultSettings())

function syncSettings() {
  const defaults = buildDefaultSettings()
  columnSettings.value = applyPersistedSettings(defaults)
}

const columnMap = computed(() =>
  new Map(effectiveColumns.value.map((col, i) => [resolveColumnKey(col, i), col])),
)

const layoutToSize = (layout: TableLayout): 'small' | 'medium' | 'large' => {
  if (layout === 'compact') {
    return 'small'
  }
  if (layout === 'loose') {
    return 'large'
  }
  return 'medium'
}

const tableColumns = computed<DataTableColumns<any>>(() => {
  const { global, columns } = columnSettings.value
  const size = layoutToSize(global.layout)
  return columns
    .filter(item => {
      if (!item.visible) {
        return false
      }
      if (item.key === '__type__selection' && !global.showCheckbox) {
        return false
      }
      if (item.key === '__type__index' && !global.showIndex) {
        return false
      }
      return true
    })
    .map(item => {
      const col = columnMap.value.get(item.key) as any
      if (!col) {
        return null
      }
      const fixed = item.fixed ?? false
      return { ...col, fixed: fixed || undefined }
    })
    .filter(Boolean) as DataTableColumns<any>
})

const tableSize = computed(() =>
  layoutToSize(columnSettings.value.global.layout),
)

const paginationConfig = computed<PaginationConfig>(() =>
  props.pagination ?? { page: 1, pageSize: 20, total: 0 },
)

const totalPages = computed(() => {
  const pageSize = Math.max(1, paginationConfig.value.pageSize)
  return Math.max(1, Math.ceil(paginationConfig.value.total / pageSize))
})

const canPrev = computed(() => paginationConfig.value.page > 1)
const canNext = computed(() => paginationConfig.value.page < totalPages.value)

const paginationStyle = computed(() => ({
  '--x-pro-table-pagination-bottom': typeof props.paginationBottomOffset === 'number'
    ? `${props.paginationBottomOffset}px`
    : props.paginationBottomOffset || '0px',
} as Record<string, string>))

function handlePrevPage() {
  if (!canPrev.value) {
    return
  }
  emit('update:page', paginationConfig.value.page - 1)
}

function handleNextPage() {
  if (!canNext.value) {
    return
  }
  emit('update:page', paginationConfig.value.page + 1)
}

watch(columnSettings, () => {
  persistSettings()
}, { deep: true })

watch(
  () => props.columns,
  () => {
    syncSettings()
  },
  { deep: true, immediate: true },
)
</script>

<template>
  <NCard :bordered="false" :class="{ 'x-pro-table--compact': props.compact }">
    <div v-if="props.showToolbar" class="mb-3 flex items-center justify-between gap-3">
      <div class="flex items-center gap-2">
        <slot name="toolbar-left" />
      </div>
      <div class="flex items-center gap-2">
        <slot name="toolbar-right" />
        <NButton v-if="props.showRefresh" size="small" @click="emit('refresh')">
          刷新
        </NButton>
        <CrudColumnPropertySetter
          v-model="columnSettings"
          :columns="effectiveColumns"
        />
      </div>
    </div>

    <NDataTable
      :columns="tableColumns"
      :data="props.data"
      :loading="props.loading"
      :row-key="props.rowKey"
      :pagination="false"
      :scroll-x="props.scrollX"
      :max-height="props.maxHeight"
      :default-expand-all="props.defaultExpandAll"
      :striped="props.striped"
      :size="tableSize"
    />

    <div
      v-if="props.showPagination"
      class="mt-4 flex items-center justify-end gap-2"
      :class="{ 'x-pro-table__pagination--sticky': props.stickyPagination }"
      :style="paginationStyle"
    >
      <NButton size="small" :disabled="!canPrev" @click="handlePrevPage">
        上一页
      </NButton>
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
      <NButton size="small" :disabled="!canNext" @click="handleNextPage">
        下一页
      </NButton>
    </div>
  </NCard>
</template>

<style scoped>
.x-pro-table--compact :deep(.n-card-header) {
  padding: 10px 12px;
}

.x-pro-table--compact :deep(.n-card__content) {
  padding: 10px 12px;
}

.x-pro-table--compact :deep(.n-data-table-th),
.x-pro-table--compact :deep(.n-data-table-td) {
  padding-top: 6px !important;
  padding-bottom: 6px !important;
}

.x-pro-table__pagination--sticky {
  position: sticky;
  bottom: var(--x-pro-table-pagination-bottom);
  z-index: 6;
  padding: 8px 0;
  background: hsl(var(--background) / 0.92);
  backdrop-filter: blur(4px);
}
</style>

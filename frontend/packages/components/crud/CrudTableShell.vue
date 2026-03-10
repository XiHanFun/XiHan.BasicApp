<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { CrudSearchField } from './types'
import { computed } from 'vue'
import ProTable from './ProTable.vue'
import QueryPanel from './QueryPanel.vue'

interface ShellPaginationConfig {
  page: number
  pageSize: number
  total: number
  pageSizes?: number[]
  showQuickJumper?: boolean
}

type CrudRow = Record<string, unknown>

const props = withDefaults(
  defineProps<{
    searchModel: Record<string, unknown>
    searchFields?: CrudSearchField[]
    searchTitle?: string
    searchDefaultCollapsed?: boolean
    columns: DataTableColumns<CrudRow>
    data: CrudRow[]
    pagination?: ShellPaginationConfig
    loading?: boolean
    rowKey?: (row: CrudRow) => number | string
    scrollX?: number
    maxHeight?: number | string
    storageKey?: string
    striped?: boolean
    size?: 'small' | 'medium' | 'large'
    defaultExpandAll?: boolean
    showToolbar?: boolean
    showRefresh?: boolean
    showPagination?: boolean
    compact?: boolean
    stickySearch?: boolean
    stickySearchTop?: number | string
    stickyPagination?: boolean
    stickyPaginationBottom?: number | string
  }>(),
  {
    searchFields: () => [],
    searchTitle: '查询条件',
    searchDefaultCollapsed: false,
    pagination: undefined,
    loading: false,
    rowKey: undefined,
    scrollX: undefined,
    maxHeight: undefined,
    storageKey: undefined,
    striped: true,
    size: 'small',
    defaultExpandAll: false,
    showToolbar: true,
    showRefresh: true,
    showPagination: true,
    compact: true,
    stickySearch: true,
    stickySearchTop: 0,
    stickyPagination: true,
    stickyPaginationBottom: 0,
  },
)

const emit = defineEmits<{
  (e: 'update:searchModel', value: Record<string, unknown>): void
  (e: 'search', value: Record<string, unknown>): void
  (e: 'reset', value: Record<string, unknown>): void
  (e: 'refresh'): void
  (e: 'update:page', value: number): void
  (e: 'update:pageSize', value: number): void
}>()

const stickySearchStyle = computed(() => {
  const top =
    typeof props.stickySearchTop === 'number'
      ? `${props.stickySearchTop}px`
      : props.stickySearchTop || '0px'

  return {
    '--x-crud-shell-top': top,
  } as Record<string, string>
})

function updateSearchModel(value: Record<string, unknown>) {
  emit('update:searchModel', value)
}
</script>

<template>
  <div class="x-crud-shell" :class="{ 'x-crud-shell--compact': props.compact }">
    <div
      v-if="props.searchFields.length"
      class="x-crud-shell__search"
      :class="{ 'x-crud-shell__search--sticky': props.stickySearch }"
      :style="stickySearchStyle"
    >
      <QueryPanel
        :model-value="props.searchModel"
        :fields="props.searchFields"
        :title="props.searchTitle"
        :default-collapsed="props.searchDefaultCollapsed"
        :compact="props.compact"
        @update:model-value="updateSearchModel"
        @search="(value) => emit('search', value)"
        @reset="(value) => emit('reset', value)"
      >
        <template #actions>
          <slot name="search-actions" />
        </template>
      </QueryPanel>
    </div>

    <ProTable
      :columns="props.columns"
      :data="props.data"
      :pagination="props.pagination"
      :loading="props.loading"
      :row-key="props.rowKey"
      :scroll-x="props.scrollX"
      :max-height="props.maxHeight"
      :storage-key="props.storageKey"
      :striped="props.striped"
      :size="props.size"
      :default-expand-all="props.defaultExpandAll"
      :show-toolbar="props.showToolbar"
      :show-refresh="props.showRefresh"
      :show-pagination="props.showPagination"
      :compact="props.compact"
      :sticky-pagination="props.stickyPagination"
      :pagination-bottom-offset="props.stickyPaginationBottom"
      @refresh="emit('refresh')"
      @update:page="(page) => emit('update:page', page)"
      @update:page-size="(size) => emit('update:pageSize', size)"
    >
      <template #toolbar-left>
        <slot name="toolbar-left" />
      </template>
      <template #toolbar-right>
        <slot name="toolbar-right" />
      </template>
    </ProTable>
  </div>
</template>

<style scoped>
.x-crud-shell {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.x-crud-shell__search--sticky {
  position: sticky;
  top: var(--x-crud-shell-top);
  z-index: 7;
  background: hsl(var(--background) / 0.9);
  backdrop-filter: blur(4px);
}
</style>

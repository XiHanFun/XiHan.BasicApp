<script setup lang="ts" generic="TRow extends object">
import type { DataTableColumn, DataTableSortState } from 'naive-ui'
import { NDataTable, NPagination } from 'naive-ui'
import { computed } from 'vue'
import { toSortParams } from './selectors'

defineOptions({ name: 'SchemaTablePanel' })

const props = withDefaults(defineProps<{
  /** 列定义（由 selectors.toColumns 派生） */
  columns: DataTableColumn<TRow>[]
  /** 数据行 */
  data: TRow[]
  /** 加载态 */
  loading?: boolean
  /** 行主键字段 */
  rowKey?: string
  /** 总条数 */
  total?: number
  /** 当前页 */
  page?: number
  /** 每页数量 */
  pageSize?: number
  /** 横向滚动宽度 */
  scrollX?: number
  /** 每页可选数量 */
  pageSizes?: number[]
  /** 是否启用多选 */
  selectable?: boolean
  /** 已选行主键 */
  checkedKeys?: Array<string | number>
  /** 密度（映射 NDataTable size） */
  density?: 'small' | 'medium' | 'large'
}>(), {
  loading: false,
  rowKey: 'basicId',
  total: 0,
  page: 1,
  pageSize: 20,
  scrollX: undefined,
  pageSizes: () => [10, 20, 50, 100],
  selectable: false,
  checkedKeys: () => [],
  density: 'small',
})

const emit = defineEmits<{
  'update:page': [value: number]
  'update:pageSize': [value: number]
  'update:checkedKeys': [keys: Array<string | number>]
  'sort': [field: string | undefined, order: 'asc' | 'desc' | undefined]
}>()

const pageCount = computed(() => Math.max(1, Math.ceil(props.total / props.pageSize)))

function rowKeyGetter(row: TRow) {
  return (row as Record<string, unknown>)[props.rowKey] as string | number
}

/** 选择列：仅 selectable 时插入到列首 */
const resolvedColumns = computed<DataTableColumn<TRow>[]>(() => {
  if (!props.selectable) {
    return props.columns
  }
  return [{ type: 'selection' } as unknown as DataTableColumn<TRow>, ...props.columns]
})

function onSort(sorter: DataTableSortState | DataTableSortState[] | null) {
  const single = Array.isArray(sorter) ? sorter[0] ?? null : sorter
  const { sortField, sortOrder } = toSortParams(single)
  emit('sort', sortField, sortOrder)
}
</script>

<template>
  <div class="flex flex-col gap-2">
    <NDataTable
      :checked-row-keys="checkedKeys"
      :columns="resolvedColumns"
      :data="data"
      :loading="loading"
      remote
      :row-key="rowKeyGetter"
      :scroll-x="scrollX"
      :size="density"
      striped
      @update:checked-row-keys="(keys) => emit('update:checkedKeys', keys as Array<string | number>)"
      @update:sorter="onSort"
    />
    <div class="flex justify-end">
      <NPagination
        :item-count="total"
        :page="page"
        :page-count="pageCount"
        :page-size="pageSize"
        :page-sizes="pageSizes"
        show-size-picker
        @update:page="(value: number) => emit('update:page', value)"
        @update:page-size="(value: number) => emit('update:pageSize', value)"
      />
    </div>
  </div>
</template>

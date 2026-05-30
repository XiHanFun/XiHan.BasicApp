<script setup lang="ts" generic="TList extends Record<string, unknown>">
import type { DataTableColumns } from 'naive-ui'
import { NDataTable, NPagination } from 'naive-ui'
import { computed } from 'vue'

defineOptions({ name: 'CrudTable' })

const props = withDefaults(defineProps<{
  /** 列定义（Naive UI 原生） */
  columns: DataTableColumns<TList>
  /** 数据行 */
  data: TList[]
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
}>(), {
  loading: false,
  rowKey: 'basicId',
  total: 0,
  page: 1,
  pageSize: 20,
  scrollX: undefined,
  pageSizes: () => [10, 20, 50, 100],
})

const emit = defineEmits<{
  'update:page': [value: number]
  'update:pageSize': [value: number]
}>()

const pageCount = computed(() => Math.max(1, Math.ceil(props.total / props.pageSize)))

function rowKeyGetter(row: TList) {
  return row[props.rowKey] as string | number
}
</script>

<template>
  <div class="flex flex-col gap-2">
    <NDataTable
      :columns="columns"
      :data="data"
      :loading="loading"
      :row-key="rowKeyGetter"
      :scroll-x="scrollX"
      size="small"
      striped
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

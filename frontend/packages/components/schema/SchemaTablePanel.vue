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
  /** 树形模式（启用后不分页、按 childrenKey 展开子行） */
  tree?: boolean
  /** 子节点字段名（默认 children） */
  childrenKey?: string
  /** 默认展开全部（默认 true） */
  defaultExpandAll?: boolean
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
  tree: false,
  childrenKey: 'children',
  defaultExpandAll: true,
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
  <!-- 定高 flex 列：表格占满中段并在内部滚动，分页栏固定底部，整体不撑破父容器 -->
  <div class="xh-table-panel">
    <NDataTable
      class="xh-table-panel__grid"
      flex-height
      :checked-row-keys="checkedKeys"
      :columns="resolvedColumns"
      :data="data"
      :loading="loading"
      :remote="!tree"
      :row-key="rowKeyGetter"
      :scroll-x="scrollX"
      :size="density"
      :children-key="childrenKey"
      :default-expand-all="tree && defaultExpandAll"
      striped
      @update:checked-row-keys="(keys) => emit('update:checkedKeys', keys as Array<string | number>)"
      @update:sorter="onSort"
    />
    <div class="xh-table-panel__footer">
      <!-- 底部左侧：数据量与页码提示（树形不分页，仅显示总数） -->
      <div class="xh-table__count">
        <template v-if="tree">
          共 <strong>{{ total }}</strong> 条
        </template>
        <template v-else>
          共 <strong>{{ total }}</strong> 条，第 <strong>{{ page }}</strong> / {{ pageCount }} 页
        </template>
      </div>
      <NPagination
        v-if="!tree"
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

<style scoped>
/* 占满父级（SchemaPage 的定高表格卡片）剩余高度，自身再分为表格 + 底部栏 */
.xh-table-panel {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-height: 0;
  height: 100%;
}

/* 表格占满中段；flex-height 使其内部纵向滚动而非撑高外层 */
.xh-table-panel__grid {
  flex: 1;
  min-height: 0;
}

/* 底部统计 + 分页：固定在底，不随表格滚动 */
.xh-table-panel__footer {
  display: flex;
  flex-shrink: 0;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
  padding-top: 12px;
}

.xh-table__count {
  font-size: 13px;
  color: var(--n-text-color);
  white-space: nowrap;
}

.xh-table__count strong {
  font-weight: 600;
  color: var(--n-text-color);
}
</style>

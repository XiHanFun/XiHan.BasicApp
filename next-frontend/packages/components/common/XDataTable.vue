<script lang="ts" setup generic="T extends object">
import type { TableColumnDef, TableRowDef, TableSelection } from '@xihan-ui/headless'
import type { VNodeChild } from 'vue'
import {
  XhTableBody,
  XhTableCell,
  XhTableColumnHeader,
  XhTableEmpty,
  XhTableHeader,
  XhTableLoadingState,
  XhTableRoot,
  XhTableRow,
  XhTableRowSelectTrigger,
  XhTableSelectAllTrigger,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { VNodeRender } from './VNodeRender'

/** 次级表格的列定义：抽屉、弹窗、面板里那些不走 Schema 的表格用它 */
export interface XDataTableColumn<Row> {
  key: string
  title?: string
  width?: number | string
  minWidth?: number
  align?: 'left' | 'center' | 'right'
  /** 贴边固定；多列固定时按声明顺序依次累加偏移 */
  fixed?: 'left' | 'right'
  ellipsis?: boolean
  render?: (row: Row, index: number) => VNodeChild
}

defineOptions({ name: 'XDataTable', inheritAttrs: false })

const props = withDefaults(defineProps<{
  columns: ReadonlyArray<XDataTableColumn<T>>
  data: ReadonlyArray<T>
  /** 行主键取值；给字符串即取该字段 */
  rowKey?: string | ((row: T) => string)
  loading?: boolean
  /** 勾选列；给了才出选择列 */
  selectable?: boolean
  size?: 'sm' | 'md' | 'lg'
  /** 横向滚动阈值（px）；列宽总和超过容器时用 */
  scrollX?: number
  /** 表头吸顶 */
  stickyHeader?: boolean
  emptyText?: string
  /** 逐行附加属性（如整行点击） */
  rowProps?: (row: T, index: number) => Record<string, unknown>
}>(), {
  rowKey: 'basicId',
  loading: false,
  selectable: false,
  size: 'sm',
  scrollX: undefined,
  stickyHeader: true,
  emptyText: undefined,
  rowProps: undefined,
})

const checkedKeys = defineModel<string[]>('checkedRowKeys', { default: () => [] })

const { t } = useI18n()

function keyOf(row: T): string {
  return typeof props.rowKey === 'function' ? props.rowKey(row) : String((row as Record<string, unknown>)[props.rowKey])
}

const SELECT_COL = '__select__'

const tableColumns = computed<TableColumnDef[]>(() => [
  ...(props.selectable ? [{ id: SELECT_COL, width: 40, ...(props.columns.some(c => c.fixed === 'left') ? { sticky: 'start' as const } : {}) }] : []),
  ...props.columns.map<TableColumnDef>(column => ({
    id: column.key,
    label: column.title,
    ...(column.fixed ? { sticky: column.fixed === 'right' ? 'end' : 'start' } : {}),
    ...(column.width === undefined ? {} : { width: column.width }),
  })),
])

const rows = computed(() => props.data.map(row => ({ key: keyOf(row), row })))
const tableRows = computed<TableRowDef[]>(() => rows.value.map(item => ({ id: item.key })))

function cellStyle(column: XDataTableColumn<T>) {
  return column.align && column.align !== 'left' ? { textAlign: column.align } : undefined
}

/** 全选时机器给的是 'all'，摊平成实际的键集合再回传 */
function onSelectionChange(selection: TableSelection) {
  checkedKeys.value = selection === 'all' ? rows.value.map(item => item.key) : selection
}

function cellContent(column: XDataTableColumn<T>, row: T, index: number): VNodeChild {
  return column.render ? column.render(row, index) : ((row as Record<string, unknown>)[column.key] as VNodeChild)
}
</script>

<template>
  <XhTableRoot
    v-bind="$attrs"
    class="x-data-table"
    :columns="tableColumns"
    :rows="tableRows"
    :selection="checkedKeys"
    :selection-mode="selectable ? 'multiple' : 'none'"
    :loading="loading"
    :empty="!loading && rows.length === 0"
    :size="size"
    :sticky-header="stickyHeader"
    :style="scrollX ? { '--xh-table-scroll-x': `${scrollX}px` } : undefined"
    @update:selection="onSelectionChange"
  >
    <XhTableHeader>
      <XhTableRow>
        <XhTableColumnHeader v-if="selectable" :value="SELECT_COL">
          <XhTableSelectAllTrigger>✓</XhTableSelectAllTrigger>
        </XhTableColumnHeader>
        <XhTableColumnHeader
          v-for="column in columns"
          :key="column.key"
          :value="column.key"
          :style="cellStyle(column)"
        >
          {{ column.title }}
        </XhTableColumnHeader>
      </XhTableRow>
    </XhTableHeader>

    <XhTableBody>
      <XhTableRow
        v-for="(item, rowIndex) in rows"
        :key="item.key"
        :value="item.key"
        v-bind="rowProps?.(item.row, rowIndex)"
      >
        <XhTableCell v-if="selectable" :value="SELECT_COL">
          <XhTableRowSelectTrigger>✓</XhTableRowSelectTrigger>
        </XhTableCell>
        <XhTableCell
          v-for="column in columns"
          :key="column.key"
          :value="column.key"
          :style="cellStyle(column)"
          :class="{ 'x-data-table__cell--ellipsis': column.ellipsis }"
        >
          <VNodeRender :content="cellContent(column, item.row, rowIndex)" />
        </XhTableCell>
      </XhTableRow>
    </XhTableBody>

    <XhTableLoadingState>
      <slot name="loading" />
    </XhTableLoadingState>
    <XhTableEmpty>
      <slot name="empty">
        {{ emptyText ?? t('common.empty') }}
      </slot>
    </XhTableEmpty>
  </XhTableRoot>
</template>

<style scoped>
.x-data-table__cell--ellipsis {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>

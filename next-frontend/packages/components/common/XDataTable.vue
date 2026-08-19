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
  /** 表格最大高度；不给则用皮肤缺省的 24rem */
  maxHeight?: number | string
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
  maxHeight: undefined,
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
/** 勾选列宽度。取皮肤给单元格的下限 3rem，声明值与渲染值一致，吸附偏移才累加得准 */
const SELECT_COL_W = 48

/**
 * 喂给组件库的确定列宽。组件库只认 width：没写 width 的列 flex-basis 退回该单元格自己的内容宽度，
 * 而表头行与每条数据行各是一个独立的 flex 容器，于是逐行各分各的宽、列边界对不齐。
 */
function declaredWidth(column: XDataTableColumn<T>): number | string {
  return column.width ?? column.minWidth ?? 120
}

const tableColumns = computed<TableColumnDef[]>(() => [
  ...(props.selectable ? [{ id: SELECT_COL, width: SELECT_COL_W, ...(props.columns.some(c => c.fixed === 'left') ? { sticky: 'start' as const } : {}) }] : []),
  ...props.columns.map<TableColumnDef>(column => ({
    id: column.key,
    label: column.title,
    ...(column.fixed ? { sticky: column.fixed === 'right' ? 'end' : 'start' } : {}),
    width: declaredWidth(column),
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
    :size="size"
    :sticky-header="stickyHeader"
    :style="maxHeight ? { '--xh-table-max-h': typeof maxHeight === 'number' ? `${maxHeight}px` : maxHeight } : undefined"
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
        >
          <!-- 截断要落在单元格内部的行内块上：单元格自身是 flex 容器，text-overflow 在它上面不生效 -->
          <span v-if="column.ellipsis" class="x-data-table__cell-text">
            <VNodeRender :content="cellContent(column, item.row, rowIndex)" />
          </span>
          <VNodeRender v-else :content="cellContent(column, item.row, rowIndex)" />
        </XhTableCell>
      </XhTableRow>
    </XhTableBody>

    <XhTableLoadingState>
      <slot name="loading">
        {{ t('common.loading') }}
      </slot>
    </XhTableLoadingState>
    <XhTableEmpty>
      <slot name="empty">
        {{ emptyText ?? t('common.empty') }}
      </slot>
    </XhTableEmpty>
  </XhTableRoot>
</template>

<style scoped>
.x-data-table__cell-text {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>

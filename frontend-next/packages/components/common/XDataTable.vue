<script lang="ts" setup generic="T extends object">
import type { TableColumnDef, TableRowDef, TableSelection } from '@xihan-ui/headless'
import type { VNodeChild } from 'vue'
import {
  XhTableBody,
  XhTableCell,
  XhTableColumnHeader,
  XhTableColumnLabel,
  XhTableEmpty,
  XhTableHeader,
  XhTableLoading,
  XhTableRoot,
  XhTableRow,
  XhTableRowSelectTrigger,
  XhTableSelectAllTrigger,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useIsMobile } from '~/composables'
import SchemaPagination from '../schema/SchemaPagination.vue'
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

/**
 * 次级表格的分页：给了就在表格下方出与列表页同一副底栏（左侧条数与页码、右侧分页）。
 * 不给 onUpdatePageSize 时不出条数选择器——换了条数却没人接，选了也不生效
 */
export interface XDataTablePagination {
  page: number
  pageSize: number
  itemCount: number
  /** 可选条数，缺省同列表页 */
  pageSizes?: number[]
  onUpdatePage?: (page: number) => void
  onUpdatePageSize?: (pageSize: number) => void
  /** 紧凑档：抽屉这类窄容器里用，少显几个页码；窄屏下自动开启 */
  compact?: boolean
}

defineOptions({ name: 'XDataTable' })

const props = withDefaults(defineProps<{
  columns: ReadonlyArray<XDataTableColumn<T>>
  data: ReadonlyArray<T>
  /** 行主键取值；给字符串即取该字段 */
  rowKey?: string | ((row: T) => string)
  loading?: boolean
  /** 勾选列；给了才出选择列 */
  selectable?: boolean
  /** 尺寸档，缺省与列表页表格同为 md */
  size?: 'sm' | 'md' | 'lg'
  /** 表格最大高度；不给则用皮肤缺省的 24rem */
  maxHeight?: number | string
  /** 表头吸顶 */
  stickyHeader?: boolean
  /** 列间分隔线，缺省开启：与列表页表格（SchemaTablePanel 缺省的非单行模式）同一副样式 */
  ruled?: boolean
  /** 分页底栏；不给则不出底栏 */
  pagination?: XDataTablePagination
  emptyText?: string
  /** 逐行附加属性（如整行点击） */
  rowProps?: (row: T, index: number) => Record<string, unknown>
}>(), {
  rowKey: 'basicId',
  loading: false,
  selectable: false,
  size: 'md',
  maxHeight: undefined,
  stickyHeader: true,
  ruled: true,
  pagination: undefined,
  emptyText: undefined,
  rowProps: undefined,
})

const checkedKeys = defineModel<string[]>('checkedRowKeys', { default: () => [] })

const { t } = useI18n()
const { isMobile } = useIsMobile()

const pageCount = computed(() =>
  props.pagination ? Math.max(1, Math.ceil(props.pagination.itemCount / props.pagination.pageSize)) : 1)
/** 调用方不接条数变更时只留当前条数一档，分页组件就不出条数选择器 */
const pageSizes = computed(() =>
  props.pagination?.onUpdatePageSize ? props.pagination.pageSizes : [props.pagination?.pageSize ?? 20])

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

/** 列对齐转成组件库的逻辑方向：它把对齐写在列头与数据格上，文字与格内的控件一起对齐 */
const COLUMN_ALIGN = { left: 'start', center: 'center', right: 'end' } as const

/**
 * width 只是 flex 基准，容器不够时各列按比例压缩；minWidth 交给组件库当这一列的下限，
 * 没写的列压到皮肤的 --xh-table-cell-min-w 为止。
 */
const tableColumns = computed<TableColumnDef[]>(() => [
  ...(props.selectable ? [{ id: SELECT_COL, width: SELECT_COL_W, ...(props.columns.some(c => c.fixed === 'left') ? { sticky: 'start' as const } : {}) }] : []),
  ...props.columns.map<TableColumnDef>(column => ({
    id: column.key,
    label: column.title,
    ...(column.fixed ? { sticky: column.fixed === 'right' ? 'end' : 'start' } : {}),
    width: declaredWidth(column),
    ...(column.minWidth !== undefined ? { minWidth: column.minWidth } : {}),
    ...(column.align ? { align: COLUMN_ALIGN[column.align] } : {}),
  })),
])

const rows = computed(() => props.data.map(row => ({ key: keyOf(row), row })))
const tableRows = computed<TableRowDef[]>(() => rows.value.map(item => ({ id: item.key })))

/** 全选时机器给的是 'all'，摊平成实际的键集合再回传 */
function onSelectionChange(selection: TableSelection) {
  checkedKeys.value = selection === 'all' ? rows.value.map(item => item.key) : selection
}

function cellContent(column: XDataTableColumn<T>, row: T, index: number): VNodeChild {
  return column.render ? column.render(row, index) : ((row as Record<string, unknown>)[column.key] as VNodeChild)
}
</script>

<template>
  <!-- 单根包裹层：XhTableRoot 渲染的是 Fragment（表格根 + aria 播报区），Vue 只把父组件的 scoped 属性打到
       单根子组件的根元素上，Fragment 根拿不到 data-v-*。没有这一层，调用方写在 class 上的 scoped 规则
       （flex:1、--xh-table-max-h…）与本组件的 :deep() 都选不中表格根，表格只能吃皮肤缺省的 24rem 上限。
       透传的 class / style 落在这层；--xh-table-max-h 是自定义属性，会继承进表格根 -->
  <div
    class="x-data-table"
    :style="maxHeight ? { '--xh-table-max-h': typeof maxHeight === 'number' ? `${maxHeight}px` : maxHeight } : undefined"
  >
    <XhTableRoot
      :columns="tableColumns"
      :rows="tableRows"
      :selection="checkedKeys"
      :selection-mode="selectable ? 'multiple' : 'none'"
      :loading="loading"
      :size="size"
      :sticky-header="stickyHeader"
      :ruled="ruled"
      @update:selection="onSelectionChange"
    >
      <XhTableHeader>
        <XhTableRow>
          <XhTableColumnHeader v-if="selectable" :value="SELECT_COL">
            <XhTableSelectAllTrigger />
          </XhTableColumnHeader>
          <XhTableColumnHeader
            v-for="column in columns"
            :key="column.key"
            :value="column.key"
          >
            <!-- 列名放进 column-label：列头是 flex 行，裸文本缩不下去；这一格超宽出省略号 -->
            <XhTableColumnLabel>{{ column.title }}</XhTableColumnLabel>
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
            <XhTableRowSelectTrigger />
          </XhTableCell>
          <XhTableCell
            v-for="column in columns"
            :key="column.key"
            :value="column.key"
          >
            <!-- 截断要落在单元格内部的行内块上：单元格自身是 flex 容器，text-overflow 在它上面不生效 -->
            <span v-if="column.ellipsis" class="x-data-table__cell-text">
              <VNodeRender :content="cellContent(column, item.row, rowIndex)" />
            </span>
            <VNodeRender v-else :content="cellContent(column, item.row, rowIndex)" />
          </XhTableCell>
        </XhTableRow>
      </XhTableBody>

      <XhTableLoading>
        <slot name="loading">
          {{ t('common.loading') }}
        </slot>
      </XhTableLoading>
      <XhTableEmpty>
        <slot name="empty">
          {{ emptyText ?? t('common.empty') }}
        </slot>
      </XhTableEmpty>
    </XhTableRoot>

    <!-- 底栏与列表页同一副：左侧条数与页码，右侧分页；窄屏换紧凑档 -->
    <div v-if="pagination" class="x-data-table__footer">
      <div class="x-data-table__count">
        {{ t('component.schema_table.total_prefix') }} <strong>{{ pagination.itemCount }}</strong> {{ t('component.schema_table.total_suffix') }}{{ t('component.schema_table.page_sep') }} <strong>{{ pagination.page }}</strong> {{ t('component.schema_table.page_of', { pageCount }) }}
      </div>
      <SchemaPagination
        class="x-data-table__pagination"
        :total="pagination.itemCount"
        :page="pagination.page"
        :page-size="pagination.pageSize"
        :page-sizes="pageSizes"
        :compact="isMobile || pagination.compact"
        @update:page="(value: number) => pagination?.onUpdatePage?.(value)"
        @update:page-size="(value: number) => pagination?.onUpdatePageSize?.(value)"
      />
    </div>
  </div>
</template>

<style scoped>
/* 包裹层是纵向 flex：表格根占满它的高度。调用方给包裹层 flex:1 / 定高时，表格就撑满该区域并在内部滚动；
   没给（普通块级语境）时包裹层随内容高，表格照旧受 --xh-table-max-h（缺省 24rem）封顶。
   表格撑满到多高仍由该令牌决定：要撑满容器就把它改成 100%（或经 max-height 传入） */
.x-data-table {
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.x-data-table :deep([data-scope='table'][data-part='root']) {
  flex: 1;
  min-block-size: 0;
}

/* 区段的下限从 max-content 换成 min-content：
   max-content 等于各列声明宽之和，容器再窄也不压缩、必出横向滚动；
   0 则让区段收到容器宽，而单元格压到各自下限后仍溢出行盒，行底色（斑马纹）就在中途断掉。
   min-content 正是各列下限之和：既允许按比例压缩，行盒又始终罩得住所有单元格 */
.x-data-table :deep([data-scope='table'][data-part='header']),
.x-data-table :deep([data-scope='table'][data-part='body']) {
  min-inline-size: min-content;
}

/* 底栏固定在表格下方，不随表体滚动；与列表页底栏同一副间距与字号 */
.x-data-table__footer {
  display: flex;
  flex-shrink: 0;
  flex-wrap: wrap;
  gap: var(--xh-space-2) var(--xh-space-3);
  align-items: center;
  justify-content: space-between;
  padding-top: var(--xh-space-2);
}

.x-data-table__count {
  font-size: var(--xh-text-secondary-size);
  color: var(--xh-fg-default);
  white-space: nowrap;
}

.x-data-table__count strong {
  font-weight: var(--xh-font-weight-semibold);
}

.x-data-table__pagination {
  max-width: 100%;
}

.x-data-table__cell-text {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>

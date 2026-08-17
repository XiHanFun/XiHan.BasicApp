<script setup lang="ts" generic="TRow extends object">
import type { TableColumnDef, TableRowDef, TableSelection, TableSortDescriptor } from '@xihan-ui/headless'
import type { VNodeChild } from 'vue'
import type { ListFieldSchema, SchemaColumn, SchemaSortRule } from './types'
import {
  XhTableBody,
  XhTableCell,
  XhTableColumnHeader,
  XhTableEmpty,
  XhTableExpandedRow,
  XhTableExpandTrigger,
  XhTableHeader,
  XhTableLoadingState,
  XhTableRoot,
  XhTableRow,
  XhTableRowSelectTrigger,
  XhTableSelectAllTrigger,
  XhTableSortTrigger,
} from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useIsMobile } from '~/composables'
import { VNodeRender } from '../common/VNodeRender'
import SchemaPagination from './SchemaPagination.vue'
import SchemaRowPeek from './SchemaRowPeek.vue'
import { useRowPeek } from './useRowPeek'

defineOptions({ name: 'SchemaTablePanel' })

const props = withDefaults(defineProps<{
  /** 列定义（由 selectors.toColumns 派生） */
  columns: SchemaColumn<TRow>[]
  /** 数据行；树形模式下是嵌套数组 */
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
  /** 是否显示序号列 */
  showIndex?: boolean
  /** 已选行主键 */
  checkedKeys?: Array<string | number>
  /** 当前多字段排序（受控回显各列箭头与优先级） */
  sorts?: ReadonlyArray<SchemaSortRule>
  /** 密度 */
  density?: 'sm' | 'md' | 'lg'
  /** 斑马纹（条纹） */
  striped?: boolean
  /** 边框 */
  bordered?: boolean
  /** 单线（关闭时显示纵向分隔线） */
  singleLine?: boolean
  /** 树形模式（启用后不分页、按 childrenKey 展开子行） */
  tree?: boolean
  /** 子节点字段名（默认 children） */
  childrenKey?: string
  /** 默认展开全部（默认 true） */
  defaultExpandAll?: boolean
  /** 重挂载令牌：变化即重建表格（用于清空展开态与拖拽列宽，如「恢复默认」） */
  remountKey?: number
  /** 悬停速览字段（提供即启用：悬停行 ~450ms 浮出全字段详情卡） */
  peekFields?: ListFieldSchema<TRow>[]
  /** 行展开渲染（提供即启用：行前出现展开箭头，展开后显示自定义内容） */
  renderExpand?: (row: TRow) => VNodeChild
  /** 行是否可展开（默认全部可展开） */
  rowExpandable?: (row: TRow) => boolean
}>(), {
  loading: false,
  rowKey: 'basicId',
  total: 0,
  page: 1,
  pageSize: 20,
  scrollX: undefined,
  pageSizes: () => [10, 20, 50, 100],
  selectable: false,
  showIndex: false,
  checkedKeys: () => [],
  sorts: () => [],
  density: 'sm',
  striped: true,
  bordered: true,
  singleLine: true,
  tree: false,
  childrenKey: 'children',
  defaultExpandAll: true,
  remountKey: 0,
  peekFields: undefined,
  renderExpand: undefined,
  rowExpandable: undefined,
})
const emit = defineEmits<{
  'update:page': [value: number]
  'update:pageSize': [value: number]
  'update:checkedKeys': [keys: Array<string | number>]
  'sort': [sorts: SchemaSortRule[]]
  'resizeColumn': [key: string, width: number]
}>()
/** 多选列、展开列、序号列的列 id：与数据列同处一个列号空间，故取不会撞车的名字 */
const SELECT_COL = '__select__'
const EXPAND_COL = '__expand__'
const INDEX_COL = '__index__'

const { t } = useI18n()
const { isMobile } = useIsMobile()

// ── 悬停速览（Peek & Pop）：移动端 / 未提供字段时禁用 ──────────────
const peek = useRowPeek<TRow>({
  enabled: () => !isMobile.value && (props.peekFields?.length ?? 0) > 0,
})
const peekEnabled = computed(() => (props.peekFields?.length ?? 0) > 0)

function rowKeyOf(row: TRow): string {
  return String((row as Record<string, unknown>)[props.rowKey])
}

function childrenOf(row: TRow): TRow[] | undefined {
  return (row as Record<string, unknown>)[props.childrenKey] as TRow[] | undefined
}

// ── 行铺平 ────────────────────────────────────────────────────────────
/** 铺平后的一行：树形模式带层级与展开态，列表模式层级恒为 0 */
interface FlatRow {
  key: string
  row: TRow
  level: number
  /** 大纲编号（树形）或全局序号（列表） */
  indexLabel: string
  /** 有子节点 */
  branch: boolean
}

/** 树形展开集合。收起的分支其子树整段不进序列，行号因此始终连续 */
const treeExpanded = ref<Set<string> | null>(null)

function isTreeOpen(key: string): boolean {
  // null 表示还没人动过：按 defaultExpandAll 决定
  return treeExpanded.value === null ? props.defaultExpandAll : treeExpanded.value.has(key)
}

function toggleTreeRow(key: string): void {
  if (treeExpanded.value === null) {
    // 首次交互：把当前的隐式展开态摊成显式集合，再翻这一行
    const seed = new Set<string>()
    if (props.defaultExpandAll) {
      const walk = (nodes: TRow[]) => {
        for (const node of nodes) {
          const children = childrenOf(node)
          if (children?.length) {
            seed.add(rowKeyOf(node))
            walk(children)
          }
        }
      }
      walk(props.data)
    }
    treeExpanded.value = seed
  }
  const set = treeExpanded.value
  if (set.has(key)) {
    set.delete(key)
  }
  else {
    set.add(key)
  }
  treeExpanded.value = new Set(set)
}

const flatRows = computed<FlatRow[]>(() => {
  const out: FlatRow[] = []
  if (!props.tree) {
    props.data.forEach((row, i) => {
      out.push({
        key: rowKeyOf(row),
        row,
        level: 0,
        indexLabel: String((props.page - 1) * props.pageSize + i + 1),
        branch: false,
      })
    })
    return out
  }
  const walk = (nodes: TRow[], level: number, prefix: string) => {
    nodes.forEach((node, i) => {
      const key = rowKeyOf(node)
      const label = prefix ? `${prefix}.${i + 1}` : `${i + 1}`
      const children = childrenOf(node)
      const branch = !!children?.length
      out.push({ key, row: node, level, indexLabel: label, branch })
      if (branch && isTreeOpen(key)) {
        walk(children!, level + 1, label)
      }
    })
  }
  walk(props.data, 0, '')
  return out
})

// ── 列 ────────────────────────────────────────────────────────────────
/** 列首前缀：展开列、多选列、序号列（按需依次插入到数据列之前） */
const prefixColumns = computed<Array<{ id: string, width: number }>>(() => {
  const list: Array<{ id: string, width: number }> = []
  if (props.renderExpand) {
    list.push({ id: EXPAND_COL, width: 40 })
  }
  if (props.selectable) {
    list.push({ id: SELECT_COL, width: 40 })
  }
  if (props.showIndex) {
    list.push({ id: INDEX_COL, width: props.tree ? 90 : 60 })
  }
  return list
})

/** 拖拽产生的列宽覆盖：只活在本次挂载内，落库由上层的列设置负责 */
const draggedWidths = ref<Record<string, number>>({})

function widthOf(column: SchemaColumn<TRow>): number | undefined {
  return draggedWidths.value[column.key] ?? column.width
}

/**
 * 喂给组件库的列契约：列号、列宽、可排序与吸附的唯一事实源。
 * 前缀列也必须在此声明，否则右侧所有列的 aria-colindex 串位。
 */
const tableColumns = computed<TableColumnDef[]>(() => [
  ...prefixColumns.value.map(c => ({ id: c.id, width: c.width })),
  ...props.columns.map<TableColumnDef>(column => ({
    id: column.key,
    label: column.title,
    ...(column.sortable ? { sortable: true } : {}),
    ...(column.fixed ? { sticky: true } : {}),
    ...(widthOf(column) === undefined ? {} : { width: widthOf(column) }),
  })),
])

const tableRows = computed<TableRowDef[]>(() => flatRows.value.map(r => ({
  id: r.key,
  ...(props.renderExpand && (props.rowExpandable?.(r.row) ?? true) ? { expandable: true } : {}),
})))

// ── 吸附列偏移 ────────────────────────────────────────────────────────
/**
 * 皮肤只给吸附列写 position: sticky，贴到哪儿要由列宽累加算出来。
 * 左吸附从左缘往右累加，右吸附从右缘往左累加；宽度未知的列不参与（算不出偏移）。
 */
const stickyOffsets = computed<Record<string, { left?: number, right?: number }>>(() => {
  const out: Record<string, { left?: number, right?: number }> = {}
  let left = prefixColumns.value.reduce((sum, c) => sum + c.width, 0)
  for (const column of props.columns) {
    if (column.fixed === 'left') {
      out[column.key] = { left }
      left += widthOf(column) ?? 0
    }
  }
  let right = 0
  for (let i = props.columns.length - 1; i >= 0; i--) {
    const column = props.columns[i]!
    if (column.fixed === 'right') {
      out[column.key] = { right }
      right += widthOf(column) ?? 0
    }
  }
  return out
})

function stickyStyle(key: string): Record<string, string> | undefined {
  const offset = stickyOffsets.value[key]
  if (!offset) {
    return undefined
  }
  return offset.left === undefined
    ? { insetInlineEnd: `${offset.right}px` }
    : { insetInlineStart: `${offset.left}px` }
}

// ── 排序 ──────────────────────────────────────────────────────────────
/** 应用侧的 { field, order } 与组件库的 { id, direction } 互转；数组顺序即优先级 */
const sortChain = computed<TableSortDescriptor[]>(() =>
  props.sorts.map(rule => ({ id: rule.field, direction: rule.order === 'asc' ? 'asc' : 'desc' })),
)

function onSortChange(chain: TableSortDescriptor[]): void {
  emit('sort', chain.map(item => ({ field: item.id, order: item.direction === 'asc' ? 'asc' : 'desc' })))
}

// ── 多选 ──────────────────────────────────────────────────────────────
const selection = computed<TableSelection>(() => props.checkedKeys.map(String))

function onSelectionChange(next: TableSelection): void {
  // 裸 'all' 摊成当前页的显式键：上层按键数组做批量操作
  const keys = next === 'all' ? flatRows.value.map(r => r.key) : next
  emit('update:checkedKeys', keys)
}

// ── 列宽拖拽 ──────────────────────────────────────────────────────────
let grabbed: { key: string, startX: number, startWidth: number } | null = null

function onResizeGrab(event: PointerEvent, column: SchemaColumn<TRow>): void {
  const el = event.currentTarget as HTMLElement
  // 未显式给宽的列以当前实测宽度起量，拖拽不会跳一下
  const startWidth = widthOf(column) ?? el.parentElement?.getBoundingClientRect().width ?? 120
  grabbed = { key: column.key, startX: event.clientX, startWidth }
  el.setPointerCapture(event.pointerId)
  event.preventDefault()
}

function onResizeDrag(event: PointerEvent): void {
  if (!grabbed) {
    return
  }
  const column = props.columns.find(c => c.key === grabbed!.key)
  const min = column?.minWidth ?? 80
  draggedWidths.value = {
    ...draggedWidths.value,
    [grabbed.key]: Math.max(min, Math.round(grabbed.startWidth + event.clientX - grabbed.startX)),
  }
}

function onResizeRelease(): void {
  if (grabbed) {
    const width = draggedWidths.value[grabbed.key]
    if (width !== undefined) {
      emit('resizeColumn', grabbed.key, width)
    }
  }
  grabbed = null
}

// 悬停速览的行事件：未启用时不挂，省掉每行三个监听
function rowPeekHandlers(row: TRow) {
  return peekEnabled.value ? peek.rowProps(row) : {}
}
</script>

<template>
  <!-- 定高 flex 列：表格占满中段并在内部滚动，分页栏固定底部，整体不撑破父容器 -->
  <div class="xh-table-panel">
    <XhTableRoot
      :key="remountKey"
      class="xh-table-panel__grid"
      :columns="tableColumns"
      :rows="tableRows"
      :sort="sortChain"
      :selection="selection"
      :selection-mode="selectable ? 'multiple' : 'none'"
      :loading="loading"
      :empty="!loading && flatRows.length === 0"
      :size="density"
      sticky-header
      :class="{
        'xh-table-panel__grid--striped': striped,
        'xh-table-panel__grid--borderless': !bordered,
        'xh-table-panel__grid--ruled': !singleLine,
      }"
      :style="scrollX ? { '--xh-table-scroll-x': `${scrollX}px` } : undefined"
      @update:sort="onSortChange"
      @update:selection="onSelectionChange"
    >
      <XhTableHeader>
        <XhTableRow>
          <XhTableColumnHeader v-if="renderExpand" :value="EXPAND_COL" />
          <XhTableColumnHeader v-if="selectable" :value="SELECT_COL">
            <XhTableSelectAllTrigger>✓</XhTableSelectAllTrigger>
          </XhTableColumnHeader>
          <XhTableColumnHeader v-if="showIndex" :value="INDEX_COL">
            {{ t('component.schema_table.index') }}
          </XhTableColumnHeader>
          <XhTableColumnHeader
            v-for="column in columns"
            :key="column.key"
            :value="column.key"
            :style="stickyStyle(column.key)"
          >
            <XhTableSortTrigger v-if="column.sortable" class="xh-table-panel__title">
              {{ column.title }}
            </XhTableSortTrigger>
            <span v-else class="xh-table-panel__title">{{ column.title }}</span>
            <!-- 调宽把手与排序把手是兄弟节点，拖它不会连带触发排序 -->
            <span
              aria-hidden="true"
              class="xh-table-panel__resize"
              @pointerdown="onResizeGrab($event, column)"
              @pointermove="onResizeDrag"
              @pointerup="onResizeRelease"
              @pointercancel="onResizeRelease"
            />
          </XhTableColumnHeader>
        </XhTableRow>
      </XhTableHeader>

      <XhTableBody>
        <template v-for="(item, rowIndex) in flatRows" :key="item.key">
          <XhTableRow :value="item.key" v-bind="rowPeekHandlers(item.row)">
            <XhTableCell v-if="renderExpand" :value="EXPAND_COL">
              <XhTableExpandTrigger>›</XhTableExpandTrigger>
            </XhTableCell>
            <XhTableCell v-if="selectable" :value="SELECT_COL">
              <XhTableRowSelectTrigger>✓</XhTableRowSelectTrigger>
            </XhTableCell>
            <XhTableCell v-if="showIndex" :value="INDEX_COL">
              {{ item.indexLabel }}
            </XhTableCell>
            <XhTableCell
              v-for="column in columns"
              :key="column.key"
              :value="column.key"
              :style="stickyStyle(column.key)"
            >
              <!-- 树形列：缩进 + 展开箭头，其余列照常渲染 -->
              <template v-if="tree && column.tree">
                <span class="xh-table-panel__indent" :style="{ inlineSize: `${item.level * 16}px` }" />
                <button
                  v-if="item.branch"
                  type="button"
                  class="xh-table-panel__tree-toggle"
                  :aria-expanded="isTreeOpen(item.key)"
                  @click="toggleTreeRow(item.key)"
                >
                  {{ isTreeOpen(item.key) ? '▾' : '▸' }}
                </button>
                <span v-else class="xh-table-panel__tree-toggle xh-table-panel__tree-toggle--leaf" />
              </template>
              <VNodeRender :content="column.render(item.row, rowIndex)" />
            </XhTableCell>
          </XhTableRow>

          <XhTableExpandedRow v-if="renderExpand" :value="item.key">
            <VNodeRender :content="renderExpand(item.row)" />
          </XhTableExpandedRow>
        </template>
      </XhTableBody>

      <XhTableEmpty>{{ t('component.schema_table.empty') }}</XhTableEmpty>
      <XhTableLoadingState>{{ t('component.schema_table.loading') }}</XhTableLoadingState>
    </XhTableRoot>

    <!-- 悬停速览卡（Teleport 到 body，pointer-events none 不干扰交互） -->
    <SchemaRowPeek
      :visible="peek.visible.value"
      :row="peek.row.value"
      :fields="peekFields ?? []"
      :x="peek.x.value"
      :y="peek.y.value"
    />

    <div class="xh-table-panel__footer">
      <!-- 底部左侧：数据量/页码提示 + 批量操作浮条（选中后在此展示，避免挤压表格） -->
      <div class="xh-table-panel__footer-left">
        <div class="xh-table__count">
          <template v-if="tree">
            {{ t('component.schema_table.total_prefix') }} <strong>{{ total }}</strong> {{ t('component.schema_table.total_suffix') }}
          </template>
          <template v-else>
            {{ t('component.schema_table.total_prefix') }} <strong>{{ total }}</strong> {{ t('component.schema_table.total_suffix') }}{{ t('component.schema_table.page_sep') }} <strong>{{ page }}</strong> {{ t('component.schema_table.page_of', { pageCount: Math.max(1, Math.ceil(total / pageSize)) }) }}
          </template>
        </div>
        <slot name="footer-actions" />
      </div>
      <SchemaPagination
        v-if="!tree"
        class="xh-table-panel__pagination"
        :total="total"
        :page="page"
        :page-size="pageSize"
        :page-sizes="pageSizes"
        :compact="isMobile"
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

/* 表格占满中段，内部纵向滚动而非撑高外层。
 * 皮肤给表格的缺省高度上限是 24rem，管理页要的是「填满卡片剩余高度」，故改写这个槽位；
 * 溢出滚动皮肤已经给了，这里不重复声明。 */
.xh-table-panel__grid {
  flex: 1;
  min-height: 0;
  --xh-table-max-h: 100%;
}

/* 横向滚动宽度：撑在三个区段上，列窄时也保持一条横向滚动轴 */
.xh-table-panel__grid :deep([data-scope='table'][data-part='header']),
.xh-table-panel__grid :deep([data-scope='table'][data-part='body']) {
  min-inline-size: max(max-content, var(--xh-table-scroll-x, 0px));
}

/* 斑马纹：偶数数据行加一层浅底。展开出来的详情行不算进条纹节奏 */
.xh-table-panel__grid--striped :deep([data-scope='table'][data-part='row']:nth-of-type(even)) {
  background: var(--xh-bg-subtle);
}

/* 无边框档：去掉外框，只留行间横线 */
.xh-table-panel__grid--borderless {
  border: 0;
}

/* 非单线档：补上纵向分隔线 */
.xh-table-panel__grid--ruled :deep([data-scope='table'][data-part='cell']),
.xh-table-panel__grid--ruled :deep([data-scope='table'][data-part='column-header']) {
  border-inline-end: 1px solid var(--xh-border-subtle);
}

.xh-table-panel__grid--ruled :deep([data-scope='table'][data-part='cell']:last-child),
.xh-table-panel__grid--ruled :deep([data-scope='table'][data-part='column-header']:last-child) {
  border-inline-end: 0;
}

/* 吸附列要盖住滚过去的普通列：普通单元格 background: inherit，吸附列须自带不透明底 */
.xh-table-panel__grid :deep([data-scope='table'][data-part='cell'][data-sticky]),
.xh-table-panel__grid :deep([data-scope='table'][data-part='column-header'][data-sticky]) {
  background: var(--xh-bg-surface);
}

/* 列标题里的文字段：占满剩余宽度并省略，把手才贴得住右缘 */
.xh-table-panel__title {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 调宽把手：贴列标题右缘的一条窄带，悬停才显形 */
.xh-table-panel__resize {
  flex: none;
  align-self: stretch;
  inline-size: 5px;
  margin-inline-end: -4px;
  cursor: col-resize;
  touch-action: none;
  background: transparent;
  transition: background-color 0.15s ease;
}

.xh-table-panel__resize:hover {
  background: var(--xh-border-strong);
}

/* 树形缩进与展开箭头 */
.xh-table-panel__indent {
  flex: none;
}

.xh-table-panel__tree-toggle {
  flex: none;
  inline-size: 16px;
  block-size: 16px;
  margin-inline-end: 4px;
  border: 0;
  background: transparent;
  color: var(--xh-fg-muted);
  cursor: pointer;
  line-height: 1;
}

.xh-table-panel__tree-toggle--leaf {
  cursor: default;
}

/* 底部统计 + 分页：固定在底，不随表格滚动 */
.xh-table-panel__footer {
  display: flex;
  flex-shrink: 0;
  flex-wrap: wrap;
  gap: 8px 12px;
  align-items: center;
  justify-content: space-between;
  padding-top: 12px;
}

/* 左侧：统计 + 批量浮条 */
.xh-table-panel__footer-left {
  display: flex;
  gap: 12px;
  align-items: center;
  min-width: 0;
}

/* 分页：限制不超过页脚宽度；窄屏/页数极多时内部横向滚动，避免撑破页面 */
.xh-table-panel__pagination {
  max-width: 100%;
  overflow-x: auto;
}

.xh-table__count {
  font-size: 13px;
  color: var(--xh-fg-default);
  white-space: nowrap;
}

.xh-table__count strong {
  font-weight: 600;
}
</style>

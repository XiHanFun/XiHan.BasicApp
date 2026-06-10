<script setup lang="ts" generic="TRow extends object">
import type { DataTableBaseColumn, DataTableColumn, DataTableSortState } from 'naive-ui'
import type { ListFieldSchema } from './types'
import { NDataTable, NPagination } from 'naive-ui'
import { computed } from 'vue'
import { useIsMobile } from '~/composables'
import SchemaRowPeek from './SchemaRowPeek.vue'
import { toSortParams } from './selectors'
import { useRowPeek } from './useRowPeek'

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
  /** 是否显示序号列 */
  showIndex?: boolean
  /** 已选行主键 */
  checkedKeys?: Array<string | number>
  /** 密度（映射 NDataTable size） */
  density?: 'small' | 'medium' | 'large'
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
  /** 重挂载令牌：变化即重建表格（用于清空 Naive 内部列宽拖拽缓存，如「恢复默认」） */
  remountKey?: number
  /** 悬停速览字段（提供即启用：悬停行 ~450ms 浮出全字段详情卡） */
  peekFields?: ListFieldSchema<TRow>[]
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
  density: 'small',
  striped: true,
  bordered: true,
  singleLine: true,
  tree: false,
  childrenKey: 'children',
  defaultExpandAll: true,
  remountKey: 0,
  peekFields: undefined,
})

const emit = defineEmits<{
  'update:page': [value: number]
  'update:pageSize': [value: number]
  'update:checkedKeys': [keys: Array<string | number>]
  'sort': [field: string | undefined, order: 'asc' | 'desc' | undefined]
  'resizeColumn': [key: string, width: number]
}>()

const { isMobile } = useIsMobile()

const pageCount = computed(() => Math.max(1, Math.ceil(props.total / props.pageSize)))

// ── 悬停速览（Peek & Pop）：移动端 / 未提供字段时禁用 ──────────────
const peek = useRowPeek<TRow>({
  enabled: () => !isMobile.value && (props.peekFields?.length ?? 0) > 0,
})
const peekRowProps = computed(() =>
  (props.peekFields?.length ?? 0) > 0 ? (row: TRow) => peek.rowProps(row) : undefined,
)

function rowKeyGetter(row: TRow) {
  return (row as Record<string, unknown>)[props.rowKey] as string | number
}

/**
 * 树形序号映射：按层级生成大纲式编号（key → 如 "1" / "1.1" / "1.2.3"）。
 * Naive 在树形模式下 render 的 rowIndex 是「根级祖先下标」而非展开后的位置，
 * 故需自行按层级路径编号。
 */
const treeIndexMap = computed(() => {
  const map = new Map<string | number, string>()
  if (!props.tree) {
    return map
  }
  const walk = (nodes: TRow[], prefix: string) => {
    nodes.forEach((node, i) => {
      const label = prefix ? `${prefix}.${i + 1}` : `${i + 1}`
      map.set(rowKeyGetter(node), label)
      const children = (node as Record<string, unknown>)[props.childrenKey] as TRow[] | undefined
      if (children?.length) {
        walk(children, label)
      }
    })
  }
  walk(props.data, '')
  return map
})

/** 序号：列表模式按全局位置（含翻页），树形模式按层级大纲编号（1 / 1.1 / 1.2） */
function indexLabel(row: TRow, rowIndex: number): string | number {
  if (props.tree) {
    return treeIndexMap.value.get(rowKeyGetter(row)) ?? rowIndex + 1
  }
  return (props.page - 1) * props.pageSize + rowIndex + 1
}

/** 列首前缀：多选列、序号列（按需依次插入到数据列之前） */
const resolvedColumns = computed<DataTableColumn<TRow>[]>(() => {
  const prefix: DataTableColumn<TRow>[] = []
  if (props.selectable) {
    prefix.push({ type: 'selection' } as unknown as DataTableColumn<TRow>)
  }
  if (props.showIndex) {
    prefix.push({
      key: '__index__',
      title: '序号',
      width: props.tree ? 90 : 60,
      align: props.tree ? 'left' : 'center',
      render: (row: TRow, rowIndex: number) => indexLabel(row, rowIndex),
    } as unknown as DataTableColumn<TRow>)
  }
  if (prefix.length === 0) {
    return props.columns
  }
  return [...prefix, ...props.columns]
})

function onSort(sorter: DataTableSortState | DataTableSortState[] | null) {
  const single = Array.isArray(sorter) ? sorter[0] ?? null : sorter
  const { sortField, sortOrder } = toSortParams(single)
  emit('sort', sortField, sortOrder)
}

/** 列宽拖拽：Naive 在拖动中持续回调，取 clamp 后的宽度回写列设置（即时生效、待「保存」落库） */
function onColumnResize(_resizedWidth: number, limitedWidth: number, column: DataTableBaseColumn) {
  emit('resizeColumn', String(column.key), Math.round(limitedWidth))
}
</script>

<template>
  <!-- 定高 flex 列：表格占满中段并在内部滚动，分页栏固定底部，整体不撑破父容器 -->
  <div class="xh-table-panel">
    <NDataTable
      :key="remountKey"
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
      :striped="striped"
      :bordered="bordered"
      :single-line="singleLine"
      :row-props="peekRowProps"
      :on-unstable-column-resize="onColumnResize"
      @update:checked-row-keys="(keys) => emit('update:checkedKeys', keys as Array<string | number>)"
      @update:sorter="onSort"
    />
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
            共 <strong>{{ total }}</strong> 条
          </template>
          <template v-else>
            共 <strong>{{ total }}</strong> 条，第 <strong>{{ page }}</strong> / {{ pageCount }} 页
          </template>
        </div>
        <slot name="footer-actions" />
      </div>
      <NPagination
        v-if="!tree"
        class="xh-table-panel__pagination"
        :item-count="total"
        :page="page"
        :page-size="pageSize"
        :page-sizes="pageSizes"
        :page-slot="isMobile ? 5 : 9"
        :size="isMobile ? 'small' : 'medium'"
        :show-size-picker="!isMobile"
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
  color: var(--n-text-color);
  white-space: nowrap;
}

.xh-table__count strong {
  font-weight: 600;
  color: var(--n-text-color);
}
</style>

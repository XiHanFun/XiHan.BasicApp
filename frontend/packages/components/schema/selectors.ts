import type { DataTableColumn } from 'naive-ui'
import type { ListFieldSchema, PageSchema } from './types'
import { renderFieldCell } from './renderer'

/**
 * 字段是否对当前用户可见（权限过滤）。
 * @param field 字段
 * @param can 权限判定函数（来自 usePermission().hasPermission）
 */
function isFieldPermitted(field: { permission?: string }, can: (code: string) => boolean): boolean {
  return !field.permission || can(field.permission)
}

/**
 * 按 order 升序稳定排序。
 */
function byOrder<T extends { order?: number }>(list: T[]): T[] {
  return [...list].sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
}

/**
 * 派生：常用搜索字段（searchable=true 且有权限）。
 */
export function toSearchFields<TRow extends object>(
  schema: PageSchema<TRow>,
  can: (code: string) => boolean,
): ListFieldSchema<TRow>[] {
  return byOrder(schema.fields.filter(f => f.searchable && isFieldPermitted(f, can)))
}

/**
 * 派生：高级搜索字段（advancedSearch=true 且有权限）。
 */
export function toAdvancedFields<TRow extends object>(
  schema: PageSchema<TRow>,
  can: (code: string) => boolean,
): ListFieldSchema<TRow>[] {
  return byOrder(schema.fields.filter(f => f.advancedSearch && isFieldPermitted(f, can)))
}

/**
 * 派生：导出字段（exportable=true 且有权限）。
 */
export function toExportFields<TRow extends object>(
  schema: PageSchema<TRow>,
  can: (code: string) => boolean,
): ListFieldSchema<TRow>[] {
  return byOrder(schema.fields.filter(f => f.exportable && isFieldPermitted(f, can)))
}

/**
 * 派生：表格列（visible=true 且有权限），可叠加视图/列设置的列可见、顺序与固定。
 * @param options.visibleKeys 覆盖的可见列；未提供则用 schema 默认
 * @param options.columnOrder 覆盖的列顺序
 * @param options.fixedMap 覆盖的列固定方向（key → left/right/undefined）
 */
export function toColumns<TRow extends object>(
  schema: PageSchema<TRow>,
  can: (code: string) => boolean,
  options?: {
    visibleKeys?: string[]
    columnOrder?: string[]
    fixedMap?: Record<string, 'left' | 'right' | undefined>
  },
): DataTableColumn<TRow>[] {
  let fields = schema.fields.filter(f => f.visible !== false && isFieldPermitted(f, can))

  if (options?.visibleKeys) {
    const allow = new Set(options.visibleKeys)
    fields = fields.filter(f => allow.has(f.key))
  }

  fields = byOrder(fields)

  if (options?.columnOrder) {
    const indexOf = (key: string) => {
      const i = options.columnOrder!.indexOf(key)
      return i === -1 ? Number.MAX_SAFE_INTEGER : i
    }
    fields = [...fields].sort((a, b) => indexOf(a.key) - indexOf(b.key))
  }

  // 按需写入可选属性，规避 exactOptionalPropertyTypes 下显式 undefined 报错
  return fields.map<DataTableColumn<TRow>>((field) => {
    const column: Record<string, unknown> = {
      key: field.key,
      title: field.title,
      ellipsis: { tooltip: true },
      render: (row: TRow) => renderFieldCell(field, row),
    }
    if (field.width !== undefined) {
      column.width = field.width
    }
    if (field.minWidth !== undefined) {
      column.minWidth = field.minWidth
    }
    // 固定方向：列设置/视图覆盖优先（含「取消固定」），否则用字段默认
    const overriddenFixed = options?.fixedMap && field.key in options.fixedMap
      ? options.fixedMap[field.key]
      : field.fixed
    if (overriddenFixed !== undefined) {
      column.fixed = overriddenFixed
      // Naive UI 固定列必须有确定 width；仅声明 minWidth/无宽度的列回退一个宽度，否则固定会错位失效
      if (field.width === undefined) {
        column.width = field.minWidth ?? 120
      }
    }
    if (field.sortable) {
      // 服务端排序：仅声明可排序，排序事件由表格上抛
      column.sorter = false
    }
    return column as unknown as DataTableColumn<TRow>
  })
}

/**
 * 派生：将 Naive UI 排序状态转为归一化 sortField/sortOrder。
 */
export function toSortParams(
  sorter: { columnKey?: string | number, order?: 'ascend' | 'descend' | false } | null,
): { sortField?: string, sortOrder?: 'asc' | 'desc' } {
  if (!sorter || !sorter.order || sorter.columnKey == null) {
    return {}
  }
  return {
    sortField: String(sorter.columnKey),
    sortOrder: sorter.order === 'ascend' ? 'asc' : 'desc',
  }
}

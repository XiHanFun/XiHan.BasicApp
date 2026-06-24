import type { DataTableColumn } from 'naive-ui'
import type { ListFieldSchema, PageSchema, SchemaSortRule } from './types'
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
 * 派生：导入字段（importable=true 且有权限）—— 导入模板列与解析校验的事实源。
 */
export function toImportFields<TRow extends object>(
  schema: PageSchema<TRow>,
  can: (code: string) => boolean,
): ListFieldSchema<TRow>[] {
  return byOrder(schema.fields.filter(f => f.importable && isFieldPermitted(f, can)))
}

/**
 * 派生：表格列（visible=true 且有权限），可叠加视图/列设置的列可见、顺序与固定。
 * @param options.visibleKeys 覆盖的可见列；未提供则用 schema 默认
 * @param options.columnOrder 覆盖的列顺序
 * @param options.fixedMap 覆盖的列固定方向（key → left/right/undefined）
 * @param options.widthMap 覆盖的列宽（key → px；undefined 表示沿用 schema 宽度）
 */
export function toColumns<TRow extends object>(
  schema: PageSchema<TRow>,
  can: (code: string) => boolean,
  options?: {
    visibleKeys?: string[]
    columnOrder?: string[]
    fixedMap?: Record<string, 'left' | 'right' | undefined>
    widthMap?: Record<string, number | undefined>
    /** 当前多字段排序（用于受控回显各列排序箭头与优先级） */
    sorts?: ReadonlyArray<SchemaSortRule>
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
    // 树形列：承载展开箭头（仅当 schema.tree 启用、页面 schema 标记 treeColumn）
    if (field.treeColumn) {
      column.tree = true
    }
    // 列宽：列设置覆盖优先，否则用字段默认 width
    const overriddenWidth = options?.widthMap?.[field.key]
    if (overriddenWidth !== undefined) {
      column.width = overriddenWidth
    }
    else if (field.width !== undefined) {
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
      if (column.width === undefined) {
        column.width = field.minWidth ?? 120
      }
    }
    if (field.sortable) {
      // 多字段服务端排序：{ multiple } 让点多个列头累加排序（remote 模式 Naive 不本地排序，仅上抛 update:sorter 数组）。
      // 受控 sortOrder 让各列箭头反映当前排序态（含「表格设置」的默认多字段排序、方案恢复的排序）；优先级由 sorts 顺序决定。
      column.sorter = { multiple: 1 }
      const rule = options?.sorts?.find(s => s.field === field.key)
      column.sortOrder = rule ? (rule.order === 'asc' ? 'ascend' : 'descend') : false
    }
    // 列宽可拖拽调整（拖动表头右边框）；缺省 minWidth 给一个下限，避免拖到过窄
    column.resizable = true
    if (column.minWidth === undefined) {
      column.minWidth = 80
    }
    return column as unknown as DataTableColumn<TRow>
  })
}

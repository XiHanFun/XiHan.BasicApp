import type { DataTableColumn } from 'naive-ui'
import type { VNodeChild } from 'vue'
import type { ApiId, PageResult } from '~/types/contracts'

/**
 * 下拉/标签选项（与 business 常量、Naive 选项结构兼容）
 */
export interface SchemaSelectOption<TValue extends string | number = string | number> {
  label: string
  value: TValue
}

/**
 * 字段数据类型（决定渲染器与默认搜索控件）
 */
export type SchemaFieldDataType
  = | 'string'
    | 'text'
    | 'number'
    | 'boolean'
    | 'enum'
    | 'date'
    | 'datetime'
    | 'money'
    | 'percent'
    | 'tag'
    | 'json'
    | 'image'
    | 'avatar'
    | 'email'
    | 'phone'
    | 'url'

/**
 * 列表字段 Schema —— 页面字段的单一事实源。
 * 搜索表单、表格列、导出字段、导入模板、详情展示等均由此派生，禁止重复定义。
 */
export interface ListFieldSchema<TRow = Record<string, unknown>> {
  /** 字段键（对应数据行与查询参数的属性名） */
  key: string
  /** 列标题（建议为 i18n key） */
  title: string
  /** 数据类型 */
  dataType: SchemaFieldDataType
  /** 是否在表格中可见 */
  visible?: boolean
  /** 是否参与常用搜索 */
  searchable?: boolean
  /** 是否参与高级搜索 */
  advancedSearch?: boolean
  /** 是否可排序（服务端排序） */
  sortable?: boolean
  /** 是否可作为筛选条件 */
  filterable?: boolean
  /** 是否可导出 */
  exportable?: boolean
  /** 是否可导入 */
  importable?: boolean
  /** 是否可在表单中编辑 */
  editable?: boolean
  /** 是否只读 */
  readonly?: boolean
  /** 是否必填 */
  required?: boolean
  /** 字段级权限码；当前用户无此权限时该字段隐藏 */
  permission?: string
  /** 字典码（enum/tag 异步取值，S2 接入；S1 优先使用 options） */
  dictionaryCode?: string
  /** 即时下拉/标签选项（优先于 dictionaryCode；常引用 business 常量） */
  options?: ReadonlyArray<SchemaSelectOption>
  /** 自定义格式化器标识（由 useFieldFormat 解析，如 maskPhone/maskEmail） */
  formatter?: string
  /** 列宽 */
  width?: number
  /** 最小列宽 */
  minWidth?: number
  /** 固定列 */
  fixed?: 'left' | 'right'
  /** 排序值（越小越靠前） */
  order?: number
  /** 默认值 */
  defaultValue?: unknown
  /** 搜索控件占位提示 */
  searchPlaceholder?: string
  /** 自定义单元格渲染（最高优先级，覆盖内置渲染器） */
  render?: (row: TRow) => VNodeChild
  /** 树形列：该列承载展开/缩进箭头（仅树形模式生效，应有且仅有一个字段标记） */
  treeColumn?: boolean
}

/**
 * 操作作用域：页面级（工具栏）/ 行级（更多菜单）/ 批量级（批量浮条）
 */
export type SchemaActionScope = 'page' | 'row' | 'batch'

/**
 * 操作 Schema
 */
export interface ActionSchema<TRow = Record<string, unknown>> {
  /** 操作唯一码 */
  key: string
  /** 操作文案（i18n key） */
  title: string
  /** 作用域 */
  scope: SchemaActionScope
  /** 按钮类型 */
  type?: 'default' | 'primary' | 'info' | 'success' | 'warning' | 'error'
  /** 图标（iconify） */
  icon?: string
  /** 所需权限码；无权限时不渲染 */
  permission?: string
  /** 是否需要二次确认 */
  confirm?: boolean
  /** 确认提示文案（i18n key） */
  confirmText?: string
  /** 行级操作可见性判定（如内置数据不可删） */
  visible?: (row: TRow) => boolean
  /** 行级操作禁用判定 */
  disabled?: (row: TRow) => boolean
}

/**
 * 操作事件载荷（页面级/行级/批量级统一上抛）
 */
export interface SchemaActionPayload<TRow = Record<string, unknown>> {
  /** 操作码 */
  key: string
  /** 作用域 */
  scope: SchemaActionScope
  /** 行级操作携带的行数据 */
  row?: TRow
  /** 批量操作携带的选中行 */
  rows?: TRow[]
}

/**
 * 视图 Schema —— 预置视图（系统视图）。租户/团队/个人视图为运行时数据。
 */
export interface ViewSchema {
  /** 视图码 */
  code: string
  /** 视图名（i18n key） */
  name: string
  /** 作用域 */
  scope: 'system' | 'tenant' | 'team' | 'personal'
  /** 列可见键 */
  visibleKeys?: string[]
  /** 列顺序 */
  columnOrder?: string[]
  /** 默认筛选条件 */
  defaultFilters?: Record<string, unknown>
  /** 默认排序 */
  defaultSort?: { field: string, order: 'asc' | 'desc' }
  /** 默认分页大小 */
  pageSize?: number
  /** 是否默认视图 */
  isDefault?: boolean
}

/**
 * 归一化查询参数 —— 框架向资源层传递的统一查询契约。
 * 页面侧资源适配器负责将其映射为具体后端 API 的入参。
 */
export interface SchemaQueryParams {
  /** 页码（从 1 开始） */
  page: number
  /** 每页数量 */
  pageSize: number
  /** 排序字段 */
  sortField?: string
  /** 排序方向 */
  sortOrder?: 'asc' | 'desc'
  /** 过滤条件（key → value，来源于 searchable/advancedSearch 字段） */
  filters: Record<string, unknown>
}

/**
 * 资源契约 —— 框架仅依赖归一化的 page / remove。
 * 与 defineResource 产出兼容（页面侧用适配器包装），亦可手工提供。
 */
export interface SchemaResource<TRow> {
  /** 分页查询（列表模式必填；接收归一化参数，返回标准分页结果） */
  page?: (params: SchemaQueryParams) => Promise<PageResult<TRow>>
  /**
   * 树形查询（树形模式必填）—— 返回嵌套数组（含 children），不分页。
   * 入参复用 SchemaQueryParams（page/pageSize 可忽略），filters 用于服务端过滤。
   */
  tree?: (params: SchemaQueryParams) => Promise<TRow[]>
  /** 删除单条（行级/批量删除依赖） */
  remove?: (id: ApiId) => Promise<void>
  /**
   * 新增单条（导入闭环依赖）—— 接收按 importable 字段组装的记录（field.key → 归一化值），
   * 页面适配器负责映射为后端 CreateDto（补默认值/裁剪字段）。
   */
  create?: (record: Record<string, unknown>) => Promise<unknown>
}

/**
 * 页面 Schema —— 整页单一事实源。
 */
export interface PageSchema<TRow = Record<string, unknown>> {
  /** 页面唯一码（偏好/视图按此维度存储） */
  pageCode: string
  /** 页面名称（i18n key） */
  pageName: string
  /** 页面级权限码 */
  permissions?: string[]
  /** 对应后端资源码（用于字段脱敏 FLS 规则匹配；缺省则不拉取脱敏规则） */
  resourceCode?: string
  /** 数据资源 */
  resource: SchemaResource<TRow>
  /** 字段单一事实源 */
  fields: ListFieldSchema<TRow>[]
  /** 操作集合（页面级/行级/批量级） */
  actions?: ActionSchema<TRow>[]
  /** 预置视图 */
  views?: ViewSchema[]
  /** 行主键字段，默认 'basicId' */
  rowKey?: string
  /** 启用内置批量删除（依赖 resource.remove；选中后批量浮条出现「批量删除」，框架统一确认/并发删除/刷新） */
  batchRemovable?: boolean
  /** 表格横向滚动宽度 */
  scrollX?: number
  /** 默认每页数量 */
  pageSize?: number
  /**
   * 树形模式配置（存在即启用树形）。
   * 启用后：走 resource.tree 取数（嵌套数组、不分页）、隐藏分页器、子行按 childrenKey 展开；
   * 需在 fields 里用 treeColumn:true 标记承载展开箭头的列。
   */
  tree?: {
    /** 子节点字段名（默认 children） */
    childrenKey?: string
    /** 默认展开全部（默认 true） */
    defaultExpandAll?: boolean
  }
}

/**
 * 表格列（Naive UI 原生列 + 框架元信息）
 */
export type SchemaColumn<TRow> = DataTableColumn<TRow>

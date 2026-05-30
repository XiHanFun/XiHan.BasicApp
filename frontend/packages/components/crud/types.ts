import type { DataTableColumns } from 'naive-ui'
import type { ApiId, PageRequest, PageResult } from '@/api'

/**
 * CRUD 资源契约：脚手架依赖的最小 API 形态。
 * 与 defineResource 产出的 ResourceApi 兼容，亦可由页面手工提供。
 */
export interface CrudResource<TList, TQuery> {
  /** 分页查询 */
  page: (query: TQuery) => Promise<PageResult<TList>>
  /** 删除（可选，未提供时表格不渲染删除按钮） */
  remove?: (id: ApiId) => Promise<void>
}

/**
 * 搜索字段控件类型
 */
export type CrudSearchFieldType = 'input' | 'select'

/**
 * 下拉选项
 */
export interface CrudSelectOption {
  label: string
  value: string | number | boolean
}

/**
 * 搜索表单字段 schema
 */
export interface CrudSearchField {
  /** 绑定字段名（对应查询对象的 key） */
  field: string
  /** 占位提示 */
  placeholder?: string
  /** 控件类型，默认 input */
  type?: CrudSearchFieldType
  /** select 选项 */
  options?: CrudSelectOption[]
  /** 控件宽度（px），默认 200 */
  width?: number
}

/**
 * CRUD 列表页配置
 */
export interface CrudPageProps<TList, TQuery extends Partial<PageRequest>> {
  /** 资源 API */
  resource: CrudResource<TList, TQuery>
  /** 表格列（Naive UI 原生列定义） */
  columns: DataTableColumns<TList>
  /** 行主键字段，默认 'basicId' */
  rowKey?: keyof TList & string
  /** 搜索字段 schema */
  searchFields?: CrudSearchField[]
  /** 查询对象默认值（含分页外的过滤条件初值） */
  defaultQuery?: Partial<TQuery>
  /** 表格横向滚动宽度 */
  scrollX?: number
  /** 每页可选数量 */
  pageSizes?: number[]
}

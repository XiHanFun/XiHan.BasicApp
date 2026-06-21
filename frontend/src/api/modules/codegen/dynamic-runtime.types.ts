import type { ApiId } from '../../types'

/**
 * 零代码运行时列元数据 DTO（镜像后端 DynamicRuntimeColumnDto，camelCase）。
 * 仅描述列的展示/查询特征，运行时按此动态渲染表格列（不写死业务字段）。
 */
export interface DynamicRuntimeColumnDto {
  columnName: string
  propertyName: string
  label?: string | null
  tsType?: string | null
  htmlType?: string | null
  queryType?: string | null
  isList: boolean
  isQuery: boolean
  isRequired: boolean
}

/**
 * 零代码运行时表结构 DTO（镜像后端 DynamicRuntimeSchemaDto，camelCase）。
 */
export interface DynamicRuntimeSchemaDto {
  tableId: ApiId
  tableName: string
  className: string
  tableComment?: string | null
  columns: DynamicRuntimeColumnDto[]
}

/**
 * 零代码运行时分页查询 DTO（镜像后端 DynamicRuntimePageQueryDto，camelCase）。
 */
export interface DynamicRuntimePageQueryDto {
  tableId: ApiId
  pageIndex: number
  pageSize: number
}

/**
 * 零代码运行时分页结果 DTO（镜像后端 DynamicRuntimePageResultDto，camelCase）。
 * rows 为动态行：键名对应列 propertyName，值类型未知，渲染时转字符串。
 */
export interface DynamicRuntimePageResultDto {
  rows: Array<Record<string, unknown>>
  totalCount: number
  pageIndex: number
  pageSize: number
}

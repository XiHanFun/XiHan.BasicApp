/**
 * 前端基础契约类型底层（API 原始类型 + 分页/查询结构 + Basic DTO）。
 *
 * 这些类型是前后端契约的前端表达，属于「可复用基建」，故归属 packages 底层；
 * `src/api/types.ts` 反向 re-export 它们以保持 `@/api` 入口不变。
 * 见架构审查报告 D1/H8：packages 不得反向依赖 src。
 */

export type ApiId = string

/**
 * 后端 long 数值（雪花 ID 之外的耗时/大小等）经全局 LongJsonConverter 统一序列化为字符串。
 * 前端如实标注为字符串；需数值运算/格式化时显式 `Number()` 转换。
 */
export type NumericString = string

export type DateTimeString = string

export type ApiPrimitive = boolean | DateTimeString | null | number | string

export enum QueryOperator {
  Equal = 1000,
  NotEqual = 1001,
  GreaterThan = 1002,
  GreaterThanOrEqual = 1003,
  LessThan = 1004,
  LessThanOrEqual = 1005,
  Contains = 2000,
  StartsWith = 2001,
  EndsWith = 2002,
  In = 3000,
  NotIn = 3001,
  Between = 4000,
  IsNull = 5000,
  IsNotNull = 5001,
}

export enum SortDirection {
  Ascending = 1000,
  Descending = 1001,
}

export interface QueryFilter {
  field: string
  operator: QueryOperator
  value?: ApiPrimitive
  values?: ApiPrimitive[]
}

export interface QuerySort {
  direction: SortDirection
  field: string
  priority: number
}

export interface QueryKeyword {
  fields: string[]
  value?: string
}

export interface QueryConditions {
  filters: QueryFilter[]
  keyword?: QueryKeyword | null
  sorts: QuerySort[]
}

export interface QueryBehavior {
  disableDefaultSort: boolean
  disablePaging: boolean
  enableSplitQuery: boolean
  ignoreSoftDelete: boolean
  ignoreTenant: boolean
  queryTimeout?: number | null
}

export interface PageRequestMetadata {
  pageIndex: number
  pageSize: number
}

export interface PageResultMetadata {
  currentPageCount: number
  endRecord: number
  hasNext: boolean
  hasPrevious: boolean
  isFirstPage: boolean
  isLastPage: boolean
  pageIndex: number
  pageSize: number
  startRecord: number
  totalCount: number
  totalPages: number
}

export interface PageRequest {
  behavior: QueryBehavior
  conditions: QueryConditions
  page: PageRequestMetadata
}

export interface PageResult<TItem> {
  extendDatas?: Record<string, unknown>
  items: TItem[]
  page: PageResultMetadata
}

export interface BasicDto {
  basicId: ApiId
}

export interface BasicCreateDto {
  createdBy?: string | null
  createdId?: ApiId | null
}

export interface BasicUpdateDto extends BasicDto {
  modifiedBy?: string | null
  modifiedId?: ApiId | null
}

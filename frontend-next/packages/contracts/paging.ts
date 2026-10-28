import type { components } from './types'
import type { SchemaQueryParams } from '~/components/schema/types'
import type { PageResult } from '~/types/contracts'
import { QueryOperator } from '~/types/contracts'

type PageFilter = components['schemas']['PageFilter']

/** 各列表操作共用的分页请求（后端 `PageRequest`；OpenAPI 里按操作内联，没有单独的 schema 名） */
export interface PageRequest {
  page: components['schemas']['PageInfo']
  filters: PageFilter[]
  sorts: components['schemas']['PageSort'][]
  keyword: null | string
}

/** 列表页搜索字段 → 后端筛选：后端按白名单接受 `<字段, 运算符>`，页面登记它用到的那几个 */
export interface PageQueryMapping {
  /** 落到 `PageRequest.keyword` 的搜索字段（缺省 `keyword`） */
  keyword?: string
  /** 其余搜索字段的运算符（字段键即后端筛选字段名） */
  filters?: Record<string, 'contains' | 'eq' | 'ge' | 'le'>
  /** 区间搜索（searchRange）的字段：区间拆成后端的 `ge` 与 `le` 两条 */
  ranges?: readonly string[]
  /** 多选搜索（searchMultiple）的字段：选中的值合成后端的 `in`（逗号分隔） */
  multiples?: readonly string[]
}

function isBlank(value: unknown): boolean {
  return value === undefined || value === null || (typeof value === 'string' && value.trim() === '')
}

/**
 * SchemaPage 的归一化查询参数 → 6.0 的 `PageRequest`（4.x 由 `@/api` 的 createPageRequest 承担）。
 * 区间与多选（conditionFilters：Between / In）只接页面登记过的字段；没登记的出现即抛错，而不是静默丢掉条件。
 */
export function toPageRequest(params: SchemaQueryParams, mapping: PageQueryMapping = {}): PageRequest {
  const keywordField = mapping.keyword ?? 'keyword'
  const ranges = new Set(mapping.ranges ?? [])
  const multiples = new Set(mapping.multiples ?? [])
  const filters: PageFilter[] = []
  for (const condition of params.conditionFilters ?? []) {
    const values = (condition.values ?? []).map(value => String(value))
    if (condition.operator === QueryOperator.Between && ranges.has(condition.field) && values.length === 2)
      filters.push({ field: condition.field, operator: 'ge', value: values[0]! }, { field: condition.field, operator: 'le', value: values[1]! })
    else if (condition.operator === QueryOperator.In && multiples.has(condition.field) && values.length > 0)
      filters.push({ field: condition.field, operator: 'in', value: values.join(',') })
    else
      throw new Error(`搜索字段 ${condition.field} 的区间 / 多选条件没有登记后端筛选：页面要在 ranges / multiples 里登记它（searchRange / searchMultiple）`)
  }
  for (const [field, value] of Object.entries(params.filters)) {
    // 区间与多选的原始值已由 conditionFilters 下发
    if (field === keywordField || ranges.has(field) || multiples.has(field) || isBlank(value))
      continue
    const operator = mapping.filters?.[field]
    if (!operator)
      throw new Error(`搜索字段 ${field} 没有登记后端筛选运算符`)
    filters.push({ field, operator, value: String(value).trim() })
  }
  const keyword = params.filters[keywordField]
  return {
    page: { index: params.page, size: params.pageSize },
    filters,
    sorts: (params.sorts ?? []).map(sort => ({ field: sort.field, descending: sort.order === 'desc' })),
    keyword: isBlank(keyword) ? null : String(keyword).trim(),
  }
}

/** 6.0 的分页结果（`items / total / page`）→ SchemaPage 读的分页结果（4.x 的 `PageResult`） */
export function toSchemaPageResult<T>(result: { items: T[], total: number | string, page: { index?: number, size?: number } }, requested: SchemaQueryParams): PageResult<T> {
  const totalCount = Number(result.total)
  const pageIndex = result.page.index ?? requested.page
  const pageSize = result.page.size ?? requested.pageSize
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const startRecord = totalCount === 0 ? 0 : (pageIndex - 1) * pageSize + 1
  return {
    items: result.items,
    page: {
      currentPageCount: result.items.length,
      endRecord: totalCount === 0 ? 0 : startRecord + result.items.length - 1,
      hasNext: pageIndex < totalPages,
      hasPrevious: pageIndex > 1,
      isFirstPage: pageIndex <= 1,
      isLastPage: pageIndex >= totalPages,
      pageIndex,
      pageSize,
      startRecord,
      totalCount,
      totalPages,
    },
  }
}

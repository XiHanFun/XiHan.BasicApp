import type {
  ApiPrimitive,
  PageRequest,
  QueryBehavior,
  QueryConditions,
  QueryFilter,
  QueryKeyword,
  QuerySort,
} from './types'
import { SortDirection } from './types'

export function createDefaultQueryBehavior(input: Partial<QueryBehavior> = {}): QueryBehavior {
  return {
    disableDefaultSort: false,
    disablePaging: false,
    enableSplitQuery: false,
    ignoreSoftDelete: false,
    ignoreTenant: false,
    queryTimeout: null,
    ...input,
  }
}

export function createDefaultQueryConditions(input: Partial<QueryConditions> = {}): QueryConditions {
  return {
    filters: [],
    keyword: null,
    sorts: [],
    ...input,
  }
}

export function createPageRequest(input: {
  behavior?: Partial<QueryBehavior>
  conditions?: Partial<QueryConditions>
  page?: { pageIndex?: number, pageSize?: number }
} = {}): PageRequest {
  return {
    behavior: createDefaultQueryBehavior(input.behavior),
    conditions: createDefaultQueryConditions(input.conditions),
    page: {
      pageIndex: input.page?.pageIndex ?? 1,
      pageSize: input.page?.pageSize ?? 20,
    },
  }
}

export function queryKeyword(value: string | undefined, fields: string[]): QueryKeyword | null {
  const keyword = value?.trim()
  if (!keyword) {
    return null
  }

  return {
    fields: fields.filter(field => field.trim().length > 0),
    value: keyword,
  }
}

export function queryFilter(
  field: string,
  value: ApiPrimitive,
  operator: QueryFilter['operator'],
): QueryFilter {
  return {
    field,
    operator,
    value,
  }
}

export function querySort(field: string, direction: SortDirection, priority = 0): QuerySort {
  return {
    direction,
    field,
    priority,
  }
}

/**
 * 由 SchemaPage 的归一化排序（sortField + 'asc'|'desc'）构建后端 conditions.sorts 数组。
 * 无排序字段时返回空数组（后端据此回退各自的默认排序）。
 */
export function querySortsFromSchema(field?: string, order?: 'asc' | 'desc'): QuerySort[] {
  if (!field) {
    return []
  }
  return [querySort(field, order === 'desc' ? SortDirection.Descending : SortDirection.Ascending)]
}

export function compactRecord<T extends Record<string, unknown>>(input: T): Partial<T> {
  return Object.fromEntries(
    Object.entries(input).filter(([, value]) => value !== undefined && value !== null && value !== ''),
  ) as Partial<T>
}

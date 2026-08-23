import type {
  ApiPrimitive,
  PageRequest,
  QueryConditions,
  QueryFilter,
  QueryKeyword,
  QuerySort,
} from './types'
import { SortDirection } from './types'

export function createDefaultQueryConditions(input: Partial<QueryConditions> = {}): QueryConditions {
  return {
    filters: [],
    keyword: null,
    sorts: [],
    ...input,
  }
}

export function createPageRequest(input: {
  conditions?: Partial<QueryConditions>
  page?: { pageIndex?: number, pageSize?: number }
} = {}): PageRequest {
  return {
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
 * 由 SchemaPage 的归一化多字段排序构建后端 conditions.sorts 数组。
 * 数组顺序即优先级（priority = 下标，0 为主排序）；为空时返回空数组（后端回退各自默认排序）。
 */
export function querySortsFromSchema(sorts?: ReadonlyArray<{ field: string, order: 'asc' | 'desc' }>): QuerySort[] {
  if (!sorts?.length) {
    return []
  }
  return sorts.map((s, index) =>
    querySort(s.field, s.order === 'desc' ? SortDirection.Descending : SortDirection.Ascending, index))
}

export function compactRecord<T extends Record<string, unknown>>(input: T): Partial<T> {
  return Object.fromEntries(
    Object.entries(input).filter(([, value]) => value !== undefined && value !== null && value !== ''),
  ) as Partial<T>
}

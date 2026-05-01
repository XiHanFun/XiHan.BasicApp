import type {
  ApiPrimitive,
  PageRequest,
  QueryBehavior,
  QueryConditions,
  QueryFilter,
  QueryKeyword,
  QuerySort,
  SortDirection,
} from './types'

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

export function createPageRequest(input: Partial<PageRequest> = {}): PageRequest {
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

export function compactRecord<T extends Record<string, unknown>>(input: T): Partial<T> {
  return Object.fromEntries(
    Object.entries(input).filter(([, value]) => value !== undefined && value !== null && value !== ''),
  ) as Partial<T>
}

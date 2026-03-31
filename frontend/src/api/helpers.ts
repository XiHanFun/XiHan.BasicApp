import type { PageQuery, PageResult } from '~/types'

export interface AnyRecord {
  [key: string]: any
}

interface BuildPageRequestOptions {
  disablePaging?: boolean
  filterFieldMap?: Record<string, string>
  keywordFields?: string[]
}

const QUERY_OPERATOR_EQUAL = 0

function hasValue(value: unknown) {
  if (value === undefined || value === null) {
    return false
  }
  if (typeof value === 'string') {
    return value.trim().length > 0
  }
  return true
}

export function unwrapPayload<T = any>(raw: unknown): T {
  if (raw && typeof raw === 'object' && 'data' in (raw as AnyRecord)) {
    return ((raw as AnyRecord).data ?? raw) as T
  }
  return raw as T
}

export function toId(value: unknown): string {
  if (value === undefined || value === null) {
    return ''
  }
  return String(value)
}

export function toNumber(value: unknown, fallback = 0): number {
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : fallback
}

export function normalizePageResult<T>(
  raw: AnyRecord,
  mapper?: (item: AnyRecord) => T,
): PageResult<T> {
  const payload = unwrapPayload<AnyRecord>(raw)
  if (!payload || typeof payload !== 'object') {
    return { items: [], total: 0, page: 1, pageSize: 20 }
  }

  const pageMeta = payload.page ?? payload.pagination ?? {}
  const list = payload.items ?? payload.records ?? payload.data ?? []
  const total = toNumber(
    payload.total ?? payload.totalCount ?? payload.count ?? pageMeta.totalCount ?? pageMeta.total,
    0,
  )
  const page = toNumber(
    payload.pageIndex ?? payload.page ?? payload.current ?? pageMeta.pageIndex,
    1,
  )
  const pageSize = toNumber(payload.pageSize ?? payload.size ?? pageMeta.pageSize, 20)

  const items = Array.isArray(list) ? list : []
  return {
    items: mapper ? items.map(item => mapper(item as AnyRecord)) : (items as T[]),
    total: Math.max(0, total),
    page: Math.max(1, page),
    pageSize: Math.max(1, pageSize),
  }
}

/**
 * 将后端分页响应扁平化为 vxe-table proxyConfig 所需的 { items, total } 格式
 */
export function flattenPageResponse<T = any>(res: AnyRecord): { items: T[], total: number } {
  if (!res || typeof res !== 'object') {
    return { items: [], total: 0 }
  }
  const items = (res.items ?? res.records ?? res.data ?? []) as T[]
  const pageMeta = res.page ?? res.pagination ?? {}
  const total = toNumber(
    res.total ?? res.totalCount ?? pageMeta.totalCount ?? pageMeta.total,
    0,
  )
  return { items: Array.isArray(items) ? items : [], total }
}

export function buildPageRequest(
  query: (PageQuery & Record<string, any>) | undefined,
  options: BuildPageRequestOptions = {},
) {
  const source = query ?? {}
  const pageIndex = Math.max(1, toNumber(source.page, 1))
  const pageSize = Math.max(1, toNumber(source.pageSize, 20))
  const filters = Object.entries(source)
    .filter(([key, value]) => key !== 'page' && key !== 'pageSize' && key !== 'keyword' && hasValue(value))
    .map(([key, value]) => ({
      field: options.filterFieldMap?.[key] ?? key,
      value,
      operator: QUERY_OPERATOR_EQUAL,
    }))

  const payload: AnyRecord = {
    conditions: {
      filters,
      sorts: [],
    },
    behavior: {},
    page: {
      pageIndex,
      pageSize,
    },
  }

  if (hasValue(source.keyword)) {
    payload.conditions.keyword = {
      value: String(source.keyword).trim(),
      fields: options.keywordFields ?? [],
    }
  }

  if (options.disablePaging) {
    payload.behavior.disablePaging = true
  }

  return payload
}

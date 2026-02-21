import type { PageResult } from '~/types'

interface AnyRecord {
  [key: string]: any
}

export function normalizePageResult<T>(raw: AnyRecord): PageResult<T> {
  if (!raw || typeof raw !== 'object') {
    return { items: [], total: 0, page: 1, pageSize: 20 }
  }

  const list = raw.items ?? raw.records ?? raw.data ?? []
  const total = Number(raw.total ?? raw.totalCount ?? raw.count ?? 0)
  const page = Number(raw.page ?? raw.current ?? raw.pageIndex ?? 1)
  const pageSize = Number(raw.pageSize ?? raw.size ?? 20)

  return {
    items: Array.isArray(list) ? list : [],
    total: Number.isFinite(total) ? total : 0,
    page: Number.isFinite(page) ? page : 1,
    pageSize: Number.isFinite(pageSize) ? pageSize : 20,
  }
}

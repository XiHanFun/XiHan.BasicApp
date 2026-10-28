import type { SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { permissions as permissionsApi, toSchemaPageResult } from '~/contracts'

export type PermissionRow = ResponseOf<'permissions.list'>['items'][number]
export type PermissionScope = PermissionRow['scope']

export const PERMISSION_SCOPES: readonly PermissionScope[] = ['Platform', 'Tenant', 'Both']

function isBlank(value: unknown): boolean {
  return value === undefined || value === null || (typeof value === 'string' && value.trim() === '')
}

/**
 * 权限目录是各包在代码里登记的（只读，6.0 没有增删改）：`permissions.list` 一次给全调用方这一侧的目录，筛选、排序与分页都在前端做。
 * 页面登记的字段之外的条件直接报错，不静默丢掉（与 toPageRequest 同一口径）。
 */
export function permissionsResource(): SchemaResource<PermissionRow> {
  return {
    page: async (params) => {
      const unsupported = [
        ...Object.keys(params.filters).filter(field => !['keyword', 'package', 'scope', 'audited'].includes(field)),
        ...(params.sorts ?? []).map(sort => sort.field).filter(field => !['code', 'package'].includes(field)),
        ...(params.conditionFilters ?? []).map(filter => filter.field),
      ]
      if (unsupported.length > 0)
        throw new Error(`权限目录不支持按 ${unsupported.join(', ')} 筛选或排序`)
      const { items } = await permissionsApi.list({})
      const keyword = isBlank(params.filters.keyword) ? '' : String(params.filters.keyword).trim().toLowerCase()
      const filtered = items.filter(item =>
        (keyword === '' || [item.code, item.displayName, item.description ?? ''].some(text => text.toLowerCase().includes(keyword)))
        && (isBlank(params.filters.package) || item.package === params.filters.package)
        && (isBlank(params.filters.scope) || item.scope === params.filters.scope)
        && (isBlank(params.filters.audited) || String(item.audited) === String(params.filters.audited)))
      const sort = params.sorts?.[0]
      const ordered = sort
        ? [...filtered].sort((a, b) => (sort.order === 'desc' ? -1 : 1) * String(a[sort.field as 'code' | 'package']).localeCompare(String(b[sort.field as 'code' | 'package'])))
        : filtered
      const start = (params.page - 1) * params.pageSize
      return toSchemaPageResult({ items: ordered.slice(start, start + params.pageSize), total: ordered.length, page: { index: params.page, size: params.pageSize } }, params)
    },
  }
}

/** 目录里出现过的包，给筛选下拉用 */
export function packagesOf(items: readonly PermissionRow[]): string[] {
  return [...new Set(items.map(item => item.package))].sort()
}

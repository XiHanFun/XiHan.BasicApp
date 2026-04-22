import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'

const api = useBaseApi('Permission')

// -------- 类型 --------

export interface SysPermission {
  basicId: string
  resourceId: string
  operationId: string
  permissionName: string
  permissionCode: string
  permissionDescription?: string
  description?: string
  isRequireAudit?: boolean
  isGlobal?: boolean
  priority?: number
  sort?: number
  tenantId?: number
  remark?: string
  createTime?: string
  updateTime?: string
  tags?: string
  tagList?: string[]
  primaryTag?: string
  status?: number
}

export interface PermissionPageQuery extends PageQuery {
  status?: number
  resourceId?: string
  operationId?: string
}

// -------- 内部 --------

const STATUS_MAP: Record<string, number> = {
  No: 0,
  Yes: 1,
}

function resolveEnum(value: unknown, map: Record<string, number>, fallback: number): number {
  if (value === undefined || value === null) {
    return fallback
  }
  if (typeof value === 'number') {
    return value
  }
  if (typeof value === 'string') {
    return map[value] ?? toNumber(value, fallback)
  }
  return fallback
}

function splitTags(tags: unknown): string[] {
  if (typeof tags !== 'string' || tags.trim().length === 0) {
    return []
  }
  return tags
    .split(/[,\uFF0C;\uFF1B|\u3001]/)
    .map(item => item.trim())
    .filter(Boolean)
}

function normalizePermission(raw: Record<string, any>): SysPermission {
  const tags = raw.tags ?? raw.Tags
  const tagList = splitTags(tags)
  return {
    basicId: toId(raw.basicId ?? raw.BasicId),
    resourceId: toId(raw.resourceId ?? raw.ResourceId),
    operationId: toId(raw.operationId ?? raw.OperationId),
    permissionName: raw.permissionName ?? raw.PermissionName ?? '',
    permissionCode: raw.permissionCode ?? raw.PermissionCode ?? '',
    permissionDescription: raw.permissionDescription ?? raw.PermissionDescription ?? raw.description ?? raw.Description ?? '',
    description: raw.permissionDescription ?? raw.PermissionDescription ?? raw.description ?? raw.Description ?? '',
    isRequireAudit: Boolean(raw.isRequireAudit ?? raw.IsRequireAudit),
    isGlobal: Boolean(raw.isGlobal ?? raw.IsGlobal),
    priority: toNumber(raw.priority ?? raw.Priority, 0),
    sort: toNumber(raw.sort ?? raw.Sort, 0),
    tenantId: raw.tenantId === null || raw.tenantId === undefined
      ? (raw.TenantId === null || raw.TenantId === undefined ? undefined : toNumber(raw.TenantId, 0))
      : toNumber(raw.tenantId, 0),
    remark: raw.remark ?? raw.Remark ?? undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? raw.CreatedTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? raw.modifiedTime ?? raw.ModifiedTime ?? undefined,
    tags: typeof tags === 'string' && tags.trim().length > 0 ? tags.trim() : undefined,
    tagList,
    primaryTag: tagList[0],
    status: resolveEnum(raw.status ?? raw.Status, STATUS_MAP, 1),
  }
}

function toCreatePayload(data: Partial<SysPermission>) {
  const normalizedTags = typeof data.tags === 'string'
    ? data.tags.trim()
    : Array.isArray(data.tagList)
      ? data.tagList.map(item => item.trim()).filter(Boolean).join(',')
      : ''
  return {
    resourceId: toId(data.resourceId ?? ''),
    operationId: toId(data.operationId ?? ''),
    permissionCode: data.permissionCode ?? '',
    permissionName: data.permissionName ?? '',
    permissionDescription: data.permissionDescription ?? data.description ?? '',
    tags: normalizedTags,
    isRequireAudit: Boolean(data.isRequireAudit),
    isGlobal: Boolean(data.isGlobal),
    priority: toNumber(data.priority, 0),
    sort: toNumber(data.sort, 0),
    tenantId: data.tenantId === undefined ? null : toNumber(data.tenantId, 0),
    remark: data.remark ?? '',
  }
}

function toUpdatePayload(id: string, data: Partial<SysPermission>) {
  return {
    ...toCreatePayload(data),
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

const PAGE_OPTIONS = {
  keywordFields: ['PermissionCode', 'PermissionName', 'PermissionDescription'],
  filterFieldMap: { status: 'Status', resourceId: 'ResourceId', operationId: 'OperationId' },
}

async function queryPermissionPage(params: Record<string, any>) {
  const data = await api.request.post<any>(
    `${api.baseUrl}Page`,
    buildPageRequest(params, PAGE_OPTIONS),
  )
  return normalizePageResult(data, normalizePermission)
}

// -------- API --------

export const permissionApi = {
  page: (params: Record<string, any>) => queryPermissionPage(params),

  list: async (params: Partial<PermissionPageQuery> = {}) => {
    const data = await api.request.post<any>(
      `${api.baseUrl}Page`,
      buildPageRequest({ page: 1, pageSize: 9999, ...params }, { disablePaging: true, ...PAGE_OPTIONS }),
    )
    return normalizePageResult(data, normalizePermission).items
  },

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizePermission),

  create: (data: Partial<SysPermission>) => api.create(toCreatePayload(data)),

  update: (id: string, data: Partial<SysPermission>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.delete(id),
}

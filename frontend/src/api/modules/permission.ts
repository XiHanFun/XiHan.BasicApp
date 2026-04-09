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
  priority?: number
  sort?: number
  tenantId?: number
  remark?: string
  createTime?: string
  updateTime?: string
  groupName?: string
  tags?: string
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

function resolveGroupName(tags: unknown, groupName: unknown): string {
  if (typeof groupName === 'string' && groupName.trim().length > 0) {
    return groupName.trim()
  }
  if (typeof tags !== 'string' || tags.trim().length === 0) {
    return ''
  }
  const first = tags
    .split(/[,\uFF0C;\uFF1B|\u3001]/)
    .map(item => item.trim())
    .find(Boolean)
  return first ?? ''
}

function normalizeTags(tags: unknown, groupName: unknown): string {
  if (typeof tags === 'string' && tags.trim().length > 0) {
    return tags.trim()
  }
  if (typeof groupName === 'string' && groupName.trim().length > 0) {
    return groupName.trim()
  }
  return ''
}

function normalizePermission(raw: Record<string, any>): SysPermission {
  const rawTags = raw.tags ?? raw.Tags
  const rawGroupName = raw.groupName ?? raw.GroupName
  const normalizedTags = normalizeTags(rawTags, rawGroupName)
  return {
    basicId: toId(raw.basicId ?? raw.BasicId),
    resourceId: toId(raw.resourceId ?? raw.ResourceId),
    operationId: toId(raw.operationId ?? raw.OperationId),
    permissionName: raw.permissionName ?? raw.PermissionName ?? '',
    permissionCode: raw.permissionCode ?? raw.PermissionCode ?? '',
    permissionDescription: raw.permissionDescription ?? raw.PermissionDescription ?? raw.description ?? raw.Description ?? '',
    description: raw.permissionDescription ?? raw.PermissionDescription ?? raw.description ?? raw.Description ?? '',
    isRequireAudit: Boolean(raw.isRequireAudit ?? raw.IsRequireAudit),
    priority: toNumber(raw.priority ?? raw.Priority, 0),
    sort: toNumber(raw.sort ?? raw.Sort, 0),
    tenantId: raw.tenantId === null || raw.tenantId === undefined
      ? (raw.TenantId === null || raw.TenantId === undefined ? undefined : toNumber(raw.TenantId, 0))
      : toNumber(raw.tenantId, 0),
    remark: raw.remark ?? raw.Remark ?? undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? raw.CreatedTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? raw.modifiedTime ?? raw.ModifiedTime ?? undefined,
    groupName: resolveGroupName(rawTags, rawGroupName),
    tags: normalizedTags || undefined,
    status: resolveEnum(raw.status ?? raw.Status, STATUS_MAP, 1),
  }
}

function toCreatePayload(data: Partial<SysPermission>) {
  const groupName = resolveGroupName(data.tags, data.groupName)
  const tags = normalizeTags(data.tags, groupName)
  return {
    resourceId: toId(data.resourceId ?? '1') || '1',
    operationId: toId(data.operationId ?? '1') || '1',
    permissionCode: data.permissionCode ?? '',
    permissionName: data.permissionName ?? '',
    permissionDescription: data.permissionDescription ?? data.description ?? '',
    groupName,
    tags,
    isRequireAudit: Boolean(data.isRequireAudit),
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

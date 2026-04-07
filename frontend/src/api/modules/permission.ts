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
  status?: number
}

export interface PermissionPageQuery extends PageQuery {
  status?: number
  resourceId?: string
  operationId?: string
}

// -------- 内部 --------

function normalizePermission(raw: Record<string, any>): SysPermission {
  return {
    basicId: toId(raw.basicId),
    resourceId: toId(raw.resourceId),
    operationId: toId(raw.operationId),
    permissionName: raw.permissionName ?? '',
    permissionCode: raw.permissionCode ?? '',
    permissionDescription: raw.permissionDescription ?? raw.description ?? '',
    description: raw.permissionDescription ?? raw.description ?? '',
    isRequireAudit: Boolean(raw.isRequireAudit),
    priority: toNumber(raw.priority, 0),
    sort: toNumber(raw.sort, 0),
    tenantId: raw.tenantId === null || raw.tenantId === undefined ? undefined : toNumber(raw.tenantId, 0),
    remark: raw.remark ?? undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    groupName: raw.groupName ?? '',
    status: toNumber(raw.status, 1),
  }
}

function toCreatePayload(data: Partial<SysPermission>) {
  return {
    resourceId: toId(data.resourceId ?? '1') || '1',
    operationId: toId(data.operationId ?? '1') || '1',
    permissionCode: data.permissionCode ?? '',
    permissionName: data.permissionName ?? '',
    permissionDescription: data.permissionDescription ?? data.description ?? '',
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

// -------- API --------

export const permissionApi = {
  page: (params: Record<string, any>) => api.page(params, PAGE_OPTIONS),

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

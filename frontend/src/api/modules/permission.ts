import type { PageResult, PermissionPageQuery, SysPermission } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const PERMISSION_API = '/api/Permission'

function normalizePermission(raw: Record<string, any>): SysPermission {
  return {
    basicId: toId(raw.basicId),
    resourceId: toNumber(raw.resourceId, 0),
    operationId: toNumber(raw.operationId, 0),
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

function toPermissionCreatePayload(data: Partial<SysPermission>) {
  return {
    resourceId: Math.max(1, toNumber(data.resourceId, 1)),
    operationId: Math.max(1, toNumber(data.operationId, 1)),
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

function toPermissionUpdatePayload(id: string, data: Partial<SysPermission>) {
  return {
    ...toPermissionCreatePayload(data),
    status: toNumber(data.status, 1),
    basicId: toNumber(id, 0),
  }
}

export async function getPermissionPageApi(
  params: PermissionPageQuery,
): Promise<PageResult<SysPermission>> {
  const data = await requestClient.post<any>(
    `${PERMISSION_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['PermissionCode', 'PermissionName', 'PermissionDescription'],
      filterFieldMap: {
        status: 'Status',
        resourceId: 'ResourceId',
        operationId: 'OperationId',
      },
    }),
  )
  return normalizePageResult(data, normalizePermission)
}

export async function getPermissionListApi(params: Partial<PermissionPageQuery> = {}) {
  const data = await requestClient.post<any>(
    `${PERMISSION_API}/Page`,
    buildPageRequest(
      { page: 1, pageSize: 9999, ...params },
      {
        disablePaging: true,
        keywordFields: ['PermissionCode', 'PermissionName', 'PermissionDescription'],
        filterFieldMap: {
          status: 'Status',
          resourceId: 'ResourceId',
          operationId: 'OperationId',
        },
      },
    ),
  )
  return normalizePageResult(data, normalizePermission).items
}

export function getPermissionDetailApi(id: string) {
  return requestClient
    .get<any>(`${PERMISSION_API}/ById`, { params: { id } })
    .then(raw => normalizePermission(raw))
}

export function createPermissionApi(data: Partial<SysPermission>) {
  return requestClient.post<void>(`${PERMISSION_API}/Create`, toPermissionCreatePayload(data))
}

export function updatePermissionApi(id: string, data: Partial<SysPermission>) {
  return requestClient.put<void>(`${PERMISSION_API}/Update`, toPermissionUpdatePayload(id, data), {
    params: { id },
  })
}

export function deletePermissionApi(id: string) {
  return requestClient.delete<void>(`${PERMISSION_API}/Delete`, {
    params: { id },
  })
}

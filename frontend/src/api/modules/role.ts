import type { PageResult, RolePageQuery, SysRole } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const ROLE_API = '/api/Role'

function normalizeRole(raw: Record<string, any>): SysRole {
  return {
    basicId: toId(raw.basicId),
    name: raw.name ?? raw.roleName ?? '',
    code: raw.code ?? raw.roleCode ?? '',
    description: raw.description ?? raw.roleDescription ?? '',
    status: toNumber(raw.status, 1),
    sort: toNumber(raw.sort, 0),
    permissions: Array.isArray(raw.permissions) ? raw.permissions : [],
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? '',
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
  }
}

function toRoleCreatePayload(data: Partial<SysRole>) {
  return {
    roleCode: data.code ?? '',
    roleName: data.name ?? '',
    roleDescription: data.description ?? '',
    roleType: 0,
    dataScope: 0,
    sort: toNumber(data.sort, 0),
  }
}

function toRoleUpdatePayload(id: string, data: Partial<SysRole>) {
  return {
    roleName: data.name ?? '',
    roleDescription: data.description ?? '',
    dataScope: 0,
    status: toNumber(data.status, 1),
    sort: toNumber(data.sort, 0),
    basicId: toId(id),
  }
}

export async function getRolePageApi(params: RolePageQuery): Promise<PageResult<SysRole>> {
  const data = await requestClient.post<any>(
    `${ROLE_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['RoleName', 'RoleCode', 'RoleDescription'],
      filterFieldMap: {
        status: 'Status',
      },
    }),
  )
  return normalizePageResult(data, normalizeRole)
}

export async function getRoleListApi() {
  const data = await requestClient.post<any>(
    `${ROLE_API}/Page`,
    buildPageRequest(
      { page: 1, pageSize: 9999 },
      {
        disablePaging: true,
      },
    ),
  )
  return normalizePageResult(data, normalizeRole).items
}

export function getRoleDetailApi(id: string) {
  return requestClient
    .get<any>(`${ROLE_API}/ById`, { params: { id } })
    .then(raw => normalizeRole(raw))
}

export function createRoleApi(data: Partial<SysRole>) {
  return requestClient.post<void>(`${ROLE_API}/Create`, toRoleCreatePayload(data))
}

export function updateRoleApi(id: string, data: Partial<SysRole>) {
  return requestClient.put<void>(`${ROLE_API}/Update`, toRoleUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteRoleApi(id: string) {
  return requestClient.delete<void>(`${ROLE_API}/Delete/${id}`)
}

export function assignRolePermissionsApi(id: string, permissions: string[]) {
  return requestClient.post<void>(`${ROLE_API}/AssignPermissions`, {
    roleId: toId(id),
    permissionIds: permissions.map(item => toId(item)).filter(item => item.length > 0),
  })
}

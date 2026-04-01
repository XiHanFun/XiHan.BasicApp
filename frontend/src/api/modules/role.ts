import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'

const api = useBaseApi('Role')

// -------- 类型 --------

export interface SysRole {
  basicId: string
  roleName: string
  roleCode: string
  roleDescription?: string
  roleType?: number
  dataScope?: number
  status: number
  sort: number
  permissions: string[]
  createTime: string
  updateTime?: string
}

export interface RolePageQuery extends PageQuery {
  status?: number
}

// -------- 内部 --------

function normalizeRole(raw: Record<string, any>): SysRole {
  return {
    basicId: toId(raw.basicId),
    roleName: raw.roleName ?? raw.name ?? '',
    roleCode: raw.roleCode ?? raw.code ?? '',
    roleDescription: raw.roleDescription ?? raw.description ?? '',
    roleType: raw.roleType !== undefined && raw.roleType !== null ? toNumber(raw.roleType, 0) : undefined,
    dataScope: raw.dataScope !== undefined && raw.dataScope !== null ? toNumber(raw.dataScope, 0) : undefined,
    status: toNumber(raw.status, 1),
    sort: toNumber(raw.sort, 0),
    permissions: Array.isArray(raw.permissions) ? raw.permissions : [],
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? '',
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
  }
}

function toCreatePayload(data: Partial<SysRole>) {
  return {
    roleCode: data.roleCode ?? '',
    roleName: data.roleName ?? '',
    roleDescription: data.roleDescription ?? '',
    roleType: toNumber(data.roleType, 0),
    dataScope: toNumber(data.dataScope, 0),
    sort: toNumber(data.sort, 0),
  }
}

function toUpdatePayload(id: string, data: Partial<SysRole>) {
  return {
    roleName: data.roleName ?? '',
    roleDescription: data.roleDescription ?? '',
    dataScope: toNumber(data.dataScope, 0),
    status: toNumber(data.status, 1),
    sort: toNumber(data.sort, 0),
    basicId: toId(id),
  }
}

// -------- API --------

export const roleApi = {
  page: (params: Record<string, any>) =>
    api.page(params, {
      keywordFields: ['RoleName', 'RoleCode', 'RoleDescription'],
      filterFieldMap: { status: 'Status' },
    }),

  list: async () => {
    const data = await api.request.post<any>(
      `${api.baseUrl}Page`,
      buildPageRequest({ page: 1, pageSize: 9999 }, { disablePaging: true }),
    )
    return normalizePageResult(data, normalizeRole).items
  },

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizeRole),

  create: (data: Partial<SysRole>) =>
    api.create(toCreatePayload(data)),

  update: (id: string, data: Partial<SysRole>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.deletePath(id),

  assignPermissions: (id: string, permissions: string[]) =>
    api.request.post(`${api.baseUrl}AssignPermissions`, {
      roleId: toId(id),
      permissionIds: permissions.map(item => toId(item)).filter(item => item.length > 0),
    }),
}

export async function getRolePageApi(params: RolePageQuery) {
  const data = await api.request.post<any>(
    `${api.baseUrl}Page`,
    buildPageRequest(params as Record<string, any>, {
      keywordFields: ['RoleName', 'RoleCode', 'RoleDescription'],
      filterFieldMap: { status: 'Status' },
    }),
  )
  return normalizePageResult(data, normalizeRole)
}

export const getRoleListApi = roleApi.list
export const getRoleDetailApi = roleApi.detail
export const createRoleApi = roleApi.create
export const updateRoleApi = roleApi.update
export const deleteRoleApi = roleApi.delete
export const assignRolePermissionsApi = roleApi.assignPermissions

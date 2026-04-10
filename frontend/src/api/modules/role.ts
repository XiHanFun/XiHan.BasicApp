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

const ROLE_TYPE_MAP: Record<string, number> = {
  System: 0,
  Business: 1,
  Custom: 2,
}

const DATA_SCOPE_MAP: Record<string, number> = {
  All: 0,
  DepartmentAndChildren: 1,
  DepartmentOnly: 2,
  SelfOnly: 3,
  Custom: 99,
}

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

function normalizeRole(raw: Record<string, any>): SysRole {
  return {
    basicId: toId(raw.basicId ?? raw.BasicId),
    roleName: raw.roleName ?? raw.RoleName ?? raw.name ?? '',
    roleCode: raw.roleCode ?? raw.RoleCode ?? raw.code ?? '',
    roleDescription: raw.roleDescription ?? raw.RoleDescription ?? raw.description ?? '',
    roleType: resolveEnum(raw.roleType ?? raw.RoleType, ROLE_TYPE_MAP, 0),
    dataScope: resolveEnum(raw.dataScope ?? raw.DataScope, DATA_SCOPE_MAP, 0),
    status: resolveEnum(raw.status ?? raw.Status, STATUS_MAP, 1),
    sort: toNumber(raw.sort ?? raw.Sort, 0),
    permissions: Array.isArray(raw.permissions ?? raw.Permissions) ? (raw.permissions ?? raw.Permissions) : [],
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? raw.createdTimeUtc ?? raw.CreatedTime ?? '',
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? raw.modifiedTime ?? raw.ModifiedTime ?? undefined,
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

const PAGE_OPTIONS = {
  keywordFields: ['RoleName', 'RoleCode', 'RoleDescription'],
  filterFieldMap: { status: 'Status' },
}

async function queryRolePage(params: Record<string, any>) {
  const data = await api.request.post<any>(
    `${api.baseUrl}Page`,
    buildPageRequest(params, PAGE_OPTIONS),
  )
  return normalizePageResult(data, normalizeRole)
}

// -------- API --------

export const roleApi = {
  page: (params: Record<string, any>) => queryRolePage(params),

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

  assignPermissions: (roleId: string, permissionIds: string[]) =>
    api.request.post(`${api.baseUrl}AssignPermissions`, {
      roleId: toId(roleId),
      permissionIds: permissionIds.map(item => toId(item)).filter(Boolean),
    }),

  assignMenus: (roleId: string, menuIds: string[]) =>
    api.request.post(`${api.baseUrl}AssignMenus`, {
      roleId: toId(roleId),
      menuIds: menuIds.map(item => toId(item)).filter(Boolean),
    }),

  assignDataScope: (roleId: string, departmentIds: string[]) =>
    api.request.post(`${api.baseUrl}AssignDataScope`, {
      roleId: toId(roleId),
      departmentIds: departmentIds.map(item => toId(item)).filter(Boolean),
    }),

  assignInheritance: (roleId: string, parentRoleIds: string[]) =>
    api.request.post(`${api.baseUrl}AssignInheritance`, {
      roleId: toId(roleId),
      parentRoleIds: parentRoleIds.map(item => toId(item)).filter(Boolean),
    }),

  /** 查询角色已分配的权限 ID 列表 */
  getRolePermissions: async (roleId: string): Promise<string[]> => {
    const id = toId(roleId)
    const data = await api.request.get<Array<Record<string, unknown>>>(
      `${api.baseUrl}RolePermissions/${id}/0`,
    )
    return Array.isArray(data)
      ? data.map(item => toId(item.permissionId)).filter(Boolean)
      : []
  },

  /** 查询角色已分配的菜单 ID 列表 */
  getRoleMenus: async (roleId: string): Promise<string[]> => {
    const id = toId(roleId)
    const data = await api.request.get<Array<Record<string, unknown>>>(
      `${api.baseUrl}RoleMenus/${id}/0`,
    )
    return Array.isArray(data)
      ? data.map(item => toId(item.menuId)).filter(Boolean)
      : []
  },

  /** 查询角色自定义数据范围的部门 ID 列表 */
  getRoleDataScopeDeptIds: async (roleId: string): Promise<string[]> => {
    const id = toId(roleId)
    const data = await api.request.get<Array<string | number>>(
      `${api.baseUrl}RoleDataScopeDepartmentIds/${id}/0`,
    )
    return Array.isArray(data) ? data.map(item => toId(item)).filter(Boolean) : []
  },

  /** 查询角色直接父角色 ID 列表 */
  getRoleParentRoleIds: async (roleId: string): Promise<string[]> => {
    const id = toId(roleId)
    const data = await api.request.get<Array<string | number>>(
      `${api.baseUrl}RoleParentRoleIds/${id}/0`,
    )
    return Array.isArray(data) ? data.map(item => toId(item)).filter(Boolean) : []
  },
}

export async function getRolePageApi(params: RolePageQuery) {
  return queryRolePage(params as Record<string, any>)
}

export const getRoleListApi = roleApi.list
export const getRoleDetailApi = roleApi.detail
export const createRoleApi = roleApi.create
export const updateRoleApi = roleApi.update
export const deleteRoleApi = roleApi.delete
export const assignRolePermissionsApi = roleApi.assignPermissions
export const assignRoleMenusApi = roleApi.assignMenus
export const assignRoleDataScopeApi = roleApi.assignDataScope
export const assignRoleInheritanceApi = roleApi.assignInheritance
export const getRolePermissionIdsApi = roleApi.getRolePermissions
export const getRoleMenuIdsApi = roleApi.getRoleMenus
export const getRoleDataScopeDeptIdsApi = roleApi.getRoleDataScopeDeptIds
export const getRoleParentRoleIdsApi = roleApi.getRoleParentRoleIds

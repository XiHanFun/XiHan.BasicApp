import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { buildPageRequest, normalizePageResult, toId, toNumber, unwrapPayload } from '../helpers'

const api = useBaseApi('User')

// -------- 类型 --------

export interface SysUser {
  basicId: string
  userName: string
  nickName: string
  realName?: string
  avatar?: string
  email?: string
  phone?: string
  gender: number
  status: number
  lastLoginTime?: string
  lastLoginIp?: string
  roles: string[]
  deptId?: string
  createTime: string
  updateTime?: string
  remark?: string
}

export interface UserPageQuery extends PageQuery {
  status?: number
  roleId?: string
}

// -------- 内部 --------

const GENDER_MAP: Record<string, number> = {
  Unknown: 0,
  Male: 1,
  Female: 2,
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

function normalizeRoles(rawRoleIds: unknown): string[] {
  return Array.isArray(rawRoleIds) ? rawRoleIds.map(item => toId(item)).filter(Boolean) : []
}

function normalizeUser(raw: Record<string, any>): SysUser {
  const rawRoleIds = raw.roles ?? raw.Roles ?? raw.roleIds ?? raw.RoleIds
  return {
    basicId: toId(raw.basicId ?? raw.BasicId),
    userName: raw.userName ?? raw.UserName ?? '',
    nickName: raw.nickName ?? raw.NickName ?? '',
    realName: raw.realName ?? raw.RealName ?? undefined,
    avatar: raw.avatar ?? raw.Avatar ?? undefined,
    email: raw.email ?? raw.Email ?? undefined,
    phone: raw.phone ?? raw.Phone ?? undefined,
    gender: resolveEnum(raw.gender ?? raw.Gender, GENDER_MAP, 0),
    status: resolveEnum(raw.status ?? raw.Status, STATUS_MAP, 1),
    lastLoginTime: raw.lastLoginTime ?? raw.LastLoginTime ?? undefined,
    lastLoginIp: raw.lastLoginIp ?? raw.LastLoginIp ?? undefined,
    roles: normalizeRoles(rawRoleIds),
    deptId: toId(raw.deptId ?? raw.DeptId ?? raw.mainDepartmentId ?? raw.MainDepartmentId) || undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? raw.CreatedTime ?? '',
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? raw.modifiedTime ?? raw.ModifiedTime ?? undefined,
    remark: raw.remark ?? raw.Remark ?? undefined,
  }
}

function normalizeUserPayload(raw: unknown): SysUser {
  const payload = unwrapPayload<Record<string, any>>(raw)
  return normalizeUser(payload && typeof payload === 'object' ? payload : {})
}

function toCreatePayload(data: Partial<SysUser & { password?: string }>) {
  return {
    userName: (data.userName ?? '').trim(),
    password: data.password ?? '',
    realName: (data.realName ?? data.nickName ?? '').trim(),
    nickName: (data.nickName ?? '').trim(),
    email: (data.email ?? '').trim(),
    phone: (data.phone ?? '').trim(),
    gender: toNumber(data.gender, 0),
  }
}

function toUpdatePayload(id: string, data: Partial<SysUser>) {
  return {
    realName: (data.realName ?? data.nickName ?? '').trim(),
    nickName: (data.nickName ?? '').trim(),
    email: (data.email ?? '').trim(),
    phone: (data.phone ?? '').trim(),
    gender: toNumber(data.gender, 0),
    status: toNumber(data.status, 1),
    avatar: (data.avatar ?? '').trim(),
    remark: (data.remark ?? '').trim(),
    basicId: toId(id),
  }
}

const PAGE_OPTIONS = {
  keywordFields: ['UserName', 'NickName', 'Email', 'Phone'],
  filterFieldMap: { status: 'Status', roleId: 'RoleId' },
}

async function queryUserPage(params: Record<string, any>) {
  const data = await api.request.post<any>(
    `${api.baseUrl}Page`,
    buildPageRequest(params, PAGE_OPTIONS),
  )
  return normalizePageResult(data, normalizeUser)
}

// -------- API --------

export const userApi = {
  page: (params: Record<string, any>) => queryUserPage(params),

  detail: (id: string) => api.detail(id).then(normalizeUserPayload),

  create: (data: Partial<SysUser & { password?: string }>) =>
    api.create(toCreatePayload(data)).then(normalizeUserPayload),

  update: (id: string, data: Partial<SysUser>) =>
    api.update(toUpdatePayload(id, data)).then(normalizeUserPayload),

  delete: (id: string) => api.deletePath(id),

  batchDelete: (ids: string[]) => Promise.all(ids.map(id => api.deletePath(id))),

  changeStatus: (id: string, status: number) =>
    api.request.post(`${api.baseUrl}ChangeStatus`, {
      userId: toId(id),
      status: toNumber(status, 1),
    }),

  resetPassword: (id: string, password: string) =>
    api.request.post(`${api.baseUrl}ResetPassword`, {
      userId: toId(id),
      newPassword: password,
    }),

  /** 查询用户已分配角色 ID 列表 */
  getUserRoles: async (userId: string): Promise<string[]> => {
    const id = toId(userId)
    const data = await api.request.get<Array<Record<string, unknown>>>(
      `${api.baseUrl}UserRoles/${id}/0`,
    )
    return Array.isArray(data)
      ? data.map(item => toId(item.roleId ?? item.RoleId)).filter(Boolean)
      : []
  },

  /** 分配用户角色（全量替换） */
  assignRoles: (userId: string, roleIds: string[]) =>
    api.request.post(`${api.baseUrl}AssignRoles`, {
      userId: toId(userId),
      roleIds: roleIds.map(item => toId(item)).filter(Boolean),
    }),

  /** 查询用户直授权限 ID 列表 */
  getUserPermissions: async (userId: string): Promise<string[]> => {
    const id = toId(userId)
    const data = await api.request.get<Array<Record<string, unknown>>>(
      `${api.baseUrl}UserPermissions/${id}/0`,
    )
    return Array.isArray(data)
      ? data.map(item => toId(item.permissionId ?? item.PermissionId)).filter(Boolean)
      : []
  },

  /** 分配用户直授权限（全量替换） */
  assignPermissions: (userId: string, permissionIds: string[]) =>
    api.request.post(`${api.baseUrl}AssignPermissions`, {
      userId: toId(userId),
      permissionIds: permissionIds.map(item => toId(item)).filter(Boolean),
    }),
}

export function getUserPageApi(params: UserPageQuery) {
  return queryUserPage(params as Record<string, any>)
}

export const getUserDetailApi = userApi.detail
export const createUserApi = userApi.create
export const updateUserApi = userApi.update
export const deleteUserApi = userApi.delete
export const batchDeleteUserApi = userApi.batchDelete
export const updateUserStatusApi = userApi.changeStatus
export const resetUserPasswordApi = userApi.resetPassword
export const getUserRoleIdsApi = userApi.getUserRoles
export const assignUserRolesApi = userApi.assignRoles
export const getUserPermissionIdsApi = userApi.getUserPermissions
export const assignUserPermissionsApi = userApi.assignPermissions

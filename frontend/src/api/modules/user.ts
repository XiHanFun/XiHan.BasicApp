import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { buildPageRequest, normalizePageResult, toId, toNumber, unwrapPayload } from '../helpers'

const api = useBaseApi('User')

// -------- 类型 --------

export interface SysUser {
  basicId: string
  tenantId?: string
  userName: string
  nickName: string
  realName?: string
  avatar?: string
  email?: string
  phone?: string
  gender: number
  birthday?: string
  status: number
  lastLoginTime?: string
  lastLoginIp?: string
  timeZone?: string
  language?: string
  country?: string
  isSystemAccount?: boolean
  roles: string[]
  permissionIds?: string[]
  departmentIds?: string[]
  mainDepartmentId?: string
  accessibleTenantIds?: string[]
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
  const rawPermissionIds = raw.permissionIds ?? raw.PermissionIds
  const rawDepartmentIds = raw.departmentIds ?? raw.DepartmentIds
  const rawAccessibleTenantIds = raw.accessibleTenantIds ?? raw.AccessibleTenantIds
  const mainDepartmentId = toId(raw.mainDepartmentId ?? raw.MainDepartmentId)
  return {
    basicId: toId(raw.basicId ?? raw.BasicId),
    tenantId: toId(raw.tenantId ?? raw.TenantId) || undefined,
    userName: raw.userName ?? raw.UserName ?? '',
    nickName: raw.nickName ?? raw.NickName ?? '',
    realName: raw.realName ?? raw.RealName ?? undefined,
    avatar: raw.avatar ?? raw.Avatar ?? undefined,
    email: raw.email ?? raw.Email ?? undefined,
    phone: raw.phone ?? raw.Phone ?? undefined,
    gender: resolveEnum(raw.gender ?? raw.Gender, GENDER_MAP, 0),
    birthday: raw.birthday ?? raw.Birthday ?? undefined,
    status: resolveEnum(raw.status ?? raw.Status, STATUS_MAP, 1),
    lastLoginTime: raw.lastLoginTime ?? raw.LastLoginTime ?? undefined,
    lastLoginIp: raw.lastLoginIp ?? raw.LastLoginIp ?? undefined,
    timeZone: raw.timeZone ?? raw.TimeZone ?? undefined,
    language: raw.language ?? raw.Language ?? undefined,
    country: raw.country ?? raw.Country ?? undefined,
    isSystemAccount: Boolean(raw.isSystemAccount ?? raw.IsSystemAccount),
    roles: normalizeRoles(rawRoleIds),
    permissionIds: normalizeRoles(rawPermissionIds),
    departmentIds: normalizeRoles(rawDepartmentIds),
    mainDepartmentId: mainDepartmentId || undefined,
    accessibleTenantIds: normalizeRoles(rawAccessibleTenantIds),
    deptId: toId(raw.deptId ?? raw.DeptId ?? mainDepartmentId) || undefined,
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
    tenantId: data.tenantId ? toId(data.tenantId) : null,
    userName: (data.userName ?? '').trim(),
    password: data.password ?? '',
    realName: (data.realName ?? data.nickName ?? '').trim(),
    nickName: (data.nickName ?? '').trim(),
    email: (data.email ?? '').trim(),
    phone: (data.phone ?? '').trim(),
    gender: toNumber(data.gender, 0),
    birthday: data.birthday ?? null,
    timeZone: (data.timeZone ?? '').trim(),
    language: (data.language ?? '').trim(),
    country: (data.country ?? '').trim(),
    roleIds: (data.roles ?? []).map(item => toId(item)).filter(Boolean),
    permissionIds: (data.permissionIds ?? []).map(item => toId(item)).filter(Boolean),
    departmentIds: (data.departmentIds ?? []).map(item => toId(item)).filter(Boolean),
    mainDepartmentId: data.mainDepartmentId ? toId(data.mainDepartmentId) : null,
    accessibleTenantIds: (data.accessibleTenantIds ?? []).map(item => toId(item)).filter(Boolean),
  }
}

function toUpdatePayload(id: string, data: Partial<SysUser>) {
  return {
    realName: (data.realName ?? data.nickName ?? '').trim(),
    nickName: (data.nickName ?? '').trim(),
    email: (data.email ?? '').trim(),
    phone: (data.phone ?? '').trim(),
    gender: toNumber(data.gender, 0),
    birthday: data.birthday ?? null,
    status: toNumber(data.status, 1),
    avatar: (data.avatar ?? '').trim(),
    timeZone: (data.timeZone ?? '').trim(),
    language: (data.language ?? '').trim(),
    country: (data.country ?? '').trim(),
    roleIds: (data.roles ?? []).map(item => toId(item)).filter(Boolean),
    permissionIds: (data.permissionIds ?? []).map(item => toId(item)).filter(Boolean),
    departmentIds: (data.departmentIds ?? []).map(item => toId(item)).filter(Boolean),
    mainDepartmentId: data.mainDepartmentId ? toId(data.mainDepartmentId) : null,
    accessibleTenantIds: (data.accessibleTenantIds ?? []).map(item => toId(item)).filter(Boolean),
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

  /** 查询用户部门关系（部门ID列表 + 主部门ID） */
  getUserDepartments: async (userId: string): Promise<{ departmentIds: string[], mainDepartmentId?: string }> => {
    const id = toId(userId)
    const data = await api.request.get<Array<Record<string, unknown>>>(
      `${api.baseUrl}UserDepartments/${id}/0`,
    )
    if (!Array.isArray(data)) {
      return { departmentIds: [], mainDepartmentId: undefined }
    }
    const relationList = data
      .map(item => ({
        departmentId: toId(item.departmentId ?? item.DepartmentId),
        isMain: Boolean(item.isMain ?? item.IsMain),
      }))
      .filter(item => item.departmentId)
    return {
      departmentIds: relationList.map(item => item.departmentId),
      mainDepartmentId: relationList.find(item => item.isMain)?.departmentId,
    }
  },

  /** 分配用户部门（全量替换） */
  assignDepartments: (userId: string, departmentIds: string[], mainDepartmentId?: string) =>
    api.request.post(`${api.baseUrl}AssignDepartments`, {
      userId: toId(userId),
      departmentIds: departmentIds.map(item => toId(item)).filter(Boolean),
      mainDepartmentId: mainDepartmentId ? toId(mainDepartmentId) : null,
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
export const getUserDepartmentIdsApi = userApi.getUserDepartments
export const assignUserDepartmentsApi = userApi.assignDepartments

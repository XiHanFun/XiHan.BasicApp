import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

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

function toCreatePayload(data: Partial<SysUser & { password?: string }>) {
  return {
    userName: (data.userName ?? '').trim(),
    password: data.password ?? '',
    realName: data.realName ?? data.nickName ?? '',
    nickName: data.nickName ?? '',
    email: data.email ?? '',
    phone: data.phone ?? '',
    gender: toNumber(data.gender, 0),
  }
}

function toUpdatePayload(id: string, data: Partial<SysUser>) {
  return {
    realName: data.realName ?? data.nickName ?? '',
    nickName: data.nickName ?? '',
    email: data.email ?? '',
    phone: data.phone ?? '',
    gender: toNumber(data.gender, 0),
    status: toNumber(data.status, 1),
    avatar: data.avatar ?? '',
    remark: data.remark ?? '',
    basicId: toId(id),
  }
}

// -------- API --------

export const userApi = {
  page: (params: Record<string, any>) =>
    api.page(params, {
      keywordFields: ['UserName', 'NickName', 'Email', 'Phone'],
      filterFieldMap: { status: 'Status', roleId: 'RoleId' },
    }),

  detail: (id: string) => api.detail(id),

  create: (data: Partial<SysUser & { password?: string }>) =>
    api.create(toCreatePayload(data)),

  update: (id: string, data: Partial<SysUser>) =>
    api.update(toUpdatePayload(id, data)),

  delete: (id: string) => api.deletePath(id),

  batchDelete: (ids: string[]) =>
    Promise.all(ids.map(id => api.deletePath(id))),

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
}

export function getUserPageApi(params: UserPageQuery) {
  return userApi.page(params as Record<string, any>)
}

export const getUserDetailApi = userApi.detail
export const createUserApi = userApi.create
export const updateUserApi = userApi.update
export const deleteUserApi = userApi.delete
export const batchDeleteUserApi = userApi.batchDelete
export const updateUserStatusApi = userApi.changeStatus
export const resetUserPasswordApi = userApi.resetPassword

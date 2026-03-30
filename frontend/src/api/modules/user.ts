import type { PageResult, SysUser, UserPageQuery } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const USER_API = '/api/User'

function normalizeUser(raw: Record<string, any>): SysUser {
  return {
    basicId: toId(raw.basicId),
    username: raw.username ?? raw.userName ?? '',
    nickname: raw.nickname ?? raw.nickName ?? raw.realName ?? '',
    avatar: raw.avatar ?? undefined,
    email: raw.email ?? undefined,
    phone: raw.phone ?? undefined,
    gender: toNumber(raw.gender, 0),
    status: toNumber(raw.status, 1),
    roles: Array.isArray(raw.roles) ? raw.roles : [],
    deptId: raw.deptId ? toId(raw.deptId) : undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? '',
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toUserCreatePayload(data: Partial<SysUser & { password?: string }>) {
  return {
    userName: (data.username ?? '').trim(),
    password: data.password ?? '',
    realName: data.nickname ?? '',
    nickName: data.nickname ?? '',
    email: data.email ?? '',
    phone: data.phone ?? '',
    gender: toNumber(data.gender, 0),
  }
}

function toUserUpdatePayload(id: string, data: Partial<SysUser>) {
  return {
    realName: data.nickname ?? '',
    nickName: data.nickname ?? '',
    email: data.email ?? '',
    phone: data.phone ?? '',
    gender: toNumber(data.gender, 0),
    status: toNumber(data.status, 1),
    avatar: data.avatar ?? '',
    remark: data.remark ?? '',
    basicId: toId(id),
  }
}

export async function getUserPageApi(params: UserPageQuery): Promise<PageResult<SysUser>> {
  const data = await requestClient.post<any>(
    `${USER_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['UserName', 'NickName', 'Email', 'Phone'],
      filterFieldMap: {
        status: 'Status',
        roleId: 'RoleId',
      },
    }),
  )
  return normalizePageResult(data, normalizeUser)
}

export function getUserDetailApi(id: string) {
  return requestClient
    .get<any>(`${USER_API}/ById`, { params: { id } })
    .then(raw => normalizeUser(raw))
}

export function createUserApi(data: Partial<SysUser & { password?: string }>) {
  return requestClient.post<void>(`${USER_API}/Create`, toUserCreatePayload(data))
}

export function updateUserApi(id: string, data: Partial<SysUser>) {
  return requestClient.put<void>(`${USER_API}/Update`, toUserUpdatePayload(id, data))
}

export function deleteUserApi(id: string) {
  return requestClient.delete<void>(`${USER_API}/Delete/${id}`)
}

export async function batchDeleteUserApi(ids: string[]) {
  await Promise.all(ids.map(id => deleteUserApi(id)))
}

export function updateUserStatusApi(id: string, status: number) {
  return requestClient.post<void>(`${USER_API}/ChangeStatus`, {
    userId: toId(id),
    status: toNumber(status, 1),
  })
}

export function resetUserPasswordApi(id: string, password: string) {
  return requestClient.post<void>(`${USER_API}/ResetPassword`, {
    userId: toId(id),
    newPassword: password,
  })
}

import type { PageResult, SysUser, UserPageQuery } from '~/types'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'
import requestClient from '../request'

export async function getUserPageApi(params: UserPageQuery): Promise<PageResult<SysUser>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.users, { params })
  return normalizePageResult<SysUser>(data)
}

export function getUserDetailApi(id: string) {
  return requestClient.get<SysUser>(`${API_CONTRACT.system.users}/${id}`)
}

export function createUserApi(data: Partial<SysUser>) {
  return requestClient.post<void>(API_CONTRACT.system.users, data)
}

export function updateUserApi(id: string, data: Partial<SysUser>) {
  return requestClient.put<void>(`${API_CONTRACT.system.users}/${id}`, data)
}

export function deleteUserApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.users}/${id}`)
}

export function batchDeleteUserApi(ids: string[]) {
  return requestClient.delete<void>(`${API_CONTRACT.system.users}/batch`, { data: { ids } })
}

export function updateUserStatusApi(id: string, status: number) {
  return requestClient.patch<void>(`${API_CONTRACT.system.users}/${id}/status`, { status })
}

export function resetUserPasswordApi(id: string, password: string) {
  return requestClient.patch<void>(`${API_CONTRACT.system.users}/${id}/password`, { password })
}

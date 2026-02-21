import type { SysRole, PageResult, RolePageQuery } from '~/types'
import requestClient from '../request'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'

export async function getRolePageApi(params: RolePageQuery): Promise<PageResult<SysRole>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.roles, { params })
  return normalizePageResult<SysRole>(data)
}

export function getRoleListApi() {
  return requestClient.get<SysRole[]>(`${API_CONTRACT.system.roles}/list`)
}

export function getRoleDetailApi(id: string) {
  return requestClient.get<SysRole>(`${API_CONTRACT.system.roles}/${id}`)
}

export function createRoleApi(data: Partial<SysRole>) {
  return requestClient.post<void>(API_CONTRACT.system.roles, data)
}

export function updateRoleApi(id: string, data: Partial<SysRole>) {
  return requestClient.put<void>(`${API_CONTRACT.system.roles}/${id}`, data)
}

export function deleteRoleApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.roles}/${id}`)
}

export function assignRolePermissionsApi(id: string, permissions: string[]) {
  return requestClient.put<void>(`${API_CONTRACT.system.roles}/${id}/permissions`, { permissions })
}

import type { SysMenu, MenuRoute } from '~/types'
import requestClient from '../request'
import { API_CONTRACT } from '../contract'

export function getMenuTreeApi() {
  return requestClient.get<SysMenu[]>(`${API_CONTRACT.system.menus}/tree`)
}

export function getMenuListApi() {
  return requestClient.get<SysMenu[]>(API_CONTRACT.system.menus)
}

export function getMenuDetailApi(id: string) {
  return requestClient.get<SysMenu>(`${API_CONTRACT.system.menus}/${id}`)
}

export function createMenuApi(data: Partial<SysMenu>) {
  return requestClient.post<void>(API_CONTRACT.system.menus, data)
}

export function updateMenuApi(id: string, data: Partial<SysMenu>) {
  return requestClient.put<void>(`${API_CONTRACT.system.menus}/${id}`, data)
}

export function deleteMenuApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.menus}/${id}`)
}

export function getUserMenuRoutesApi() {
  return requestClient.get<MenuRoute[]>(API_CONTRACT.auth.userMenus)
}

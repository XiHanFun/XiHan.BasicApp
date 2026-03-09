import type { MenuRoute, SysMenu } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const MENU_API = '/api/Menu'

function normalizeMenu(raw: Record<string, any>): SysMenu {
  const visible = raw.isVisible === undefined ? !Boolean(raw.hidden) : Boolean(raw.isVisible)
  return {
    basicId: toId(raw.basicId),
    parentId: raw.parentId === null || raw.parentId === undefined ? undefined : toId(raw.parentId),
    name: raw.name ?? raw.menuName ?? '',
    path: raw.path ?? '',
    component: raw.component ?? undefined,
    icon: raw.icon ?? undefined,
    type: toNumber(raw.type ?? raw.menuType, 1),
    permission: raw.permission ?? raw.menuCode ?? '',
    sort: toNumber(raw.sort, 0),
    status: toNumber(raw.status, 1),
    hidden: !visible,
    children: Array.isArray(raw.children) ? raw.children.map(item => normalizeMenu(item)) : undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? '',
  }
}

function normalizeMenuRoute(raw: Record<string, any>): MenuRoute {
  const menu = normalizeMenu(raw)
  return {
    basicId: menu.basicId,
    path: menu.path || `/${menu.basicId}`,
    name: menu.name || `menu-${menu.basicId}`,
    component: menu.component,
    meta: {
      title: menu.name,
      icon: menu.icon,
      hidden: menu.hidden,
      order: menu.sort,
      permissions: menu.permission ? [menu.permission] : [],
    },
    children: Array.isArray(menu.children)
      ? menu.children.map(item => normalizeMenuRoute(item as unknown as Record<string, any>))
      : undefined,
  }
}

function resolveMenuCode(data: Partial<SysMenu>) {
  const candidate = data.permission || data.path || data.name
  if (candidate && String(candidate).trim().length > 0) {
    return String(candidate).trim()
  }
  return `menu_${Date.now()}`
}

function toMenuCreatePayload(data: Partial<SysMenu>) {
  return {
    resourceId: data.permission ? toNumber(data.permission, 0) || undefined : undefined,
    parentId: data.parentId ? toNumber(data.parentId, 0) : null,
    menuName: data.name ?? '',
    menuCode: resolveMenuCode(data),
    menuType: toNumber(data.type, 1),
    path: data.path ?? '',
    component: data.component ?? '',
    icon: data.icon ?? '',
    isVisible: !Boolean(data.hidden),
    sort: toNumber(data.sort, 0),
    remark: '',
  }
}

function toMenuUpdatePayload(id: string, data: Partial<SysMenu>) {
  return {
    ...toMenuCreatePayload(data),
    status: toNumber(data.status, 1),
    basicId: toNumber(id, 0),
  }
}

export function getMenuTreeApi() {
  return getMenuListApi()
}

export async function getMenuListApi() {
  const data = await requestClient.post<any>(
    `${MENU_API}/Page`,
    buildPageRequest(
      { page: 1, pageSize: 9999 },
      {
        disablePaging: true,
      },
    ),
  )
  return normalizePageResult(data, normalizeMenu).items
}

export function getMenuDetailApi(id: string) {
  return requestClient
    .get<any>(`${MENU_API}/ById`, { params: { id } })
    .then(raw => normalizeMenu(raw))
}

export function createMenuApi(data: Partial<SysMenu>) {
  return requestClient.post<void>(`${MENU_API}/Create`, toMenuCreatePayload(data))
}

export function updateMenuApi(id: string, data: Partial<SysMenu>) {
  return requestClient.put<void>(`${MENU_API}/Update`, toMenuUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteMenuApi(id: string) {
  return requestClient.delete<void>(`${MENU_API}/Delete`, {
    params: { id },
  })
}

export async function getUserMenuRoutesApi() {
  const data = await requestClient.get<any[]>(`${MENU_API}/UserMenus`)
  return Array.isArray(data) ? data.map(item => normalizeMenuRoute(item)) : []
}

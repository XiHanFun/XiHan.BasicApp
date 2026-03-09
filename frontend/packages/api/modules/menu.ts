import type { MenuRoute, SysMenu } from '~/types'
import requestClient from '../request'

const MENU_API = '/api/Menu'

const MENU_TYPE_MAP: Record<string, number> = { Directory: 0, Menu: 1, Button: 2 }
const STATUS_MAP: Record<string, number> = { Yes: 1, No: 0 }

function resolveEnum(value: unknown, map: Record<string, number>, fallback: number): number {
  if (typeof value === 'number') {
    return value
  }
  if (typeof value === 'string') {
    return map[value] ?? (Number(value) || fallback)
  }
  return fallback
}

function normalizeMenu(raw: Record<string, any>): SysMenu {
  const children = Array.isArray(raw.children) && raw.children.length > 0
    ? raw.children.map((c: Record<string, any>) => normalizeMenu(c))
    : undefined
  return {
    basicId: String(raw.basicId),
    parentId: raw.parentId != null ? String(raw.parentId) : undefined,
    menuName: raw.menuName ?? '',
    menuCode: raw.menuCode ?? '',
    menuType: resolveEnum(raw.menuType, MENU_TYPE_MAP, 0),
    path: raw.path ?? undefined,
    component: raw.component ?? undefined,
    routeName: raw.routeName ?? undefined,
    redirect: raw.redirect ?? undefined,
    icon: raw.icon ?? undefined,
    title: raw.title ?? undefined,
    isExternal: Boolean(raw.isExternal),
    externalUrl: raw.externalUrl ?? undefined,
    isCache: Boolean(raw.isCache),
    isVisible: raw.isVisible !== false && raw.isVisible !== 'false',
    isAffix: Boolean(raw.isAffix),
    sort: Number(raw.sort ?? 0),
    status: resolveEnum(raw.status, STATUS_MAP, 1),
    remark: raw.remark ?? undefined,
    children,
    createTime: raw.createTime ?? raw.creationTime ?? '',
  }
}

function toMenuRoute(menu: SysMenu): MenuRoute {
  return {
    basicId: menu.basicId,
    path: menu.path || `/${menu.basicId}`,
    name: menu.routeName || menu.menuCode || `menu-${menu.basicId}`,
    component: menu.component ?? undefined,
    redirect: menu.redirect ?? undefined,
    meta: {
      title: menu.title || menu.menuName,
      icon: menu.icon,
      hidden: !menu.isVisible,
      order: menu.sort,
      permissions: menu.menuCode ? [menu.menuCode] : [],
    },
    children: Array.isArray(menu.children)
      ? menu.children.map(c => toMenuRoute(c))
      : undefined,
  }
}

const MENU_TYPE_NAMES = ['Directory', 'Menu', 'Button']
const STATUS_NAMES = ['No', 'Yes']

function toCreatePayload(data: Partial<SysMenu>) {
  return {
    parentId: data.parentId ? Number(data.parentId) : null,
    menuName: data.menuName ?? '',
    menuCode: data.menuCode ?? '',
    menuType: MENU_TYPE_NAMES[data.menuType ?? 1] ?? 'Menu',
    path: data.path ?? '',
    component: data.component ?? '',
    routeName: data.routeName ?? '',
    redirect: data.redirect ?? '',
    icon: data.icon ?? '',
    title: data.title || data.menuName || '',
    isExternal: data.isExternal ?? false,
    externalUrl: data.externalUrl ?? '',
    isCache: data.isCache ?? true,
    isVisible: data.isVisible !== false,
    isAffix: data.isAffix ?? false,
    sort: data.sort ?? 0,
    remark: data.remark ?? '',
  }
}

function toUpdatePayload(data: Partial<SysMenu>) {
  return {
    ...toCreatePayload(data),
    basicId: data.basicId ? Number(data.basicId) : 0,
    status: STATUS_NAMES[data.status ?? 1] ?? 'Yes',
  }
}

export function getMenuTreeApi() {
  return getMenuListApi()
}

export async function getMenuListApi(): Promise<SysMenu[]> {
  const data = await requestClient.get<any[]>(`${MENU_API}/List`)
  return Array.isArray(data) ? data.map(normalizeMenu) : []
}

export function getMenuDetailApi(id: string) {
  return requestClient
    .get<any>(`${MENU_API}/ById`, { params: { id } })
    .then(raw => normalizeMenu(raw))
}

export function createMenuApi(data: Partial<SysMenu>) {
  return requestClient.post<any>(`${MENU_API}/Create`, toCreatePayload(data))
}

export function updateMenuApi(_id: string, data: Partial<SysMenu>) {
  return requestClient.put<any>(`${MENU_API}/Update`, toUpdatePayload(data))
}

export function deleteMenuApi(id: string) {
  return requestClient.delete<boolean>(`${MENU_API}/Delete`, { params: { id } })
}

export async function getUserMenuRoutesApi() {
  const data = await requestClient.get<any[]>(`${MENU_API}/UserMenus`)
  if (!Array.isArray(data)) {
    return []
  }
  return data.map(raw => toMenuRoute(normalizeMenu(raw)))
}

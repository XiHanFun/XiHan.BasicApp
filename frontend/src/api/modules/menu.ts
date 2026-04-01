import type { MenuRoute } from '~/types'
import { useBaseApi } from '../base'
import { toId } from '../helpers'

const api = useBaseApi('Menu')

// -------- 类型 --------

export interface SysMenu {
  basicId: string
  parentId?: string
  menuName: string
  menuCode: string
  menuType: number
  path?: string
  component?: string
  routeName?: string
  redirect?: string
  icon?: string
  title?: string
  isExternal?: boolean
  externalUrl?: string
  isCache?: boolean
  isVisible: boolean
  isAffix?: boolean
  sort: number
  status: number
  remark?: string
  children?: SysMenu[]
  createTime?: string
}

// -------- 内部 --------

const MENU_TYPE_MAP: Record<string, number> = { Directory: 0, Menu: 1, Button: 2 }
const STATUS_MAP: Record<string, number> = { Yes: 1, No: 0 }
const MENU_TYPE_NAMES = ['Directory', 'Menu', 'Button']
const STATUS_NAMES = ['No', 'Yes']

function resolveEnum(value: unknown, map: Record<string, number>, fallback: number): number {
  if (typeof value === 'number')
    return value
  if (typeof value === 'string')
    return map[value] ?? (Number(value) || fallback)
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

function toCreatePayload(data: Partial<SysMenu>) {
  return {
    parentId: data.parentId ? toId(data.parentId) : null,
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
    basicId: data.basicId ? toId(data.basicId) : '',
    status: STATUS_NAMES[data.status ?? 1] ?? 'Yes',
  }
}

// -------- API --------

export const menuApi = {
  tree: async (): Promise<SysMenu[]> => {
    const data = await api.request.get<any[]>(`${api.baseUrl}List`)
    return Array.isArray(data) ? data.map(normalizeMenu) : []
  },

  list: async (): Promise<SysMenu[]> => {
    const data = await api.request.get<any[]>(`${api.baseUrl}List`)
    return Array.isArray(data) ? data.map(normalizeMenu) : []
  },

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizeMenu),

  create: (data: Partial<SysMenu>) =>
    api.request.post(`${api.baseUrl}Create`, toCreatePayload(data)),

  update: (data: Partial<SysMenu>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(data)),

  delete: (id: string) => api.delete(id),

  userMenuRoutes: async (): Promise<MenuRoute[]> => {
    const data = await api.request.get<any[]>(`${api.baseUrl}UserMenus`)
    if (!Array.isArray(data))
      return []
    return data.map(raw => toMenuRoute(normalizeMenu(raw)))
  },
}

export const getMenuTreeApi = menuApi.tree
export const getMenuListApi = menuApi.list
export const getMenuDetailApi = menuApi.detail
export const createMenuApi = menuApi.create
export const updateMenuApi = (_id: string, data: Partial<SysMenu>) => menuApi.update(data)
export const deleteMenuApi = menuApi.delete
export const getUserMenuRoutesApi = menuApi.userMenuRoutes

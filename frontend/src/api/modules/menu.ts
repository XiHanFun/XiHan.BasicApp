import type { MenuRoute } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber, unwrapPayload } from '../helpers'

const api = useBaseApi('Menu')

// -------- 类型 --------

export interface SysMenu {
  basicId: string
  permissionId?: string
  isGlobal?: boolean
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
  badge?: string
  badgeType?: string
  badgeDot?: boolean
  metadata?: string
  sort: number
  status: number
  remark?: string
  children?: SysMenu[]
  createTime?: string
  updateTime?: string
}

// -------- 内部 --------

const MENU_TYPE_MAP: Record<string, number> = { Directory: 0, Menu: 1, Button: 2 }
const STATUS_MAP: Record<string, number> = { Yes: 1, No: 0 }

function resolveEnum(value: unknown, map: Record<string, number>, fallback: number): number {
  if (value === undefined || value === null)
    return fallback
  if (typeof value === 'number')
    return value
  if (typeof value === 'string')
    return map[value] ?? toNumber(value, fallback)
  return fallback
}

function resolveBool(value: unknown, fallback: boolean): boolean {
  if (value === undefined || value === null)
    return fallback
  if (typeof value === 'boolean')
    return value
  if (typeof value === 'number')
    return value !== 0
  if (typeof value === 'string') {
    const normalized = value.trim().toLowerCase()
    if (normalized === 'true' || normalized === '1' || normalized === 'yes')
      return true
    if (normalized === 'false' || normalized === '0' || normalized === 'no')
      return false
  }
  return fallback
}

function normalizeMenu(raw: Record<string, any>): SysMenu {
  const rawChildren = raw.children ?? raw.Children
  const children = Array.isArray(rawChildren) && rawChildren.length > 0
    ? rawChildren.map((c: Record<string, any>) => normalizeMenu(c))
    : undefined
  return {
    basicId: toId(raw.basicId ?? raw.BasicId),
    permissionId: toId(raw.permissionId ?? raw.PermissionId) || undefined,
    isGlobal: resolveBool(raw.isGlobal ?? raw.IsGlobal, false),
    parentId: raw.parentId !== null && raw.parentId !== undefined
      ? toId(raw.parentId)
      : (raw.ParentId !== null && raw.ParentId !== undefined ? toId(raw.ParentId) : undefined),
    menuName: raw.menuName ?? raw.MenuName ?? '',
    menuCode: raw.menuCode ?? raw.MenuCode ?? '',
    menuType: resolveEnum(raw.menuType ?? raw.MenuType, MENU_TYPE_MAP, 0),
    path: raw.path ?? raw.Path ?? undefined,
    component: raw.component ?? raw.Component ?? undefined,
    routeName: raw.routeName ?? raw.RouteName ?? undefined,
    redirect: raw.redirect ?? raw.Redirect ?? undefined,
    icon: raw.icon ?? raw.Icon ?? undefined,
    title: raw.title ?? raw.Title ?? undefined,
    isExternal: resolveBool(raw.isExternal ?? raw.IsExternal, false),
    externalUrl: raw.externalUrl ?? raw.ExternalUrl ?? undefined,
    isCache: resolveBool(raw.isCache ?? raw.IsCache, false),
    isVisible: resolveBool(raw.isVisible ?? raw.IsVisible, true),
    isAffix: resolveBool(raw.isAffix ?? raw.IsAffix, false),
    badge: raw.badge ?? raw.Badge ?? undefined,
    badgeType: raw.badgeType ?? raw.BadgeType ?? undefined,
    badgeDot: resolveBool(raw.badgeDot ?? raw.BadgeDot, false),
    metadata: raw.metadata ?? raw.Metadata ?? undefined,
    sort: toNumber(raw.sort ?? raw.Sort, 0),
    status: resolveEnum(raw.status ?? raw.Status, STATUS_MAP, 1),
    remark: raw.remark ?? raw.Remark ?? undefined,
    children,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? raw.CreatedTime ?? '',
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? raw.modifiedTime ?? raw.ModifiedTime ?? undefined,
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
      badge: menu.badge || undefined,
      badgeType: menu.badgeType || undefined,
      dot: menu.badgeDot || undefined,
    },
    children: Array.isArray(menu.children)
      ? menu.children.map(c => toMenuRoute(c))
      : undefined,
  }
}

function toCreatePayload(data: Partial<SysMenu>) {
  return {
    permissionId: data.permissionId ? toId(data.permissionId) : null,
    isGlobal: data.isGlobal ?? false,
    parentId: data.parentId ? toId(data.parentId) : null,
    menuName: data.menuName ?? '',
    menuCode: data.menuCode ?? '',
    menuType: toNumber(data.menuType, 1),
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
    badge: data.badge ?? '',
    badgeType: data.badgeType ?? '',
    badgeDot: data.badgeDot ?? false,
    metadata: data.metadata ?? '',
    sort: data.sort ?? 0,
    remark: data.remark ?? '',
  }
}

function toUpdatePayload(data: Partial<SysMenu>) {
  return {
    ...toCreatePayload(data),
    basicId: data.basicId ? toId(data.basicId) : '',
    status: toNumber(data.status, 1),
  }
}

function normalizeMenuArray(raw: unknown): SysMenu[] {
  const payload = unwrapPayload<any>(raw)
  return Array.isArray(payload) ? payload.map(item => normalizeMenu(item)) : []
}

// -------- API --------

export const menuApi = {
  tree: async (): Promise<SysMenu[]> => {
    const data = await api.request.get<any>(`${api.baseUrl}List`)
    return normalizeMenuArray(data)
  },

  list: async (): Promise<SysMenu[]> => {
    const data = await api.request.get<any>(`${api.baseUrl}List`)
    return normalizeMenuArray(data)
  },

  detail: (id: string) =>
    api.request
      .get<any>(`${api.baseUrl}ById`, { params: { id } })
      .then(raw => normalizeMenu((unwrapPayload<any>(raw) ?? {}) as Record<string, any>)),

  create: (data: Partial<SysMenu>) =>
    api.request.post(`${api.baseUrl}Create`, toCreatePayload(data)),

  update: (data: Partial<SysMenu>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(data)),

  delete: (id: string) => api.delete(id),

  roleMenus: async (roleId: string, tenantId?: string): Promise<SysMenu[]> => {
    const id = toId(roleId)
    const resolvedTenantId = tenantId ? toId(tenantId) : '0'
    const data = await api.request.get<any>(`${api.baseUrl}RoleMenus/${id}/${resolvedTenantId}`)
    return normalizeMenuArray(data)
  },

  userMenuRoutes: async (): Promise<MenuRoute[]> => {
    const data = await api.request.get<any>(`${api.baseUrl}UserMenus`)
    const menus = normalizeMenuArray(data)
    if (!Array.isArray(menus) || menus.length === 0)
      return []
    return menus.map(menu => toMenuRoute(menu))
  },
}

export const getMenuTreeApi = menuApi.tree
export const getMenuListApi = menuApi.list
export const getMenuDetailApi = menuApi.detail
export const createMenuApi = menuApi.create
export function updateMenuApi(id: string, data: Partial<SysMenu>) {
  return menuApi.update({ ...data, basicId: data.basicId ?? id })
}
export const deleteMenuApi = menuApi.delete
export const getRoleMenusApi = menuApi.roleMenus
export const getUserMenuRoutesApi = menuApi.userMenuRoutes

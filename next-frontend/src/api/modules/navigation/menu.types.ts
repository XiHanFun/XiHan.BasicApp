import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export { EnableStatus } from '../shared'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum MenuType {
  Directory = 'Directory',
  Menu = 'Menu',
  Button = 'Button',
}

export interface MenuPageQueryDto extends PageRequest {
  isGlobal?: boolean | null
  keyword?: string | null
  menuType?: MenuType | null
  parentId?: ApiId | null
  permissionId?: ApiId | null
  status?: EnableStatus | null
}

/** 菜单列表查询（不分页，返回全部匹配项） */
export interface MenuListQueryDto {
  isExternal?: boolean | null
  isGlobal?: boolean | null
  isVisible?: boolean | null
  keyword?: string | null
  menuType?: MenuType | null
  parentId?: ApiId | null
  permissionId?: ApiId | null
  status?: EnableStatus | null
}

export interface MenuListItemDto extends BasicDto {
  badge?: string | null
  badgeDot: boolean
  badgeType?: string | null
  component?: string | null
  createdTime: DateTimeString
  icon?: string | null
  isAffix: boolean
  isCache: boolean
  isExternal: boolean
  isGlobal: boolean
  isVisible: boolean
  menuCode: string
  menuName: string
  menuType: MenuType
  modifiedTime?: DateTimeString | null
  parentId?: ApiId | null
  path: string
  permissionId?: ApiId | null
  redirect?: string | null
  routeName?: string | null
  sort: number
  status: EnableStatus
  title?: string | null
  /** 国际化键（如 menu.identity_user，有值时前端按键翻译菜单标题） */
  i18nKey?: string | null
}

export interface MenuDetailDto extends MenuListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  externalUrl?: string | null
  metadata?: string | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  permissionCode?: string | null
  permissionName?: string | null
  remark?: string | null
}

export interface MenuCreateDto {
  badge?: string | null
  badgeDot: boolean
  badgeType?: string | null
  component?: string | null
  externalUrl?: string | null
  icon?: string | null
  isAffix: boolean
  isCache: boolean
  isExternal: boolean
  isVisible: boolean
  menuCode: string
  menuName: string
  menuType: MenuType
  metadata?: string | null
  parentId?: ApiId | null
  path: string
  permissionId?: ApiId | null
  redirect?: string | null
  remark?: string | null
  routeName?: string | null
  sort: number
  status: EnableStatus
  title?: string | null
  /** 国际化键（如 menu.identity_user，有值时前端按键翻译菜单标题） */
  i18nKey?: string | null
}

export interface MenuUpdateDto extends BasicUpdateDto {
  badge?: string | null
  badgeDot: boolean
  badgeType?: string | null
  component?: string | null
  externalUrl?: string | null
  icon?: string | null
  isAffix: boolean
  isCache: boolean
  isExternal: boolean
  isVisible: boolean
  menuName: string
  menuType: MenuType
  metadata?: string | null
  parentId?: ApiId | null
  path: string
  permissionId?: ApiId | null
  redirect?: string | null
  remark?: string | null
  routeName?: string | null
  sort: number
  title?: string | null
  /** 国际化键（如 menu.identity_user，有值时前端按键翻译菜单标题） */
  i18nKey?: string | null
}

export interface MenuStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: EnableStatus
}

export interface MenuTreeQueryDto {
  includeButtons?: boolean | null
  keyword?: string | null
  limit: number
  onlyEnabled: boolean
  onlyVisible?: boolean | null
}

export interface MenuTreeNodeDto extends BasicDto {
  children: MenuTreeNodeDto[]
  component?: string | null
  icon?: string | null
  isGlobal: boolean
  menuCode: string
  menuName: string
  menuType: MenuType
  parentId?: ApiId | null
  path: string
  sort: number
  status: EnableStatus
  title?: string | null
  /** 国际化键（如 menu.identity_user，有值时前端按键翻译菜单标题） */
  i18nKey?: string | null
}

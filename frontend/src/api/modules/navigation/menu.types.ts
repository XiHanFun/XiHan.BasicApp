import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export { EnableStatus } from '../shared'

export enum MenuType {
  Directory = 0,
  Menu = 1,
  Button = 2,
  ExternalLink = 3,
}

export interface MenuPageQueryDto extends PageRequest {
  isGlobal?: boolean | null
  keyword?: string | null
  menuType?: MenuType | null
  parentId?: ApiId | null
  permissionId?: ApiId | null
  status?: EnableStatus | null
}

export interface MenuListItemDto extends BasicDto {
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
}

export interface MenuDetailDto extends MenuListItemDto {
  badge?: string | null
  badgeDot: boolean
  badgeType?: string | null
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
}

export interface MenuStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: EnableStatus
}

export interface MenuTreeQueryDto {
  keyword?: string | null
  limit: number
  onlyEnabled: boolean
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
}

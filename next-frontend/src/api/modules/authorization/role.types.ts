import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum DataPermissionScope {
  SelfOnly = 'SelfOnly',
  DepartmentOnly = 'DepartmentOnly',
  DepartmentAndChildren = 'DepartmentAndChildren',
  All = 'All',
  Custom = 'Custom',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum RoleType {
  System = 'System',
  Business = 'Business',
  Custom = 'Custom',
}

export interface RolePageQueryDto extends PageRequest {
  dataScope?: DataPermissionScope | null
  isGlobal?: boolean | null
  keyword?: string | null
  roleType?: RoleType | null
  status?: EnableStatus | null
}

export interface RoleListItemDto extends BasicDto {
  createdTime: DateTimeString
  dataScope: DataPermissionScope
  isGlobal: boolean
  maxMembers: number
  modifiedTime?: DateTimeString | null
  roleCode: string
  roleDescription?: string | null
  roleName: string
  roleType: RoleType
  sort: number
  status: EnableStatus
}

export interface RoleDetailDto extends RoleListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface RoleCreateDto {
  dataScope: DataPermissionScope
  maxMembers: number
  remark?: string | null
  roleCode: string
  roleDescription?: string | null
  roleName: string
  roleType: RoleType
  sort: number
  status: EnableStatus
}

export interface RoleUpdateDto extends BasicDto {
  dataScope: DataPermissionScope
  maxMembers: number
  remark?: string | null
  roleDescription?: string | null
  roleName: string
  roleType: RoleType
  sort: number
}

export interface RoleStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: EnableStatus
}

export interface RoleSelectQueryDto {
  isGlobal?: boolean | null
  keyword?: string | null
  limit: number
  roleType?: RoleType | null
}

export interface RoleSelectItemDto extends BasicDto {
  isGlobal: boolean
  roleCode: string
  roleName: string
  roleType: RoleType
}

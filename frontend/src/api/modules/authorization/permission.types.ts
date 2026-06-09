import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus, PermissionType } from '../shared'

export interface PermissionPageQueryDto extends PageRequest {
  isGlobal?: boolean | null
  isRequireAudit?: boolean | null
  keyword?: string | null
  moduleCode?: string | null
  operationId?: ApiId | null
  permissionType?: PermissionType | null
  resourceId?: ApiId | null
  status?: EnableStatus | null
}

export interface PermissionListItemDto extends BasicDto {
  createdTime: DateTimeString
  isGlobal: boolean
  isRequireAudit: boolean
  modifiedTime?: DateTimeString | null
  moduleCode?: string | null
  operationCode?: string | null
  operationId?: ApiId | null
  operationName?: string | null
  permissionCode: string
  permissionDescription?: string | null
  permissionName: string
  permissionType: PermissionType
  priority: number
  resourceCode?: string | null
  resourceId?: ApiId | null
  resourceName?: string | null
  sort: number
  status: EnableStatus
}

export interface PermissionDetailDto extends PermissionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
  tags?: string | null
}

export interface PermissionCreateDto {
  isRequireAudit: boolean
  moduleCode?: string | null
  operationId?: ApiId | null
  permissionCode: string
  permissionDescription?: string | null
  permissionName: string
  permissionType: PermissionType
  priority: number
  remark?: string | null
  resourceId?: ApiId | null
  sort: number
  status: EnableStatus
  tags?: string | null
}

export interface PermissionUpdateDto extends BasicUpdateDto {
  isRequireAudit: boolean
  permissionDescription?: string | null
  permissionName: string
  priority: number
  remark?: string | null
  sort: number
  tags?: string | null
}

export interface PermissionStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: EnableStatus
}

export interface PermissionSelectQueryDto {
  keyword?: string | null
  limit: number
  moduleCode?: string | null
  permissionType?: PermissionType | null
}

export interface PermissionSelectItemDto extends BasicDto {
  isRequireAudit: boolean
  moduleCode?: string | null
  permissionCode: string
  permissionName: string
  permissionType: PermissionType
  priority: number
}

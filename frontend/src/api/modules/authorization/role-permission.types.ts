import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString } from '../../types'
import type { EnableStatus, PermissionType, ValidityStatus } from '../shared'

export enum PermissionAction {
  Grant = 0,
  Deny = 1,
}

export interface RolePermissionListItemDto extends BasicDto {
  createdTime: DateTimeString
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  isGlobalPermission?: boolean | null
  isRequireAudit?: boolean | null
  moduleCode?: string | null
  permissionAction: PermissionAction
  permissionCode?: string | null
  permissionId: ApiId
  permissionName?: string | null
  permissionStatus?: EnableStatus | null
  permissionType?: PermissionType | null
  remark?: string | null
  roleId: ApiId
  status: ValidityStatus
}

export interface RolePermissionDetailDto extends RolePermissionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  permissionDescription?: string | null
  permissionPriority?: number | null
  tags?: string | null
}

export interface RolePermissionGrantDto {
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  permissionAction: PermissionAction
  permissionId: ApiId
  remark?: string | null
  roleId: ApiId
}

export interface RolePermissionUpdateDto extends BasicUpdateDto {
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  permissionAction: PermissionAction
  remark?: string | null
}

export interface RolePermissionStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: ValidityStatus
}

import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString } from '../../types'
import type { EnableStatus, PermissionType, ValidityStatus } from '../shared'
import type { TenantMemberInviteStatus, TenantMemberType } from '../tenant'
import type { PermissionAction } from './role-permission.types'

export interface UserPermissionListItemDto extends BasicDto {
  createdTime: DateTimeString
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  isExpired: boolean
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
  status: ValidityStatus
  tenantMemberDisplayName?: string | null
  tenantMemberId?: ApiId | null
  tenantMemberInviteStatus?: TenantMemberInviteStatus | null
  tenantMemberStatus?: ValidityStatus | null
  tenantMemberType?: TenantMemberType | null
  userId: ApiId
}

export interface UserPermissionDetailDto extends UserPermissionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  permissionDescription?: string | null
  permissionPriority?: number | null
  tags?: string | null
}

export interface UserPermissionGrantDto {
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  permissionAction: PermissionAction
  permissionId: ApiId
  remark?: string | null
  userId: ApiId
}

export interface UserPermissionUpdateDto extends BasicUpdateDto {
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  permissionAction: PermissionAction
  remark?: string | null
}

export interface UserPermissionStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: ValidityStatus
}

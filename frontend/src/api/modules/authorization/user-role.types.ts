import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString } from '../../types'
import type { EnableStatus, ValidityStatus } from '../shared'
import type { TenantMemberInviteStatus, TenantMemberType } from '../tenant'
import type { DataPermissionScope, RoleType } from './role.types'

export interface UserRoleListItemDto extends BasicDto {
  createdTime: DateTimeString
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  isExpired: boolean
  isGlobalRole?: boolean | null
  remark?: string | null
  roleCode?: string | null
  roleDataScope?: DataPermissionScope | null
  roleId: ApiId
  roleName?: string | null
  roleStatus?: EnableStatus | null
  roleType?: RoleType | null
  status: ValidityStatus
  tenantMemberDisplayName?: string | null
  tenantMemberId?: ApiId | null
  tenantMemberInviteStatus?: TenantMemberInviteStatus | null
  tenantMemberStatus?: ValidityStatus | null
  tenantMemberType?: TenantMemberType | null
  userId: ApiId
}

export interface UserRoleDetailDto extends UserRoleListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  roleDescription?: string | null
}

export interface UserRoleGrantDto {
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  remark?: string | null
  roleId: ApiId
  userId: ApiId
}

export interface UserRoleUpdateDto extends BasicUpdateDto {
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  remark?: string | null
}

export interface UserRoleStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: ValidityStatus
}

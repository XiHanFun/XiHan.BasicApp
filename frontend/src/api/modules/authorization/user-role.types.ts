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

/** 批量变更用户角色（一次性提交授予与撤销） */
export interface UserRoleBatchUpdateDto {
  grantRoleIds: ApiId[]
  revokeUserRoleIds: ApiId[]
  userId: ApiId
}

/** 以角色为中心批量维护成员：加入按用户主键，移出按绑定主键 */
export interface RoleMemberBatchUpdateDto {
  grantUserIds: ApiId[]
  revokeUserRoleIds: ApiId[]
  roleId: ApiId
}

/** 角色在本租户此刻生效的成员 */
export interface RoleMemberDto {
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  isExternalMember: boolean
  nickName?: string | null
  realName?: string | null
  userId: ApiId
  userName: string
  userRoleId: ApiId
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

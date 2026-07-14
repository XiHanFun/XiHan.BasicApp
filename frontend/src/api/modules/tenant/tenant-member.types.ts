import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { ValidityStatus } from '../shared'
import type { TenantMemberInviteStatus, TenantMemberType } from './tenant-enums.types'

export interface TenantMemberPageQueryDto extends PageRequest {
  expirationTimeEnd?: DateTimeString | null
  expirationTimeStart?: DateTimeString | null
  inviteStatus?: TenantMemberInviteStatus | null
  keyword?: string | null
  memberType?: TenantMemberType | null
  status?: ValidityStatus | null
  /** 所属租户；查"某租户的成员"时必传，否则平台态会返回所有租户的成员关系 */
  tenantId?: ApiId | null
  userId?: ApiId | null
}

export interface TenantMemberListItemDto extends BasicDto {
  createdTime: DateTimeString
  /** 租户内覆盖名（多数成员为空）；展示请用 resolveMemberName 回退 */
  displayName?: string | null
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  inviteStatus: TenantMemberInviteStatus
  invitedBy?: ApiId | null
  invitedTime?: DateTimeString | null
  isExpired: boolean
  lastActiveTime?: DateTimeString | null
  memberType: TenantMemberType
  modifiedTime?: DateTimeString | null
  /** 只读，来自 SysUser */
  nickName?: string | null
  /** 只读，来自 SysUser */
  realName?: string | null
  respondedTime?: DateTimeString | null
  status: ValidityStatus
  userId: ApiId
  /** 只读，来自 SysUser */
  userName?: string | null
}

export interface TenantMemberDetailDto extends TenantMemberListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  inviteRemark?: string | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface TenantMemberUpdateDto extends BasicDto {
  displayName?: string | null
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  inviteRemark?: string | null
  memberType: TenantMemberType
  remark?: string | null
}

export interface TenantMemberStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: ValidityStatus
}

export interface TenantMemberInviteStatusUpdateDto extends BasicDto {
  inviteRemark?: string | null
  inviteStatus: TenantMemberInviteStatus
}

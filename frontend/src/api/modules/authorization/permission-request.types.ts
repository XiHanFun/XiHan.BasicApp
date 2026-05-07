import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { AuditResult, AuditStatus } from '../workflow'

export enum PermissionRequestStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Withdrawn = 3,
  Expired = 4,
}

export interface PermissionRequestPageQueryDto extends PageRequest {
  keyword?: string | null
  permissionId?: ApiId | null
  requestStatus?: PermissionRequestStatus | null
  requestUserId?: ApiId | null
  reviewId?: ApiId | null
  roleId?: ApiId | null
}

export interface PermissionRequestListItemDto extends BasicDto {
  createdTime: DateTimeString
  expectedEffectiveTime?: DateTimeString | null
  expectedExpirationTime?: DateTimeString | null
  isExpectedExpired: boolean
  modifiedTime?: DateTimeString | null
  permissionCode?: string | null
  permissionId?: ApiId | null
  permissionName?: string | null
  requestReason: string
  requestStatus: PermissionRequestStatus
  requestTenantMemberId?: ApiId | null
  requestUserDisplayName?: string | null
  requestUserId: ApiId
  reviewCode?: string | null
  reviewId?: ApiId | null
  reviewResult?: AuditResult | null
  reviewStatus?: AuditStatus | null
  reviewTitle?: string | null
  roleCode?: string | null
  roleId?: ApiId | null
  roleName?: string | null
}

export interface PermissionRequestDetailDto extends PermissionRequestListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  permissionDescription?: string | null
  remark?: string | null
  reviewDescription?: string | null
  roleDescription?: string | null
}

export interface PermissionRequestCreateDto {
  expectedEffectiveTime?: DateTimeString | null
  expectedExpirationTime?: DateTimeString | null
  permissionId?: ApiId | null
  remark?: string | null
  requestReason: string
  roleId?: ApiId | null
}

export interface PermissionRequestUpdateDto extends BasicUpdateDto {
  expectedEffectiveTime?: DateTimeString | null
  expectedExpirationTime?: DateTimeString | null
  permissionId?: ApiId | null
  remark?: string | null
  requestReason: string
  roleId?: ApiId | null
}

export interface PermissionRequestStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  requestStatus: PermissionRequestStatus
  reviewId?: ApiId | null
}

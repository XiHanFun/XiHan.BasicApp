import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'

export enum DelegationStatus {
  Pending = 0,
  Active = 1,
  Expired = 2,
  Revoked = 3,
}

export interface PermissionDelegationPageQueryDto extends PageRequest {
  delegateeUserId?: ApiId | null
  delegationStatus?: DelegationStatus | null
  delegatorUserId?: ApiId | null
  keyword?: string | null
  permissionId?: ApiId | null
  roleId?: ApiId | null
}

export interface PermissionDelegationListItemDto extends BasicDto {
  createdTime: DateTimeString
  delegateeDisplayName?: string | null
  delegateeTenantMemberId?: ApiId | null
  delegateeUserId: ApiId
  delegationReason?: string | null
  delegationStatus: DelegationStatus
  delegatorDisplayName?: string | null
  delegatorTenantMemberId?: ApiId | null
  delegatorUserId: ApiId
  effectiveTime?: DateTimeString | null
  expirationTime: DateTimeString
  isExpired: boolean
  modifiedTime?: DateTimeString | null
  permissionCode?: string | null
  permissionId?: ApiId | null
  permissionName?: string | null
  roleCode?: string | null
  roleId?: ApiId | null
  roleName?: string | null
}

export interface PermissionDelegationDetailDto extends PermissionDelegationListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  permissionDescription?: string | null
  remark?: string | null
  roleDescription?: string | null
}

export interface PermissionDelegationCreateDto {
  delegateeUserId: ApiId
  delegationReason?: string | null
  delegatorUserId: ApiId
  effectiveTime?: DateTimeString | null
  expirationTime: DateTimeString
  permissionId?: ApiId | null
  remark?: string | null
  roleId?: ApiId | null
}

export interface PermissionDelegationUpdateDto extends BasicUpdateDto {
  delegateeUserId: ApiId
  delegationReason?: string | null
  delegatorUserId: ApiId
  effectiveTime?: DateTimeString | null
  expirationTime: DateTimeString
  permissionId?: ApiId | null
  remark?: string | null
  roleId?: ApiId | null
}

export interface PermissionDelegationStatusUpdateDto extends BasicUpdateDto {
  delegationStatus: DelegationStatus
  remark?: string | null
}

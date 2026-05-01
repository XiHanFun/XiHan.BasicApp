import type { ApiId, BasicCreateDto, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'

export enum EnableStatus {
  Disabled = 0,
  Enabled = 1,
}

export enum ValidityStatus {
  Invalid = 0,
  Valid = 1,
}

export enum TenantConfigStatus {
  Pending = 0,
  Configuring = 1,
  Configured = 2,
  Failed = 3,
  Disabled = 4,
}

export enum TenantIsolationMode {
  Field = 0,
  Database = 1,
  Schema = 2,
}

export enum TenantStatus {
  Normal = 0,
  Suspended = 1,
  Expired = 2,
  Disabled = 3,
}

export enum TenantMemberInviteStatus {
  Pending = 0,
  Accepted = 1,
  Rejected = 2,
  Revoked = 3,
  Expired = 4,
}

export enum TenantMemberType {
  Owner = 0,
  Admin = 1,
  Member = 2,
  External = 3,
  Guest = 4,
  Consultant = 5,
  PlatformAdmin = 99,
}

export enum PermissionType {
  ResourceBased = 0,
  Functional = 1,
  DataScope = 2,
}

export interface TenantPageQueryDto extends PageRequest {
  configStatus?: TenantConfigStatus | null
  editionId?: ApiId | null
  expireTimeEnd?: DateTimeString | null
  expireTimeStart?: DateTimeString | null
  keyword?: string | null
  tenantStatus?: TenantStatus | null
}

export interface TenantListItemDto extends BasicDto {
  configStatus: TenantConfigStatus
  createdTime: DateTimeString
  domain?: string | null
  editionId?: ApiId | null
  expireTime?: DateTimeString | null
  isExpired: boolean
  isolationMode: TenantIsolationMode
  logo?: string | null
  modifiedTime?: DateTimeString | null
  sort: number
  storageLimit?: number | null
  tenantCode: string
  tenantName: string
  tenantShortName?: string | null
  tenantStatus: TenantStatus
  userLimit?: number | null
}

export interface TenantDetailDto extends TenantListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
}

export interface TenantCreateDto extends BasicCreateDto {
  domain?: string | null
  editionId?: ApiId | null
  expireTime?: DateTimeString | null
  isolationMode: TenantIsolationMode
  logo?: string | null
  remark?: string | null
  sort: number
  storageLimit?: number | null
  tenantCode: string
  tenantName: string
  tenantShortName?: string | null
  userLimit?: number | null
}

export interface TenantUpdateDto extends BasicUpdateDto {
  domain?: string | null
  editionId?: ApiId | null
  expireTime?: DateTimeString | null
  isolationMode: TenantIsolationMode
  logo?: string | null
  remark?: string | null
  sort: number
  storageLimit?: number | null
  tenantName: string
  tenantShortName?: string | null
  userLimit?: number | null
}

export interface TenantStatusUpdateDto extends BasicDto {
  reason?: string | null
  tenantStatus: TenantStatus
}

export interface TenantSwitcherDto {
  configStatus: TenantConfigStatus
  domain?: string | null
  expireTime?: DateTimeString | null
  inviteStatus: TenantMemberInviteStatus
  isCurrent: boolean
  logo?: string | null
  memberType: TenantMemberType
  membershipExpirationTime?: DateTimeString | null
  membershipId: ApiId
  tenantCode: string
  tenantId: ApiId
  tenantName: string
  tenantShortName?: string | null
  tenantStatus: TenantStatus
}

export interface TenantEditionPageQueryDto extends PageRequest {
  isDefault?: boolean | null
  isFree?: boolean | null
  keyword?: string | null
  status?: EnableStatus | null
}

export interface TenantEditionListItemDto extends BasicDto {
  billingPeriodMonths?: number | null
  createdTime: DateTimeString
  description?: string | null
  editionCode: string
  editionName: string
  isDefault: boolean
  isFree: boolean
  modifiedTime?: DateTimeString | null
  price?: number | null
  sort: number
  status: EnableStatus
  storageLimit?: number | null
  userLimit?: number | null
}

export interface TenantEditionDetailDto extends TenantEditionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface TenantEditionCreateDto extends BasicCreateDto {
  billingPeriodMonths?: number | null
  description?: string | null
  editionCode: string
  editionName: string
  isDefault: boolean
  isFree: boolean
  price?: number | null
  remark?: string | null
  sort: number
  status: EnableStatus
  storageLimit?: number | null
  userLimit?: number | null
}

export interface TenantEditionUpdateDto extends BasicUpdateDto {
  billingPeriodMonths?: number | null
  description?: string | null
  editionName: string
  isDefault: boolean
  isFree: boolean
  price?: number | null
  remark?: string | null
  sort: number
  storageLimit?: number | null
  userLimit?: number | null
}

export interface TenantEditionStatusUpdateDto extends BasicDto {
  status: EnableStatus
}

export interface TenantEditionDefaultUpdateDto extends BasicDto {}

export interface TenantMemberPageQueryDto extends PageRequest {
  expirationTimeEnd?: DateTimeString | null
  expirationTimeStart?: DateTimeString | null
  inviteStatus?: TenantMemberInviteStatus | null
  keyword?: string | null
  memberType?: TenantMemberType | null
  status?: ValidityStatus | null
  userId?: ApiId | null
}

export interface TenantMemberListItemDto extends BasicDto {
  createdTime: DateTimeString
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
  respondedTime?: DateTimeString | null
  status: ValidityStatus
  userId: ApiId
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

export interface TenantEditionPermissionListItemDto extends BasicDto {
  createdTime: DateTimeString
  editionId: ApiId
  isGlobalPermission?: boolean | null
  isRequireAudit?: boolean | null
  moduleCode?: string | null
  permissionCode?: string | null
  permissionId: ApiId
  permissionName?: string | null
  permissionStatus?: EnableStatus | null
  permissionType?: PermissionType | null
  remark?: string | null
  status: ValidityStatus
}

export interface TenantEditionPermissionDetailDto extends TenantEditionPermissionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  permissionDescription?: string | null
  permissionPriority?: number | null
  tags?: string | null
}

export interface TenantEditionPermissionGrantDto {
  editionId: ApiId
  permissionId: ApiId
  remark?: string | null
}

export interface TenantEditionPermissionStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: ValidityStatus
}

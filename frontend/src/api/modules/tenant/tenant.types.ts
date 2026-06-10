import type { ApiId, BasicCreateDto, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type {
  TenantConfigStatus,
  TenantIsolationMode,
  TenantMemberInviteStatus,
  TenantMemberType,
  TenantStatus,
} from './tenant-enums.types'

export interface TenantPageQueryDto extends PageRequest {
  configStatus?: TenantConfigStatus | null
  editionId?: ApiId | null
  expirationTimeEnd?: DateTimeString | null
  expirationTimeStart?: DateTimeString | null
  keyword?: string | null
  tenantStatus?: TenantStatus | null
}

export interface TenantListItemDto extends BasicDto {
  configStatus: TenantConfigStatus
  createdTime: DateTimeString
  domain?: string | null
  editionId?: ApiId | null
  expirationTime?: DateTimeString | null
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
  /** 租户管理员用户名（与 adminEmail/adminPassword 同时提供时一站式开通管理员/角色/授权） */
  adminUserName?: string | null
  /** 租户管理员邮箱（登录身份标识，全平台唯一；开通管理员时必填） */
  adminEmail?: string | null
  /** 租户管理员初始密码 */
  adminPassword?: string | null
  domain?: string | null
  editionId?: ApiId | null
  expirationTime?: DateTimeString | null
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
  expirationTime?: DateTimeString | null
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
  expirationTime?: DateTimeString | null
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

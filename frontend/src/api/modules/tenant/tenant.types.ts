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

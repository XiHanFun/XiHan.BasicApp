import type { ApiId, BasicCreateDto, BasicDto, BasicUpdateDto, DateTimeString, NumericString, PageRequest } from '../../types'
import type {
  TenantConfigStatus,
  TenantDatabaseType,
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
  databaseType?: TenantDatabaseType | null
  domain?: string | null
  editionId?: ApiId | null
  /** 生效存储上限(MB)：租户未设值时回落到所属版本套餐，null 表示不限（后端 long，按字符串传输） */
  effectiveStorageLimit?: NumericString | null
  /** 生效用户数上限：租户未设值时回落到所属版本套餐，null 表示不限 */
  effectiveUserLimit?: number | null
  expirationTime?: DateTimeString | null
  isExpired: boolean
  isolationMode: TenantIsolationMode
  logo?: string | null
  modifiedTime?: DateTimeString | null
  sort: number
  /** 存储空间限制(MB)（后端 long，按字符串传输） */
  storageLimit?: NumericString | null
  tenantCode: string
  tenantName: string
  tenantShortName?: string | null
  tenantStatus: TenantStatus
  /** 已占用存储空间(字节)（后端 long，按字符串传输） */
  usedStorageBytes: NumericString
  /** 已占用席位数（不含平台管理员成员；后端 long，按字符串传输） */
  usedUserCount: NumericString
  /** 是否已开通管理员（已有所有者成员） */
  hasOwner: boolean
  userLimit?: number | null
}

export interface TenantDetailDto extends TenantListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

/** 已超出配额的租户（存量核对用；上限为空即不限的租户不会出现在结果里） */
export interface TenantOverQuotaDto {
  /** 席位是否已超出上限 */
  seatExceeded: boolean
  /** 生效存储上限(MB)（后端 long，按字符串传输） */
  storageLimit?: NumericString | null
  /** 存储是否已超出上限 */
  storageExceeded: boolean
  tenantCode: string
  tenantId: ApiId
  tenantName: string
  /** 已占用存储空间(字节)（后端 long，按字符串传输） */
  usedStorageBytes: NumericString
  /** 已占用席位数（后端 long，按字符串传输） */
  usedUserCount: NumericString
  /** 生效席位上限 */
  userLimit?: number | null
}

/** 租户创建：不含管理员，建好之后经 initializeTenantAdmin 开通（库隔离租户先初始化数据库） */
export interface TenantCreateDto extends BasicCreateDto {
  /** 数据库连接字符串（隔离模式为 Database 时必填；加密落库、绝不回显） */
  connectionString?: string | null
  /** 数据库类型（隔离模式为 Database 时必填） */
  databaseType?: TenantDatabaseType | null
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

/** 租户更新：隔离模式创建后不能修改，不在更新契约里 */
export interface TenantUpdateDto extends BasicUpdateDto {
  /** 数据库连接字符串（留空表示保持不变；填写则加密覆盖、绝不回显） */
  connectionString?: string | null
  /** 数据库类型（隔离模式为 Database 时必填） */
  databaseType?: TenantDatabaseType | null
  domain?: string | null
  editionId?: ApiId | null
  expirationTime?: DateTimeString | null
  logo?: string | null
  remark?: string | null
  sort: number
  storageLimit?: number | null
  tenantName: string
  tenantShortName?: string | null
  userLimit?: number | null
}

/** 初始化租户管理员（建租户之后；库隔离租户在独立库初始化完成之后） */
export interface TenantAdminInitializeDto {
  adminEmail: string
  adminPassword: string
  adminUserName: string
  tenantId: ApiId
}

/** 当前租户的订阅（租户自己看：版本套餐、到期时间、席位与存储用量） */
export interface TenantSubscriptionDto {
  editionCode?: string | null
  editionDescription?: string | null
  /** 版本名称（未绑定版本时为空） */
  editionName?: string | null
  /** 生效存储上限(MB)（租户未设值时取版本的，空表示不限；后端 long，按字符串传输） */
  effectiveStorageLimit?: NumericString | null
  /** 生效席位上限（租户未设值时取版本的，空表示不限） */
  effectiveUserLimit?: number | null
  /** 到期时间（空表示长期有效） */
  expirationTime?: DateTimeString | null
  isExpired: boolean
  isFreeEdition: boolean
  tenantCode: string
  tenantId: ApiId
  tenantName: string
  tenantStatus: TenantStatus
  /** 已占用存储(字节)（后端 long，按字符串传输） */
  usedStorageBytes: NumericString
  /** 已占用席位数（不含支持人员；后端 long，按字符串传输） */
  usedUserCount: NumericString
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
  /** 加入时间（受邀响应时间，缺省为成员关系创建时间） */
  joinedTime: DateTimeString
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

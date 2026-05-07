import type { ApiId, BasicDto, DateTimeString } from '../../types'
import type { PermissionChangeLogListItemDto } from '../audit'
import type {
  FieldLevelSecurityListItemDto,
  OperationDetailDto,
  PermissionConditionListItemDto,
  PermissionDelegationListItemDto,
  PermissionDetailDto,
  PermissionRequestListItemDto,
  ResourceDetailDto,
  RoleDataScopeListItemDto,
  RoleHierarchyListItemDto,
  RolePermissionListItemDto,
  UserDataScopeListItemDto,
  UserPermissionListItemDto,
  UserRoleListItemDto,
} from '../authorization'
import type { RoleDetailDto } from '../authorization/role.types'
import type { UserDetailDto, UserSessionListItemDto, UserStatisticsDetailDto } from '../identity'
import type { DepartmentType } from '../organization/department.types'
import type { EnableStatus, ValidityStatus } from '../shared'
import type { TenantMemberListItemDto } from '../tenant'

export enum TwoFactorMethod {
  None = 0,
  Totp = 1,
  Email = 2,
  Phone = 4,
}

export interface UserManagementDepartmentDto extends BasicDto {
  createdTime: DateTimeString
  departmentCode?: string | null
  departmentId: ApiId
  departmentName?: string | null
  departmentStatus?: EnableStatus | null
  departmentType?: DepartmentType | null
  isMain: boolean
  parentId?: ApiId | null
  remark?: string | null
  status: ValidityStatus
  userId: ApiId
}

export interface UserManagementSecurityDto extends BasicDto {
  allowMultiLogin: boolean
  createdBy?: string | null
  createdId?: ApiId | null
  createdTime: DateTimeString
  emailVerified: boolean
  failedLoginAttempts: number
  isLocked: boolean
  isPasswordExpired: boolean
  lastFailedLoginTime?: DateTimeString | null
  lastPasswordChangeTime?: DateTimeString | null
  lastSecurityCheckTime?: DateTimeString | null
  lastUserNameChangeTime?: DateTimeString | null
  lockoutEndTime?: DateTimeString | null
  lockoutTime?: DateTimeString | null
  maxLoginDevices: number
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  modifiedTime?: DateTimeString | null
  nickName?: string | null
  passwordExpiryTime?: DateTimeString | null
  phoneVerified: boolean
  realName?: string | null
  remark?: string | null
  twoFactorEnabled: boolean
  twoFactorMethod: TwoFactorMethod
  userId: ApiId
  userName?: string | null
}

export interface UserManagementExternalLoginDto extends BasicDto {
  createdTime: DateTimeString
  externalAccountMasked: string
  externalEmailMasked?: string | null
  lastLoginTime?: DateTimeString | null
  modifiedTime?: DateTimeString | null
  nickName?: string | null
  provider: string
  providerDisplayName?: string | null
  realName?: string | null
  userId: ApiId
  userName?: string | null
}

export interface UserManagementPasswordHistoryDto extends BasicDto {
  changedTime: DateTimeString
  createdTime: DateTimeString
  nickName?: string | null
  realName?: string | null
  userId: ApiId
  userName?: string | null
}

export interface UserManagementDetailDto {
  dataScopes: UserDataScopeListItemDto[]
  departments: UserManagementDepartmentDto[]
  externalLogins: UserManagementExternalLoginDto[]
  generatedTime: DateTimeString
  passwordHistories: UserManagementPasswordHistoryDto[]
  permissions: UserPermissionListItemDto[]
  roles: UserRoleListItemDto[]
  security?: UserManagementSecurityDto | null
  sessions: UserSessionListItemDto[]
  statistics: UserStatisticsDetailDto[]
  tenantMembership?: TenantMemberListItemDto | null
  user: UserDetailDto
}

export interface RoleManagementGrantedUserDto {
  avatar?: string | null
  basicId: ApiId
  createdTime: DateTimeString
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  isExpired: boolean
  nickName?: string | null
  realName?: string | null
  remark?: string | null
  status: ValidityStatus
  userId: ApiId
  userName?: string | null
}

export interface RoleManagementDetailDto {
  ancestors: RoleHierarchyListItemDto[]
  dataScopes: RoleDataScopeListItemDto[]
  descendants: RoleHierarchyListItemDto[]
  generatedTime: DateTimeString
  grantedUsers: RoleManagementGrantedUserDto[]
  permissions: RolePermissionListItemDto[]
  role: RoleDetailDto
}

export interface PermissionCenterDetailDto {
  changeLogs: PermissionChangeLogListItemDto[]
  conditions: PermissionConditionListItemDto[]
  delegations: PermissionDelegationListItemDto[]
  fieldSecurities: FieldLevelSecurityListItemDto[]
  generatedTime: DateTimeString
  operation?: OperationDetailDto | null
  permission: PermissionDetailDto
  requests: PermissionRequestListItemDto[]
  resource?: ResourceDetailDto | null
}

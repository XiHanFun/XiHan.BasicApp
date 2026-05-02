import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus, PermissionType, ValidityStatus } from '../shared'
import type { TenantMemberInviteStatus, TenantMemberType } from '../tenant'

export interface PermissionPageQueryDto extends PageRequest {
  isGlobal?: boolean | null
  isRequireAudit?: boolean | null
  keyword?: string | null
  moduleCode?: string | null
  operationId?: ApiId | null
  permissionType?: PermissionType | null
  resourceId?: ApiId | null
  status?: EnableStatus | null
}

export interface PermissionListItemDto extends BasicDto {
  createdTime: DateTimeString
  isGlobal: boolean
  isRequireAudit: boolean
  modifiedTime?: DateTimeString | null
  moduleCode?: string | null
  operationCode?: string | null
  operationId?: ApiId | null
  operationName?: string | null
  permissionCode: string
  permissionDescription?: string | null
  permissionName: string
  permissionType: PermissionType
  priority: number
  resourceCode?: string | null
  resourceId?: ApiId | null
  resourceName?: string | null
  sort: number
  status: EnableStatus
}

export interface PermissionDetailDto extends PermissionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
  tags?: string | null
}

export interface PermissionCreateDto {
  isRequireAudit: boolean
  moduleCode?: string | null
  operationId?: ApiId | null
  permissionCode: string
  permissionDescription?: string | null
  permissionName: string
  permissionType: PermissionType
  priority: number
  remark?: string | null
  resourceId?: ApiId | null
  sort: number
  status: EnableStatus
  tags?: string | null
}

export interface PermissionUpdateDto extends BasicUpdateDto {
  isRequireAudit: boolean
  permissionDescription?: string | null
  permissionName: string
  priority: number
  remark?: string | null
  sort: number
  tags?: string | null
}

export interface PermissionStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: EnableStatus
}

export interface PermissionSelectQueryDto {
  keyword?: string | null
  limit: number
  moduleCode?: string | null
  permissionType?: PermissionType | null
}

export interface PermissionSelectItemDto extends BasicDto {
  isRequireAudit: boolean
  moduleCode?: string | null
  permissionCode: string
  permissionName: string
  permissionType: PermissionType
  priority: number
}

export enum ResourceAccessLevel {
  Public = 0,
  Authenticated = 1,
  Authorized = 2,
}

export enum ResourceType {
  Api = 0,
  File = 1,
  DataTable = 2,
  BusinessObject = 3,
  Other = 99,
}

export interface ResourcePageQueryDto extends PageRequest {
  accessLevel?: ResourceAccessLevel | null
  isGlobal?: boolean | null
  keyword?: string | null
  resourceType?: ResourceType | null
  status?: EnableStatus | null
}

export interface ResourceListItemDto extends BasicDto {
  accessLevel: ResourceAccessLevel
  createdTime: DateTimeString
  description?: string | null
  isGlobal: boolean
  modifiedTime?: DateTimeString | null
  resourceCode: string
  resourceName: string
  resourcePath?: string | null
  resourceType: ResourceType
  sort: number
  status: EnableStatus
}

export interface ResourceDetailDto extends ResourceListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  metadata?: string | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface ResourceCreateDto {
  accessLevel: ResourceAccessLevel
  description?: string | null
  metadata?: string | null
  remark?: string | null
  resourceCode: string
  resourceName: string
  resourcePath?: string | null
  resourceType: ResourceType
  sort: number
  status: EnableStatus
}

export interface ResourceUpdateDto extends BasicUpdateDto {
  accessLevel: ResourceAccessLevel
  description?: string | null
  metadata?: string | null
  remark?: string | null
  resourceName: string
  resourcePath?: string | null
  resourceType: ResourceType
  sort: number
}

export interface ResourceStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: EnableStatus
}

export interface ResourceSelectQueryDto {
  keyword?: string | null
  limit: number
  resourceType?: ResourceType | null
}

export interface ResourceSelectItemDto extends BasicDto {
  resourceCode: string
  resourceName: string
  resourcePath?: string | null
  resourceType: ResourceType
}

export enum HttpMethodType {
  GET = 0,
  POST = 1,
  PUT = 2,
  DELETE = 3,
  PATCH = 4,
  HEAD = 5,
  OPTIONS = 6,
  ALL = 99,
}

export enum OperationCategory {
  Crud = 0,
  Business = 1,
  Admin = 2,
  System = 3,
  Custom = 99,
}

export enum OperationTypeCode {
  Create = 0,
  Read = 1,
  Update = 2,
  Delete = 3,
  View = 4,
  Approve = 10,
  Execute = 11,
  Import = 20,
  Export = 21,
  Upload = 22,
  Download = 23,
  Print = 24,
  Share = 25,
  Grant = 30,
  Revoke = 31,
  Enable = 32,
  Disable = 33,
  Custom = 99,
}

export interface OperationPageQueryDto extends PageRequest {
  category?: OperationCategory | null
  httpMethod?: HttpMethodType | null
  isDangerous?: boolean | null
  isGlobal?: boolean | null
  isRequireAudit?: boolean | null
  keyword?: string | null
  operationTypeCode?: OperationTypeCode | null
  status?: EnableStatus | null
}

export interface OperationListItemDto extends BasicDto {
  category: OperationCategory
  color?: string | null
  createdTime: DateTimeString
  description?: string | null
  httpMethod?: HttpMethodType | null
  icon?: string | null
  isDangerous: boolean
  isGlobal: boolean
  isRequireAudit: boolean
  modifiedTime?: DateTimeString | null
  operationCode: string
  operationName: string
  operationTypeCode: OperationTypeCode
  sort: number
  status: EnableStatus
}

export interface OperationDetailDto extends OperationListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface OperationCreateDto {
  category: OperationCategory
  color?: string | null
  description?: string | null
  httpMethod?: HttpMethodType | null
  icon?: string | null
  isDangerous: boolean
  isRequireAudit: boolean
  operationCode: string
  operationName: string
  operationTypeCode: OperationTypeCode
  remark?: string | null
  sort: number
  status: EnableStatus
}

export interface OperationUpdateDto extends BasicUpdateDto {
  category: OperationCategory
  color?: string | null
  description?: string | null
  httpMethod?: HttpMethodType | null
  icon?: string | null
  isDangerous: boolean
  isRequireAudit: boolean
  operationName: string
  operationTypeCode: OperationTypeCode
  remark?: string | null
  sort: number
}

export interface OperationStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: EnableStatus
}

export interface OperationSelectQueryDto {
  category?: OperationCategory | null
  keyword?: string | null
  limit: number
  operationTypeCode?: OperationTypeCode | null
}

export interface OperationSelectItemDto extends BasicDto {
  category: OperationCategory
  httpMethod?: HttpMethodType | null
  isDangerous: boolean
  isRequireAudit: boolean
  operationCode: string
  operationName: string
  operationTypeCode: OperationTypeCode
}

export enum DataPermissionScope {
  SelfOnly = 0,
  DepartmentOnly = 1,
  DepartmentAndChildren = 2,
  All = 3,
  Custom = 99,
}

export enum RoleType {
  System = 0,
  Business = 1,
  Custom = 2,
}

export interface RolePageQueryDto extends PageRequest {
  dataScope?: DataPermissionScope | null
  isGlobal?: boolean | null
  keyword?: string | null
  roleType?: RoleType | null
  status?: EnableStatus | null
}

export interface RoleListItemDto extends BasicDto {
  createdTime: DateTimeString
  dataScope: DataPermissionScope
  isGlobal: boolean
  maxMembers: number
  modifiedTime?: DateTimeString | null
  roleCode: string
  roleDescription?: string | null
  roleName: string
  roleType: RoleType
  sort: number
  status: EnableStatus
}

export interface RoleDetailDto extends RoleListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface RoleCreateDto {
  dataScope: DataPermissionScope
  maxMembers: number
  remark?: string | null
  roleCode: string
  roleDescription?: string | null
  roleName: string
  roleType: RoleType
  sort: number
  status: EnableStatus
}

export interface RoleUpdateDto extends BasicDto {
  dataScope: DataPermissionScope
  maxMembers: number
  remark?: string | null
  roleDescription?: string | null
  roleName: string
  roleType: RoleType
  sort: number
}

export interface RoleStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: EnableStatus
}

export interface RoleSelectQueryDto {
  isGlobal?: boolean | null
  keyword?: string | null
  limit: number
  roleType?: RoleType | null
}

export interface RoleSelectItemDto extends BasicDto {
  isGlobal: boolean
  roleCode: string
  roleName: string
  roleType: RoleType
}

export enum PermissionAction {
  Grant = 0,
  Deny = 1,
}

export interface RolePermissionListItemDto extends BasicDto {
  createdTime: DateTimeString
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  isGlobalPermission?: boolean | null
  isRequireAudit?: boolean | null
  moduleCode?: string | null
  permissionAction: PermissionAction
  permissionCode?: string | null
  permissionId: ApiId
  permissionName?: string | null
  permissionStatus?: EnableStatus | null
  permissionType?: PermissionType | null
  remark?: string | null
  roleId: ApiId
  status: ValidityStatus
}

export interface RolePermissionDetailDto extends RolePermissionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  permissionDescription?: string | null
  permissionPriority?: number | null
  tags?: string | null
}

export interface RolePermissionGrantDto {
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  permissionAction: PermissionAction
  permissionId: ApiId
  remark?: string | null
  roleId: ApiId
}

export interface RolePermissionUpdateDto extends BasicUpdateDto {
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  permissionAction: PermissionAction
  remark?: string | null
}

export interface RolePermissionStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: ValidityStatus
}

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

export interface UserPermissionListItemDto extends BasicDto {
  createdTime: DateTimeString
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  isExpired: boolean
  isGlobalPermission?: boolean | null
  isRequireAudit?: boolean | null
  moduleCode?: string | null
  permissionAction: PermissionAction
  permissionCode?: string | null
  permissionId: ApiId
  permissionName?: string | null
  permissionStatus?: EnableStatus | null
  permissionType?: PermissionType | null
  remark?: string | null
  status: ValidityStatus
  tenantMemberDisplayName?: string | null
  tenantMemberId?: ApiId | null
  tenantMemberInviteStatus?: TenantMemberInviteStatus | null
  tenantMemberStatus?: ValidityStatus | null
  tenantMemberType?: TenantMemberType | null
  userId: ApiId
}

export interface UserPermissionDetailDto extends UserPermissionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  permissionDescription?: string | null
  permissionPriority?: number | null
  tags?: string | null
}

export interface UserPermissionGrantDto {
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  permissionAction: PermissionAction
  permissionId: ApiId
  remark?: string | null
  userId: ApiId
}

export interface UserPermissionUpdateDto extends BasicUpdateDto {
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  grantReason?: string | null
  permissionAction: PermissionAction
  remark?: string | null
}

export interface UserPermissionStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: ValidityStatus
}

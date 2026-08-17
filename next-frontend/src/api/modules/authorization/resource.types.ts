import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum ResourceAccessLevel {
  Public = 'Public',
  Authenticated = 'Authenticated',
  Authorized = 'Authorized',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum ResourceType {
  Api = 'Api',
  File = 'File',
  DataTable = 'DataTable',
  BusinessObject = 'BusinessObject',
  Other = 'Other',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum FieldMaskStrategy {
  None = 'None',
  Hidden = 'Hidden',
  FullMask = 'FullMask',
  PartialMask = 'PartialMask',
  Hash = 'Hash',
  Redact = 'Redact',
  Custom = 'Custom',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum FieldSecurityTargetType {
  Role = 'Role',
  User = 'User',
  Permission = 'Permission',
  Department = 'Department',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum ConditionOperator {
  Equals = 'Equals',
  NotEquals = 'NotEquals',
  GreaterThan = 'GreaterThan',
  GreaterThanOrEquals = 'GreaterThanOrEquals',
  LessThan = 'LessThan',
  LessThanOrEquals = 'LessThanOrEquals',
  Contains = 'Contains',
  NotContains = 'NotContains',
  In = 'In',
  NotIn = 'NotIn',
  Between = 'Between',
  StartsWith = 'StartsWith',
  EndsWith = 'EndsWith',
  IsNull = 'IsNull',
  IsNotNull = 'IsNotNull',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum ConfigDataType {
  String = 'String',
  Number = 'Number',
  Boolean = 'Boolean',
  Json = 'Json',
  Array = 'Array',
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

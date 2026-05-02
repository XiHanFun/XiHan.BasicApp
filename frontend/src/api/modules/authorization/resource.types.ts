import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

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

export enum FieldMaskStrategy {
  None = 0,
  Hidden = 1,
  FullMask = 2,
  PartialMask = 3,
  Hash = 4,
  Redact = 5,
  Custom = 99,
}

export enum FieldSecurityTargetType {
  Role = 0,
  User = 1,
  Permission = 2,
  Department = 3,
}

export enum ConditionOperator {
  Equals = 0,
  NotEquals = 1,
  GreaterThan = 2,
  GreaterThanOrEquals = 3,
  LessThan = 4,
  LessThanOrEquals = 5,
  Contains = 6,
  NotContains = 7,
  In = 8,
  NotIn = 9,
  Between = 10,
  StartsWith = 11,
  EndsWith = 12,
  IsNull = 13,
  IsNotNull = 14,
}

export enum ConfigDataType {
  String = 0,
  Number = 1,
  Boolean = 2,
  Json = 3,
  Array = 4,
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

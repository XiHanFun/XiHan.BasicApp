import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString } from '../../types'
import type { ValidityStatus } from '../shared'
import type { ConditionOperator, ConfigDataType } from './resource.types'

export interface PermissionConditionListItemDto extends BasicDto {
  attributeName: string
  conditionGroup: number
  conditionValue: string
  createdTime: DateTimeString
  description?: string | null
  isNegated: boolean
  operator: ConditionOperator
  permissionCode?: string | null
  permissionId?: ApiId | null
  permissionName?: string | null
  remark?: string | null
  roleCode?: string | null
  roleId?: ApiId | null
  roleName?: string | null
  rolePermissionId?: ApiId | null
  status: ValidityStatus
  userDisplayName?: string | null
  userId?: ApiId | null
  userPermissionId?: ApiId | null
  valueType: ConfigDataType
}

export interface PermissionConditionDetailDto extends PermissionConditionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  modifiedTime?: DateTimeString | null
}

export interface PermissionConditionCreateDto {
  attributeName: string
  conditionGroup: number
  conditionValue: string
  description?: string | null
  isNegated: boolean
  operator: ConditionOperator
  remark?: string | null
  rolePermissionId?: ApiId | null
  status: ValidityStatus
  userPermissionId?: ApiId | null
  valueType: ConfigDataType
}

export interface PermissionConditionUpdateDto extends BasicUpdateDto {
  attributeName: string
  conditionGroup: number
  conditionValue: string
  description?: string | null
  isNegated: boolean
  operator: ConditionOperator
  remark?: string | null
  rolePermissionId?: ApiId | null
  userPermissionId?: ApiId | null
  valueType: ConfigDataType
}

export interface PermissionConditionStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: ValidityStatus
}

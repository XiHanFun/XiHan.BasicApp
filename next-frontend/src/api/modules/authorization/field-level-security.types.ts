import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'
import type { FieldMaskStrategy, FieldSecurityTargetType, ResourceType } from './resource.types'

export interface FieldLevelSecurityPageQueryDto extends PageRequest {
  keyword?: string | null
  maskStrategy?: FieldMaskStrategy | null
  resourceId?: ApiId | null
  status?: EnableStatus | null
  targetId?: ApiId | null
  targetType?: FieldSecurityTargetType | null
}

export interface FieldLevelSecurityListItemDto extends BasicDto {
  createdTime: DateTimeString
  description?: string | null
  fieldName: string
  isEditable: boolean
  isReadable: boolean
  maskStrategy: FieldMaskStrategy
  modifiedTime?: DateTimeString | null
  priority: number
  resourceCode?: string | null
  resourceId: ApiId
  resourceName?: string | null
  resourceType?: ResourceType | null
  status: EnableStatus
  targetCode?: string | null
  targetId: ApiId
  targetName?: string | null
  targetType: FieldSecurityTargetType
}

export interface FieldLevelSecurityDetailDto extends FieldLevelSecurityListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  maskPattern?: string | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface FieldLevelSecurityCreateDto {
  description?: string | null
  fieldName: string
  isEditable: boolean
  isReadable: boolean
  maskPattern?: string | null
  maskStrategy: FieldMaskStrategy
  priority: number
  remark?: string | null
  resourceId: ApiId
  status: EnableStatus
  targetId: ApiId
  targetType: FieldSecurityTargetType
}

export interface FieldLevelSecurityUpdateDto extends BasicUpdateDto {
  description?: string | null
  fieldName: string
  isEditable: boolean
  isReadable: boolean
  maskPattern?: string | null
  maskStrategy: FieldMaskStrategy
  priority: number
  remark?: string | null
  resourceId: ApiId
  targetId: ApiId
  targetType: FieldSecurityTargetType
}

export interface FieldLevelSecurityStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: EnableStatus
}

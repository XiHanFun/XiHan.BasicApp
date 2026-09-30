import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'
import type { FieldMaskStrategy, FieldSecurityTargetType } from './resource.types'

export interface FieldLevelSecurityPageQueryDto extends PageRequest {
  entityName?: string | null
  keyword?: string | null
  maskStrategy?: FieldMaskStrategy | null
  status?: EnableStatus | null
  targetId?: ApiId | null
  targetType?: FieldSecurityTargetType | null
}

export interface FieldLevelSecurityListItemDto extends BasicDto {
  createdTime: DateTimeString
  /** 实体已不再支持字段安全时为空 */
  entityDisplayName?: string | null
  entityName: string
  /** 字段已不存在时为空 */
  fieldDisplayName?: string | null
  fieldName: string
  isEditable: boolean
  /** 平台规则：对所有租户生效，只能在平台维护 */
  isGlobal: boolean
  maskKeepHead?: number | null
  maskKeepTail?: number | null
  maskReplacement?: string | null
  maskStrategy: FieldMaskStrategy
  modifiedTime?: DateTimeString | null
  remark?: string | null
  status: EnableStatus
  targetCode?: string | null
  targetId: ApiId
  targetName?: string | null
  targetType: FieldSecurityTargetType
}

export interface FieldLevelSecurityDetailDto extends FieldLevelSecurityListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
}

export interface FieldLevelSecurityCreateDto {
  entityName: string
  fieldName: string
  isEditable: boolean
  maskKeepHead?: number | null
  maskKeepTail?: number | null
  maskReplacement?: string | null
  maskStrategy: FieldMaskStrategy
  remark?: string | null
  status: EnableStatus
  targetId: ApiId
  targetType: FieldSecurityTargetType
}

export interface FieldLevelSecurityUpdateDto extends BasicUpdateDto {
  entityName: string
  fieldName: string
  isEditable: boolean
  maskKeepHead?: number | null
  maskKeepTail?: number | null
  maskReplacement?: string | null
  maskStrategy: FieldMaskStrategy
  remark?: string | null
  targetId: ApiId
  targetType: FieldSecurityTargetType
}

export interface FieldLevelSecurityStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: EnableStatus
}

/** 可配置字段安全的字段 */
export interface FieldSecurityFieldDto {
  displayName: string
  fieldName: string
  /** 非文本字段只能明文只读或隐藏 */
  isText: boolean
}

/** 可配置字段安全的实体 */
export interface FieldSecurityEntityDto {
  displayName: string
  entityName: string
  fields: FieldSecurityFieldDto[]
}

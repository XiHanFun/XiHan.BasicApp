// 字典网络契约：标签样式通过原字典接口传递，业务编码保持不变。
import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'
import type { DictTagStyle } from '~/types/dictionary'

export { EnableStatus } from '../shared'

export interface DictPageQueryDto extends PageRequest {
  dictCode?: string | null
  dictType?: string | null
  isBuiltIn?: boolean | null
  keyword?: string | null
  status?: EnableStatus | null
}

export interface DictListItemDto extends BasicDto {
  createdTime: DateTimeString
  dictCode: string
  dictDescription?: string | null
  dictName: string
  dictType: string
  hasNote: boolean
  isBuiltIn: boolean
  /** 全局字典：平台维护，租户只读 */
  isGlobal: boolean
  modifiedTime?: DateTimeString | null
  sort: number
  status: EnableStatus
}

export interface DictDetailDto extends DictListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface DictItemPageQueryDto extends PageRequest {
  dictId?: ApiId | null
  isDefault?: boolean | null
  itemCode?: string | null
  keyword?: string | null
  parentId?: ApiId | null
  status?: EnableStatus | null
}

export interface DictItemListItemDto extends BasicDto {
  editVersion?: string
  tagStyle?: DictTagStyle | null
  createdTime: DateTimeString
  dictId: ApiId
  hasExtension: boolean
  hasNote: boolean
  isDefault: boolean
  itemCode: string
  itemDescription?: string | null
  itemName: string
  itemValue?: string | null
  modifiedTime?: DateTimeString | null
  parentId?: ApiId | null
  sort: number
  status: EnableStatus
}

export interface DictItemDetailDto extends DictItemListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  metadata?: string | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface DictItemTreeQueryDto {
  dictId: ApiId
  limit: number
  onlyEnabled: boolean
}

/** 字典选项（业务表单下拉；值为字典项编码，树形字典按深度优先展平） */
export interface DictOptionDto {
  description?: string | null
  tagStyle?: DictTagStyle | null
  disabled: boolean
  isDefault: boolean
  label: string
  parentValue?: string | null
  value: string
}

export interface DictItemTreeNodeDto extends BasicDto {
  tagStyle?: DictTagStyle | null
  children: DictItemTreeNodeDto[]
  dictId: ApiId
  isDefault: boolean
  itemCode: string
  itemDescription?: string | null
  itemName: string
  itemValue?: string | null
  parentId?: ApiId | null
  sort: number
  status: EnableStatus
}

export interface DictCreateDto {
  dictCode: string
  dictDescription?: string | null
  dictName: string
  dictType: string
  sort: number
  status: EnableStatus
}

export interface DictUpdateDto extends BasicDto {
  dictDescription?: string | null
  dictName: string
  dictType: string
  remark?: string | null
  sort: number
}

export interface DictStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: EnableStatus
}

export interface DictItemCreateDto {
  tagStyle?: DictTagStyle | null
  dictId: ApiId
  isDefault: boolean
  itemCode: string
  itemDescription?: string | null
  itemName: string
  itemValue?: string | null
  parentId?: ApiId | null
  sort: number
  status: EnableStatus
}

export interface DictItemUpdateDto extends BasicDto {
  expectedVersion?: string
  tagStyle?: DictTagStyle | null
  isDefault: boolean
  itemDescription?: string | null
  itemName: string
  itemValue?: string | null
  metadata?: string | null
  parentId?: ApiId | null
  remark?: string | null
  sort: number
}

export interface DictItemStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: EnableStatus
}

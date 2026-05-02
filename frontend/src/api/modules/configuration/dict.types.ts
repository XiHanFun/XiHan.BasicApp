import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

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
  modifiedTime?: DateTimeString | null
  sort: number
  status: EnableStatus
}

export interface DictDetailDto extends DictListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
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
  modifiedBy?: string | null
  modifiedId?: ApiId | null
}

export interface DictItemTreeQueryDto {
  dictId: ApiId
  limit: number
  onlyEnabled: boolean
}

export interface DictItemTreeNodeDto extends BasicDto {
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

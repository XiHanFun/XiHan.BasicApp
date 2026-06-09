import type { ApiId, BasicCreateDto, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export interface TenantEditionPageQueryDto extends PageRequest {
  isDefault?: boolean | null
  isFree?: boolean | null
  keyword?: string | null
  status?: EnableStatus | null
}

export interface TenantEditionListItemDto extends BasicDto {
  billingPeriodMonths?: number | null
  createdTime: DateTimeString
  description?: string | null
  editionCode: string
  editionName: string
  isDefault: boolean
  isFree: boolean
  modifiedTime?: DateTimeString | null
  price?: number | null
  sort: number
  status: EnableStatus
  storageLimit?: number | null
  userLimit?: number | null
}

export interface TenantEditionDetailDto extends TenantEditionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface TenantEditionCreateDto extends BasicCreateDto {
  billingPeriodMonths?: number | null
  description?: string | null
  editionCode: string
  editionName: string
  isDefault: boolean
  isFree: boolean
  price?: number | null
  remark?: string | null
  sort: number
  status: EnableStatus
  storageLimit?: number | null
  userLimit?: number | null
}

export interface TenantEditionUpdateDto extends BasicUpdateDto {
  billingPeriodMonths?: number | null
  description?: string | null
  editionName: string
  isDefault: boolean
  isFree: boolean
  price?: number | null
  remark?: string | null
  sort: number
  storageLimit?: number | null
  userLimit?: number | null
}

export interface TenantEditionStatusUpdateDto extends BasicDto {
  status: EnableStatus
}

export interface TenantEditionDefaultUpdateDto extends BasicDto {}

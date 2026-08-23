import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export interface PositionPageQueryDto extends PageRequest {
  keyword?: string | null
  status?: EnableStatus | null
}

export interface PositionListItemDto extends BasicDto {
  createdTime: DateTimeString
  modifiedTime?: DateTimeString | null
  positionCode: string
  positionName: string
  sort: number
  status: EnableStatus
}

export interface PositionDetailDto extends PositionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface PositionCreateDto {
  positionCode: string
  positionName: string
  remark?: string | null
  sort: number
  status: EnableStatus
}

export interface PositionUpdateDto extends BasicUpdateDto {
  positionName: string
  remark?: string | null
  sort: number
}

export interface PositionStatusUpdateDto extends BasicDto {
  remark?: string | null
  status: EnableStatus
}

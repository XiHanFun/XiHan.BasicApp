import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString } from '../../types'
import type { EnableStatus, ValidityStatus } from '../shared'
import type { DepartmentType } from './department.types'

export interface UserDepartmentListItemDto extends BasicDto {
  createdTime: DateTimeString
  departmentCode?: string | null
  departmentId: ApiId
  departmentName?: string | null
  departmentStatus?: EnableStatus | null
  departmentType?: DepartmentType | null
  isMain: boolean
  parentId?: ApiId | null
  remark?: string | null
  status: ValidityStatus
  userId: ApiId
}

export interface UserDepartmentAssignDto {
  userId: ApiId
  departmentId: ApiId
  isMain: boolean
  remark?: string | null
}

export interface UserDepartmentUpdateDto extends BasicUpdateDto {
  isMain: boolean
  remark?: string | null
}

export interface UserDepartmentStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: ValidityStatus
}

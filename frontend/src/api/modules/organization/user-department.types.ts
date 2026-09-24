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
  jobLevel?: string | null
  jobNumber?: string | null
  joinTime?: DateTimeString | null
  parentId?: ApiId | null
  positionId?: ApiId | null
  positionName?: string | null
  remark?: string | null
  status: ValidityStatus
  userId: ApiId
}

export interface UserDepartmentBatchAssignItemDto {
  departmentId: ApiId
  /** 设为主部门（一次至多一项）；都不设时后端沿用原主部门，没有则由最早的归属接任 */
  isMain: boolean
  jobLevel?: string | null
  jobNumber?: string | null
  joinTime?: DateTimeString | null
  positionId?: ApiId | null
  remark?: string | null
}

/** 批量变更用户部门归属（一次性提交分配与撤销）；已有效的部门再次下发只参与主部门调整 */
export interface UserDepartmentBatchUpdateDto {
  assigns: UserDepartmentBatchAssignItemDto[]
  revokeUserDepartmentIds: ApiId[]
  userId: ApiId
}

export interface UserDepartmentUpdateDto extends BasicUpdateDto {
  isMain: boolean
  jobLevel?: string | null
  jobNumber?: string | null
  joinTime?: DateTimeString | null
  positionId?: ApiId | null
  remark?: string | null
}

export interface UserDepartmentStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: ValidityStatus
}

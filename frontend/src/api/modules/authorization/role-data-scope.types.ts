import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString } from '../../types'
import type { EnableStatus, ValidityStatus } from '../shared'
import type { DepartmentType } from '../organization'

export interface RoleDataScopeListItemDto extends BasicDto {
  createdTime: DateTimeString
  departmentCode?: string | null
  departmentId: ApiId
  departmentName?: string | null
  departmentStatus?: EnableStatus | null
  departmentType?: DepartmentType | null
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  includeChildren: boolean
  parentId?: ApiId | null
  remark?: string | null
  roleId: ApiId
  status: ValidityStatus
}

export interface RoleDataScopeDetailDto extends RoleDataScopeListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}

export interface RoleDataScopeGrantDto {
  departmentId: ApiId
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  includeChildren: boolean
  remark?: string | null
  roleId: ApiId
}

export interface RoleDataScopeUpdateDto extends BasicUpdateDto {
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  includeChildren: boolean
  remark?: string | null
}

export interface RoleDataScopeStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: ValidityStatus
}

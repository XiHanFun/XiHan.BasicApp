import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString } from '../../types'
import type { DepartmentType } from '../organization'
import type { EnableStatus, ValidityStatus } from '../shared'

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

export interface RoleDataScopeBatchGrantItemDto {
  departmentId: ApiId
  includeChildren: boolean
}

/** 批量变更角色数据范围（一次性提交授予与撤销）；已授予的部门再次下发即改其含下级 */
export interface RoleDataScopeBatchUpdateDto {
  grants: RoleDataScopeBatchGrantItemDto[]
  revokeRoleDataScopeIds: ApiId[]
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

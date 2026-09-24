import type { ApiId, BasicDto, DateTimeString } from '../../types'
import type { DepartmentType } from '../organization'
import type { EnableStatus, ValidityStatus } from '../shared'
import type { DataPermissionScope } from './role.types'

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

/** 自定义数据范围里的一个部门 */
export interface DataScopeDepartmentDto {
  departmentId: ApiId
  includeChildren: boolean
}

/** 设置角色数据范围：档位与自定义部门一次提交（全局角色不能自定义） */
export interface RoleDataScopeSetDto {
  dataScope: DataPermissionScope
  /** 仅档位为 Custom 时提交，且至少一个 */
  departments: DataScopeDepartmentDto[]
  roleId: ApiId
}

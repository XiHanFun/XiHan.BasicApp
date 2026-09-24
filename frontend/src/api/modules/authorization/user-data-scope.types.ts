import type { ApiId, BasicDto, DateTimeString } from '../../types'
import type { DepartmentType } from '../organization'
import type { EnableStatus, ValidityStatus } from '../shared'
import type { TenantMemberInviteStatus, TenantMemberType } from '../tenant'
import type { DataScopeDepartmentDto } from './role-data-scope.types'
import type { DataPermissionScope } from './role.types'

export interface UserDataScopeListItemDto extends BasicDto {
  createdTime: DateTimeString
  departmentCode?: string | null
  departmentId: ApiId
  departmentName?: string | null
  departmentStatus?: EnableStatus | null
  departmentType?: DepartmentType | null
  includeChildren: boolean
  parentId?: ApiId | null
  remark?: string | null
  status: ValidityStatus
  tenantMemberDisplayName?: string | null
  tenantMemberId?: ApiId | null
  tenantMemberInviteStatus?: TenantMemberInviteStatus | null
  tenantMemberStatus?: ValidityStatus | null
  tenantMemberType?: TenantMemberType | null
  userId: ApiId
}

export interface UserDataScopeDetailDto extends UserDataScopeListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}

/** 设置成员在本租户的数据范围：覆盖档位与自定义部门一次提交 */
export interface UserDataScopeSetDto {
  /** null 表示跟随角色 */
  dataScope: DataPermissionScope | null
  /** 仅档位为 Custom 时提交，且至少一个 */
  departments: DataScopeDepartmentDto[]
  userId: ApiId
}

/** 成员在本租户的数据范围设置（与设置 DTO 同形，供编辑回显） */
export interface UserDataScopeSettingDto {
  /** null 表示跟随角色 */
  dataScope: DataPermissionScope | null
  /** 当前生效的自定义部门 */
  departments: UserDataScopeListItemDto[]
  userId: ApiId
}

import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString } from '../../types'
import type { DepartmentType } from '../organization'
import type { EnableStatus, ValidityStatus } from '../shared'
import type { TenantMemberInviteStatus, TenantMemberType } from '../tenant'

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

export interface UserDataScopeBatchGrantItemDto {
  departmentId: ApiId
  includeChildren: boolean
}

/** 批量变更用户数据范围（一次性提交授予与撤销）；已授予的部门再次下发即改其含下级 */
export interface UserDataScopeBatchUpdateDto {
  grants: UserDataScopeBatchGrantItemDto[]
  revokeUserDataScopeIds: ApiId[]
  userId: ApiId
}

export interface UserDataScopeUpdateDto extends BasicUpdateDto {
  includeChildren: boolean
  remark?: string | null
}

export interface UserDataScopeStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: ValidityStatus
}

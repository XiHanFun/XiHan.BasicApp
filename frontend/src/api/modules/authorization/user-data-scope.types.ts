import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString } from '../../types'
import type { EnableStatus, ValidityStatus } from '../shared'
import type { DepartmentType } from '../organization'
import type { TenantMemberInviteStatus, TenantMemberType } from '../tenant'
import type { DataPermissionScope } from './role.types'

export interface UserDataScopeListItemDto extends BasicDto {
  createdTime: DateTimeString
  dataScope: DataPermissionScope
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

export interface UserDataScopeGrantDto {
  dataScope: DataPermissionScope
  departmentId?: ApiId | null
  includeChildren: boolean
  remark?: string | null
  userId: ApiId
}

export interface UserDataScopeUpdateDto extends BasicUpdateDto {
  dataScope: DataPermissionScope
  departmentId?: ApiId | null
  includeChildren: boolean
  remark?: string | null
}

export interface UserDataScopeStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: ValidityStatus
}

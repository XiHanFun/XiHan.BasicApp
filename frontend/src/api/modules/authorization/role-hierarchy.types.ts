import type { ApiId, BasicDto, DateTimeString } from '../../types'
import type { EnableStatus } from '../shared'
import type { RoleType } from './role.types'

export interface RoleHierarchyListItemDto extends BasicDto {
  ancestorId: ApiId
  ancestorRoleCode?: string | null
  ancestorRoleName?: string | null
  ancestorRoleType?: RoleType | null
  ancestorStatus?: EnableStatus | null
  createdTime: DateTimeString
  depth: number
  descendantId: ApiId
  descendantRoleCode?: string | null
  descendantRoleName?: string | null
  descendantRoleType?: RoleType | null
  descendantStatus?: EnableStatus | null
  isAncestorGlobal?: boolean | null
  isDescendantGlobal?: boolean | null
  path?: string | null
  remark?: string | null
}

export interface RoleHierarchyDetailDto extends RoleHierarchyListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}

/** 批量变更角色的直接父角色（一次性提交新增与移除） */
export interface RoleHierarchyBatchUpdateDto {
  addParentRoleIds: ApiId[]
  removeParentRoleIds: ApiId[]
  roleId: ApiId
}

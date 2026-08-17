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

export interface RoleHierarchyCreateDto {
  ancestorId: ApiId
  descendantId: ApiId
  remark?: string | null
}

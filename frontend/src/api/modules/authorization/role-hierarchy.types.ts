import type { ApiId } from '../../types'
import type { EnableStatus } from '../shared'
import type { PermissionAction } from './role-permission.types'
import type { RoleType } from './role.types'

/** 继承链上的一个角色：上级链里是本角色的上级，下级链里是本角色的下级 */
export interface RoleInheritanceItemDto {
  depth: number
  isEffective: boolean
  isGlobal: boolean
  /** 继承路径上的角色名称，按「上级 → 下级」排列，含两端 */
  pathRoleNames: string[]
  roleCode: string
  roleId: ApiId
  roleName: string
  roleType: RoleType
  status: EnableStatus
}

/** 角色从生效的上级继承来的一条权限绑定；同一权限来自多个上级时逐条列出 */
export interface RoleInheritedPermissionDto {
  depth: number
  permissionAction: PermissionAction
  permissionCode?: string | null
  permissionId: ApiId
  permissionName?: string | null
  sourceRoleCode?: string | null
  sourceRoleId: ApiId
  sourceRoleName?: string | null
}

/** 批量变更角色的直接上级（一次提交新增与解除） */
export interface RoleHierarchyBatchUpdateDto {
  addParentRoleIds: ApiId[]
  removeParentRoleIds: ApiId[]
  roleId: ApiId
}

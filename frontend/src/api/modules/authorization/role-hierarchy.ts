import type { ApiId } from '../../types'
import type {
  RoleHierarchyBatchUpdateDto,
  RoleInheritanceItemDto,
  RoleInheritedPermissionDto,
} from './role-hierarchy.types'
import { createDynamicApiClient } from '../../base'

const roleHierarchyQueryApi = createDynamicApiClient('RoleHierarchyQuery')
const roleHierarchyCommandApi = createDynamicApiClient('Role')

export const roleHierarchyApi = {
  /** 角色的全部上级（不含自身），按继承深度排列 */
  ancestors(roleId: ApiId) {
    return roleHierarchyQueryApi.get<RoleInheritanceItemDto[]>('RoleAncestors', { roleId })
  },
  /** 一次提交本角色直接上级的新增与解除（单事务，先解除后新增） */
  batchUpdateParents(input: RoleHierarchyBatchUpdateDto) {
    return roleHierarchyCommandApi.post<void, RoleHierarchyBatchUpdateDto>('BatchUpdateRoleParents', input)
  },
  /** 角色的全部下级（不含自身），按继承深度排列 */
  descendants(roleId: ApiId) {
    return roleHierarchyQueryApi.get<RoleInheritanceItemDto[]>('RoleDescendants', { roleId })
  },
  /** 角色从生效的上级继承来的权限绑定 */
  inheritedPermissions(roleId: ApiId) {
    return roleHierarchyQueryApi.get<RoleInheritedPermissionDto[]>('RoleInheritedPermissions', { roleId })
  },
}

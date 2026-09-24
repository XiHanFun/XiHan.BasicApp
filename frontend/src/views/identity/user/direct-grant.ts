import type { ApiId, PermissionAction, UserPermissionBatchGrantItemDto } from '@/api'

/** 当前生效的一条角色直授：记录主键用于撤销，角色主键用于比对 */
export interface RoleGrantRow {
  basicId: ApiId
  roleId: ApiId
}

/** 当前生效的一条权限直授 */
export interface PermissionGrantRow {
  basicId: ApiId
  permissionAction: PermissionAction
  permissionId: ApiId
}

/**
 * 角色直授的差量：右栏（或表单勾选）与当前生效的授予比对。
 * 授予给角色主键，撤销给记录主键——后端撤销按记录走，只认本用户名下的有效记录
 */
export function diffRoleGrants(selected: readonly ApiId[], current: readonly RoleGrantRow[]) {
  const selectedSet = new Set(selected)
  const currentRoleIds = new Set(current.map(row => row.roleId))
  return {
    grantRoleIds: selected.filter(roleId => !currentRoleIds.has(roleId)),
    revokeUserRoleIds: current.filter(row => !selectedSet.has(row.roleId)).map(row => row.basicId),
  }
}

/**
 * 某个动作的右栏变成 next：移出本栏的回到未设置，移入本栏的落为该动作。
 * 授予与拒绝互斥——原在另一栏的权限挪进来，就从另一栏移走
 */
export function applyPermissionTransfer(
  actions: ReadonlyMap<ApiId, PermissionAction>,
  action: PermissionAction,
  next: readonly ApiId[],
): Map<ApiId, PermissionAction> {
  const keep = new Set(next)
  const result = new Map(actions)
  for (const [permissionId, value] of result) {
    if (value === action && !keep.has(permissionId)) {
      result.delete(permissionId)
    }
  }
  for (const permissionId of keep) {
    result.set(permissionId, action)
  }
  return result
}

/**
 * 权限直授的差量：新增或改了动作的才下发，动作没变的不重复提交；
 * 草稿里已没有的当前授予按记录主键撤销
 */
export function diffPermissionGrants(
  actions: ReadonlyMap<ApiId, PermissionAction>,
  current: readonly PermissionGrantRow[],
) {
  const currentById = new Map(current.map(row => [row.permissionId, row] as const))
  const grants: UserPermissionBatchGrantItemDto[] = [...actions]
    .filter(([permissionId, action]) => currentById.get(permissionId)?.permissionAction !== action)
    .map(([permissionId, permissionAction]) => ({ permissionId, permissionAction }))
  const revokeUserPermissionIds = current
    .filter(row => !actions.has(row.permissionId))
    .map(row => row.basicId)
  return { grants, revokeUserPermissionIds }
}

import type { ApiId, PermissionListItemDto, RoleInheritedPermissionDto, RolePermissionListItemDto } from '@/api'
import type { PermissionGrantItem } from '~/components/common/permission-grant-panel'
import { PermissionAction, ValidityStatus } from '@/api'

/** 权限穿梭框条目：目录项，或已授予却不在目录里的授权 */
export type RolePermissionItem = PermissionGrantItem & Partial<Pick<PermissionListItemDto, 'side'>> & { basicId: ApiId }

/** 撤销是软删除（Status=Invalid），列表含历史行；比对与回显只认有效授权 */
export function validRoleGrants(grants: readonly RolePermissionListItemDto[]) {
  return grants.filter(grant => grant.status === ValidityStatus.Valid)
}

/**
 * 权限目录再补上已授予却不在目录里的（例如业务租户的目录不列平台侧权限）。
 * 穿梭框只认条目里有的键，不补进来，一动穿梭框这些授权就从草稿里掉出去，保存时被当成收回
 */
export function mergeGrantedIntoCatalog(
  catalog: readonly PermissionListItemDto[],
  grants: readonly RolePermissionListItemDto[],
): RolePermissionItem[] {
  const known = new Set(catalog.map(permission => permission.basicId))
  const extra = validRoleGrants(grants)
    .filter(grant => !known.has(grant.permissionId))
    .map(grant => ({
      basicId: grant.permissionId,
      permissionCode: grant.permissionCode ?? '',
      permissionName: grant.permissionName ?? String(grant.permissionId),
      moduleCode: grant.moduleCode,
    }))
  return [...catalog, ...extra]
}

/**
 * 菜单授权的差量：勾中菜单的关联权限为目标集，只在菜单树覆盖到的权限范围内比对。
 * 没挂在任何菜单上的权限（撤销类、接口类）在菜单树里看不到，归「权限分配」管，这里不能顺手收回
 */
export function diffMenuGrants(
  checkedMenuIds: readonly ApiId[],
  menuPermIdById: ReadonlyMap<ApiId, ApiId>,
  grants: readonly RolePermissionListItemDto[],
) {
  const checked = new Set(checkedMenuIds.map(String))
  const target = new Set<ApiId>()
  for (const [menuId, permId] of menuPermIdById) {
    if (checked.has(String(menuId))) {
      target.add(permId)
    }
  }
  const managed = new Set(menuPermIdById.values())
  const valid = validRoleGrants(grants)
  const granted = new Set(valid.map(grant => grant.permissionId))
  return {
    grantPermissionIds: [...target].filter(permId => !granted.has(permId)),
    revokeRolePermissionIds: valid
      .filter(grant => managed.has(grant.permissionId) && !target.has(grant.permissionId))
      .map(grant => grant.basicId),
  }
}

/** 本角色从上级继承的一条权限：哪些上级授予、哪些上级拒绝 */
export interface InheritedPermissionSources {
  grantedBy: string[]
  deniedBy: string[]
}

/**
 * 继承来的绑定按权限汇总，键为权限主键的字符串形式。
 * 链上任一上级拒绝，本角色就拿不到该权限，即使本角色自己授予了它
 */
export function summarizeInheritedPermissions(items: readonly RoleInheritedPermissionDto[]) {
  const summary = new Map<string, InheritedPermissionSources>()
  for (const item of items) {
    const key = String(item.permissionId)
    const entry = summary.get(key) ?? { grantedBy: [], deniedBy: [] }
    const source = item.sourceRoleName || item.sourceRoleCode || String(item.sourceRoleId)
    const sources = item.permissionAction === PermissionAction.Deny ? entry.deniedBy : entry.grantedBy
    if (!sources.includes(source)) {
      sources.push(source)
    }
    summary.set(key, entry)
  }
  return summary
}

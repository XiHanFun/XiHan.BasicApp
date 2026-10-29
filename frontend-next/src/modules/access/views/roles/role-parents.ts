import type { ResponseOf } from '~/contracts'

export type RoleHierarchy = ResponseOf<'roles.hierarchy'>
export type RoleRelative = RoleHierarchy['ancestors'][number]
export type InheritedPermission = RoleHierarchy['inherited'][number]
type RoleCandidate = Pick<ResponseOf<'roles.list'>['items'][number], 'id' | 'code' | 'isBuiltIn' | 'name' | 'status'>

/** 上级角色候选项：value 为角色主键 */
export interface RoleParentOption {
  value: string
  label: string
  description?: string
  disabled?: boolean
}

/** 候选项说明里的补充文字 */
export interface RoleParentOptionNotes {
  /** 已是本角色的下级：设为上级会形成环路 */
  descendant: string
  /** 已经间接继承：再直接继承是多余的 */
  indirectAncestor: string
  /** 现有直接上级已停用：不再传下授权，可以解除 */
  disabledParent: string
}

/**
 * 上级角色候选：启用的非内置角色并上现有直接上级，去掉自己；下级与间接上级列出但不可选，说明原因。
 *
 * 候选只含启用的角色，停用的现有直接上级不在其中；不并进来它就会在选择框里「消失」，保存时被当成解除。
 */
export function buildRoleParentOptions(
  roleId: string,
  candidates: readonly RoleCandidate[],
  ancestors: readonly RoleRelative[],
  descendants: readonly RoleRelative[],
  notes: RoleParentOptionNotes,
): RoleParentOption[] {
  const directParents = new Map(ancestors.filter(item => item.depth === 1).map(item => [item.roleId, item] as const))
  const indirectAncestors = new Set(ancestors.filter(item => item.depth > 1).map(item => item.roleId))
  const descendantIds = new Set(descendants.map(item => item.roleId))

  const options: RoleParentOption[] = []
  const seen = new Set<string>([roleId])
  for (const role of candidates) {
    if (seen.has(role.id) || role.isBuiltIn || role.status !== 'Active')
      continue
    seen.add(role.id)
    const blocked = directParents.has(role.id)
      ? undefined
      : descendantIds.has(role.id)
        ? notes.descendant
        : indirectAncestors.has(role.id) ? notes.indirectAncestor : undefined
    options.push({
      value: role.id,
      label: role.name,
      description: blocked ? `${role.code} · ${blocked}` : role.code,
      ...(blocked ? { disabled: true } : {}),
    })
  }

  for (const [value, parent] of directParents) {
    if (seen.has(value))
      continue
    seen.add(value)
    const note = parent.status === 'Active' ? undefined : notes.disabledParent
    options.push({
      value,
      label: parent.name,
      description: note ? `${parent.code} · ${note}` : parent.code,
    })
  }

  return options
}

/** 现有直接上级 */
export function directParentIds(ancestors: readonly RoleRelative[]): string[] {
  return ancestors.filter(item => item.depth === 1).map(item => item.roleId)
}

/** 选择结果与现有直接上级是否相同（顺序无关） */
export function sameParents(current: readonly string[], selected: readonly string[]): boolean {
  const before = new Set(current)
  const after = new Set(selected)
  return before.size === after.size && [...after].every(id => before.has(id))
}

/** 一个权限从上级继承来的来源：授予它的上级、拒绝它的上级 */
export interface InheritedPermissionSources {
  grantedBy: string[]
  deniedBy: string[]
}

/** 继承来的授予与拒绝按权限码汇总来源上级的名称（同一上级只列一次） */
export function summarizeInheritedPermissions(items: readonly InheritedPermission[]): Map<string, InheritedPermissionSources> {
  const summary = new Map<string, InheritedPermissionSources>()
  for (const item of items) {
    const entry = summary.get(item.permission) ?? { grantedBy: [], deniedBy: [] }
    const sources = item.action === 'Deny' ? entry.deniedBy : entry.grantedBy
    if (!sources.includes(item.sourceRoleName))
      sources.push(item.sourceRoleName)
    summary.set(item.permission, entry)
  }
  return summary
}

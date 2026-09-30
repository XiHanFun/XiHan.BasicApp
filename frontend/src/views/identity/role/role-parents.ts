import type { ApiId, RoleInheritanceItemDto, RoleSelectItemDto } from '@/api'
import { EnableStatus } from '@/api'

/** 上级角色候选项：value 为角色主键的字符串形式（组合框值一律是字符串） */
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
 * 上级角色候选：可选角色并上现有直接上级，去掉自己；下级与间接上级列出但不可选，说明原因。
 *
 * 可选角色只含启用的非系统角色，停用的现有直接上级不在其中；不并进来它就会在选择框里「消失」，保存时被当成解除。
 */
export function buildRoleParentOptions(
  roleId: ApiId,
  candidates: readonly RoleSelectItemDto[],
  ancestors: readonly RoleInheritanceItemDto[],
  descendants: readonly RoleInheritanceItemDto[],
  notes: RoleParentOptionNotes,
): RoleParentOption[] {
  const self = String(roleId)
  const directParents = new Map(
    ancestors
      .filter(item => item.depth === 1)
      .map(item => [String(item.roleId), item] as const),
  )
  const indirectAncestors = new Set(
    ancestors
      .filter(item => item.depth > 1)
      .map(item => String(item.roleId)),
  )
  const descendantIds = new Set(descendants.map(item => String(item.roleId)))

  const options: RoleParentOption[] = []
  const seen = new Set<string>([self])
  for (const role of candidates) {
    const value = String(role.basicId)
    if (seen.has(value)) {
      continue
    }
    seen.add(value)
    const blocked = directParents.has(value)
      ? undefined
      : descendantIds.has(value)
        ? notes.descendant
        : indirectAncestors.has(value) ? notes.indirectAncestor : undefined
    options.push({
      value,
      label: role.roleName,
      description: blocked ? `${role.roleCode} · ${blocked}` : role.roleCode,
      ...(blocked ? { disabled: true } : {}),
    })
  }

  for (const [value, parent] of directParents) {
    if (seen.has(value)) {
      continue
    }
    seen.add(value)
    const note = parent.status === EnableStatus.Enabled ? undefined : notes.disabledParent
    options.push({
      value,
      label: parent.roleName,
      description: note ? `${parent.roleCode} · ${note}` : parent.roleCode,
    })
  }

  return options
}

/** 现有直接上级 */
export function directParentIds(ancestors: readonly RoleInheritanceItemDto[]): string[] {
  return ancestors.filter(item => item.depth === 1).map(item => String(item.roleId))
}

/** 选择结果与现有直接上级比对，得出要新增与要解除的上级 */
export function diffRoleParents(current: readonly string[], selected: readonly string[]) {
  const before = new Set(current)
  const after = new Set(selected)
  return {
    addParentRoleIds: [...after].filter(id => !before.has(id)),
    removeParentRoleIds: [...before].filter(id => !after.has(id)),
  }
}

import { describe, expect, it } from 'vitest'
import { buildRoleParentOptions, directParentIds, sameParents, summarizeInheritedPermissions } from './role-parents'

function candidate(id: string, name: string, extra: Record<string, unknown> = {}) {
  return { id, code: name.toLowerCase(), name, isBuiltIn: false, status: 'Active' as const, ...extra }
}

function relative(roleId: string, name: string, depth: number, extra: Record<string, unknown> = {}) {
  return { roleId, code: name.toLowerCase(), name, status: 'Active' as const, depth, path: [name, 'Self'], isEffective: true, ...extra }
}

const notes = { descendant: 'cycle', indirectAncestor: 'indirect', disabledParent: 'disabled' }

describe('buildRoleParentOptions', () => {
  it('去掉自己与内置、停用的角色；下级与间接上级列出但不可选；停用的现有直接上级补进来以免保存时被当成解除', () => {
    const options = buildRoleParentOptions(
      'self',
      [candidate('self', 'Self'), candidate('a', 'A'), candidate('b', 'B'), candidate('c', 'C'), candidate('admin', 'Admin', { isBuiltIn: true }), candidate('off', 'Off', { status: 'Disabled' })],
      [relative('a', 'A', 1), relative('b', 'B', 2), relative('old', 'Old', 1, { status: 'Disabled' })],
      [relative('c', 'C', 1)],
      notes,
    )
    expect(options).toEqual([
      { value: 'a', label: 'A', description: 'a' },
      { value: 'b', label: 'B', description: 'b · indirect', disabled: true },
      { value: 'c', label: 'C', description: 'c · cycle', disabled: true },
      { value: 'old', label: 'Old', description: 'old · disabled' },
    ])
  })

  it('现有直接上级只取一级；比对只看集合', () => {
    expect(directParentIds([relative('a', 'A', 1), relative('b', 'B', 2)])).toEqual(['a'])
    expect(sameParents(['a', 'b'], ['b', 'a'])).toBe(true)
    expect(sameParents(['a'], ['a', 'b'])).toBe(false)
    expect(sameParents(['a'], [])).toBe(false)
  })
})

describe('summarizeInheritedPermissions', () => {
  it('按权限码汇总授予与拒绝的来源上级，同一上级只列一次', () => {
    const summary = summarizeInheritedPermissions([
      { permission: 'identity.users.read', action: 'Grant', sourceRoleId: 'a', sourceRoleName: 'A', depth: 1 },
      { permission: 'identity.users.read', action: 'Grant', sourceRoleId: 'a', sourceRoleName: 'A', depth: 1 },
      { permission: 'identity.users.read', action: 'Deny', sourceRoleId: 'b', sourceRoleName: 'B', depth: 2 },
    ])
    expect(summary.get('identity.users.read')).toEqual({ grantedBy: ['A'], deniedBy: ['B'] })
    expect(summary.has('identity.users.update')).toBe(false)
  })
})

/**
 * 用户直授的差量计算：决定下发给批量接口的授予与撤销。
 *
 * 算多了会误授，算少了会漏撤；撤销必须给记录主键而不是角色 / 权限主键——
 * 后端按记录撤销，只认本用户名下的有效记录，给错了就是一次静默的空操作。
 */
import { describe, expect, it } from 'vitest'
import { PermissionAction } from '@/api'
import { applyPermissionTransfer, diffPermissionGrants, diffRoleGrants } from './direct-grant'

describe('diffRoleGrants', () => {
  const current = [
    { basicId: 'ur-1', roleId: 'r-1' },
    { basicId: 'ur-2', roleId: 'r-2' },
  ]

  it('新挪进右栏的按角色主键授予，挪出去的按记录主键撤销', () => {
    expect(diffRoleGrants(['r-2', 'r-3'], current)).toEqual({
      grantRoleIds: ['r-3'],
      revokeUserRoleIds: ['ur-1'],
    })
  })

  it('右栏与当前一致时没有差量', () => {
    expect(diffRoleGrants(['r-1', 'r-2'], current)).toEqual({ grantRoleIds: [], revokeUserRoleIds: [] })
  })

  it('调用方先把 current 限定到可选范围，范围外的授予不会被撤', () => {
    const optionIds = new Set(['r-1', 'r-3'])
    const scoped = current.filter(row => optionIds.has(row.roleId))
    expect(diffRoleGrants(['r-3'], scoped)).toEqual({
      grantRoleIds: ['r-3'],
      revokeUserRoleIds: ['ur-1'],
    })
  })
})

describe('applyPermissionTransfer', () => {
  it('挪进授予栏的落为授予，挪出去的回到未设置', () => {
    const before = new Map([['p-1', PermissionAction.Grant], ['p-2', PermissionAction.Grant]])
    const after = applyPermissionTransfer(before, PermissionAction.Grant, ['p-2', 'p-3'])
    expect([...after]).toEqual([['p-2', PermissionAction.Grant], ['p-3', PermissionAction.Grant]])
  })

  it('授予与拒绝互斥：拒绝栏里的挪进授予栏就改为授予，不会两边都在', () => {
    const before = new Map([['p-1', PermissionAction.Deny]])
    const after = applyPermissionTransfer(before, PermissionAction.Grant, ['p-1'])
    expect(after.get('p-1')).toBe(PermissionAction.Grant)
  })

  it('只动当前动作的那一栏：清空授予栏不影响已拒绝的', () => {
    const before = new Map([['p-1', PermissionAction.Grant], ['p-2', PermissionAction.Deny]])
    const after = applyPermissionTransfer(before, PermissionAction.Grant, [])
    expect([...after]).toEqual([['p-2', PermissionAction.Deny]])
  })

  it('返回新表，不改传入的草稿', () => {
    const before = new Map([['p-1', PermissionAction.Grant]])
    applyPermissionTransfer(before, PermissionAction.Grant, [])
    expect(before.get('p-1')).toBe(PermissionAction.Grant)
  })
})

describe('diffPermissionGrants', () => {
  const current = [
    { basicId: 'up-1', permissionId: 'p-1', permissionAction: PermissionAction.Grant },
    { basicId: 'up-2', permissionId: 'p-2', permissionAction: PermissionAction.Deny },
  ]

  it('新增与改了动作的下发，动作没变的不重复提交，草稿里没有的按记录主键撤销', () => {
    const draft = new Map([
      ['p-1', PermissionAction.Deny],
      ['p-3', PermissionAction.Grant],
    ])
    expect(diffPermissionGrants(draft, current)).toEqual({
      grants: [
        { permissionId: 'p-1', permissionAction: PermissionAction.Deny },
        { permissionId: 'p-3', permissionAction: PermissionAction.Grant },
      ],
      revokeUserPermissionIds: ['up-2'],
    })
  })

  it('草稿与当前一致时没有差量', () => {
    const draft = new Map([
      ['p-1', PermissionAction.Grant],
      ['p-2', PermissionAction.Deny],
    ])
    expect(diffPermissionGrants(draft, current)).toEqual({ grants: [], revokeUserPermissionIds: [] })
  })
})

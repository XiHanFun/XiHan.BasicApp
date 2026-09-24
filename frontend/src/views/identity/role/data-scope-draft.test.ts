/**
 * 角色数据范围草稿的差量计算：决定下发给批量接口的授予与撤销。
 *
 * 已授予的部门再次下发即改其含下级，所以「改了含下级」也要算进授予；
 * 撤销必须给记录主键——后端只认本角色名下的有效记录。
 */
import { describe, expect, it } from 'vitest'
import { diffScopeDraft, upsertScopeDraft } from './data-scope-draft'

const current = [
  { basicId: 'rs-1', departmentId: 'd-1', includeChildren: true },
  { basicId: 'rs-2', departmentId: 'd-2', includeChildren: false },
]

describe('diffScopeDraft', () => {
  it('草稿与当前一致时没有差量', () => {
    const draft = current.map(({ departmentId, includeChildren }) => ({ departmentId, includeChildren }))

    expect(diffScopeDraft(draft, current)).toEqual({ grants: [], revokeRoleDataScopeIds: [] })
  })

  it('新部门按部门授予，移走的按记录主键撤销', () => {
    const draft = [
      { departmentId: 'd-1', includeChildren: true },
      { departmentId: 'd-3', includeChildren: false },
    ]

    expect(diffScopeDraft(draft, current)).toEqual({
      grants: [{ departmentId: 'd-3', includeChildren: false }],
      revokeRoleDataScopeIds: ['rs-2'],
    })
  })

  it('只改了含下级的部门也下发为授予，不撤销', () => {
    const draft = [
      { departmentId: 'd-1', includeChildren: false },
      { departmentId: 'd-2', includeChildren: false },
    ]

    expect(diffScopeDraft(draft, current)).toEqual({
      grants: [{ departmentId: 'd-1', includeChildren: false }],
      revokeRoleDataScopeIds: [],
    })
  })
})

describe('upsertScopeDraft', () => {
  it('草稿里没有的部门追加到末尾', () => {
    expect(upsertScopeDraft([{ departmentId: 'd-1', includeChildren: true }], { departmentId: 'd-2', includeChildren: false }))
      .toEqual([
        { departmentId: 'd-1', includeChildren: true },
        { departmentId: 'd-2', includeChildren: false },
      ])
  })

  it('已在草稿里的部门原位改含下级，不重复添加', () => {
    const draft = [
      { departmentId: 'd-1', includeChildren: true },
      { departmentId: 'd-2', includeChildren: true },
    ]

    expect(upsertScopeDraft(draft, { departmentId: 'd-1', includeChildren: false })).toEqual([
      { departmentId: 'd-1', includeChildren: false },
      { departmentId: 'd-2', includeChildren: true },
    ])
  })
})

import type { RoleInheritanceItemDto, RoleSelectItemDto } from '@/api'
import { describe, expect, it } from 'vitest'
import { EnableStatus } from '@/api'
import { buildRoleParentOptions, diffRoleParents, directParentIds } from './role-parents'

const notes = { descendant: '会形成环路', indirectAncestor: '已间接继承', disabledParent: '已停用' }

function role(basicId: string, roleCode: string, roleName: string): RoleSelectItemDto {
  return { basicId, roleCode, roleName } as RoleSelectItemDto
}

function chainItem(roleId: string, depth: number, status = EnableStatus.Enabled): RoleInheritanceItemDto {
  return { roleId, depth, status, roleName: `R${roleId}`, roleCode: `r${roleId}` } as RoleInheritanceItemDto
}

describe('buildRoleParentOptions', () => {
  it('去掉自己；下级与间接上级列出但不可选并说明原因；直接上级可选', () => {
    const options = buildRoleParentOptions(
      '1',
      [role('1', 'self', '自己'), role('2', 'parent', '直接上级'), role('3', 'grand', '间接上级'), role('4', 'child', '下级'), role('5', 'other', '其他')],
      [chainItem('2', 1), chainItem('3', 2)],
      [chainItem('4', 1)],
      notes,
    )

    expect(options).toEqual([
      { value: '2', label: '直接上级', description: 'parent' },
      { value: '3', label: '间接上级', description: 'grand · 已间接继承', disabled: true },
      { value: '4', label: '下级', description: 'child · 会形成环路', disabled: true },
      { value: '5', label: '其他', description: 'other' },
    ])
  })

  it('不在候选里的现有直接上级也并进来，停用的注明，免得保存时被当成解除', () => {
    const options = buildRoleParentOptions('1', [role('5', 'other', '其他')], [chainItem('9', 1, EnableStatus.Disabled)], [], notes)

    expect(options).toEqual([
      { value: '5', label: '其他', description: 'other' },
      { value: '9', label: 'R9', description: 'r9 · 已停用' },
    ])
  })

  it('直接上级同时也经别的上级可达时仍按直接上级处理，可以解除', () => {
    const options = buildRoleParentOptions('1', [role('2', 'parent', '上级')], [chainItem('2', 1)], [], notes)

    expect(options).toEqual([{ value: '2', label: '上级', description: 'parent' }])
  })
})

describe('directParentIds', () => {
  it('只取深度为 1 的上级', () => {
    expect(directParentIds([chainItem('2', 1), chainItem('3', 2), chainItem('4', 1)])).toEqual(['2', '4'])
  })
})

describe('diffRoleParents', () => {
  it('新选的新增，取消的解除，留下的不动', () => {
    expect(diffRoleParents(['2', '3'], ['3', '4'])).toEqual({ addParentRoleIds: ['4'], removeParentRoleIds: ['2'] })
  })

  it('没有变化时两边都为空', () => {
    expect(diffRoleParents(['2'], ['2'])).toEqual({ addParentRoleIds: [], removeParentRoleIds: [] })
  })
})

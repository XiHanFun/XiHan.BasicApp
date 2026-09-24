import type { DepartmentTreeNodeDto } from '@/api'
import { describe, expect, it } from 'vitest'
import { DataPermissionScope } from '@/api'
import {
  buildParentMap,
  findCoveredDepartments,
  isDataScopeComplete,
  isDataScopeDirty,
  setIncludeChildren,
  syncPickedDepartments,
  toDataScopePayload,
} from './data-scope'

function node(basicId: string, children: DepartmentTreeNodeDto[] = []): DepartmentTreeNodeDto {
  return { basicId, departmentName: `部门${basicId}`, children } as unknown as DepartmentTreeNodeDto
}

// 1 ─┬─ 2 ── 3
//    └─ 4
const tree = [node('1', [node('2', [node('3')]), node('4')])]

describe('syncPickedDepartments', () => {
  it('留下的部门保留含下级，新勾选的默认含下级，取消勾选的移走', () => {
    const current = [
      { departmentId: '1', includeChildren: false },
      { departmentId: '2', includeChildren: true },
    ]

    expect(syncPickedDepartments(current, ['1', '4'])).toEqual([
      { departmentId: '1', includeChildren: false },
      { departmentId: '4', includeChildren: true },
    ])
  })
})

describe('setIncludeChildren', () => {
  it('只改目标部门', () => {
    const current = [
      { departmentId: '1', includeChildren: true },
      { departmentId: '2', includeChildren: true },
    ]

    expect(setIncludeChildren(current, '2', false)).toEqual([
      { departmentId: '1', includeChildren: true },
      { departmentId: '2', includeChildren: false },
    ])
  })
})

describe('findCoveredDepartments', () => {
  const parents = buildParentMap(tree)

  it('上级含下级时，已选的下级（含隔代）被它覆盖', () => {
    const covered = findCoveredDepartments([
      { departmentId: '1', includeChildren: true },
      { departmentId: '3', includeChildren: false },
      { departmentId: '4', includeChildren: true },
    ], parents)

    expect(Object.fromEntries(covered)).toEqual({ 3: '1', 4: '1' })
  })

  it('上级只含本级时不覆盖下级，取最近的含下级上级', () => {
    const covered = findCoveredDepartments([
      { departmentId: '1', includeChildren: false },
      { departmentId: '2', includeChildren: true },
      { departmentId: '3', includeChildren: false },
    ], parents)

    expect(Object.fromEntries(covered)).toEqual({ 3: '2' })
  })
})

describe('toDataScopePayload / isDataScopeComplete / isDataScopeDirty', () => {
  const departments = [{ departmentId: '1', includeChildren: true }]

  it('非自定义档位不带部门', () => {
    expect(toDataScopePayload({ dataScope: DataPermissionScope.All, departments })).toEqual({ dataScope: DataPermissionScope.All, departments: [] })
    expect(toDataScopePayload({ dataScope: null, departments })).toEqual({ dataScope: null, departments: [] })
  })

  it('自定义档位至少一个部门才可提交', () => {
    expect(isDataScopeComplete({ dataScope: DataPermissionScope.Custom, departments: [] })).toBe(false)
    expect(isDataScopeComplete({ dataScope: DataPermissionScope.Custom, departments })).toBe(true)
    expect(isDataScopeComplete({ dataScope: null, departments: [] })).toBe(true)
  })

  it('按提交载荷比较：非自定义档位下残留的部门不算改动，部门顺序无关', () => {
    const original = { dataScope: DataPermissionScope.All, departments: [] }

    expect(isDataScopeDirty({ dataScope: DataPermissionScope.All, departments }, original)).toBe(false)
    expect(isDataScopeDirty({ dataScope: DataPermissionScope.SelfOnly, departments: [] }, original)).toBe(true)

    const custom = {
      dataScope: DataPermissionScope.Custom,
      departments: [{ departmentId: '1', includeChildren: true }, { departmentId: '2', includeChildren: false }],
    }
    const reordered = { ...custom, departments: [...custom.departments].reverse() }
    expect(isDataScopeDirty(reordered, custom)).toBe(false)
    expect(isDataScopeDirty({ ...custom, departments: setIncludeChildren(custom.departments, '2', true) }, custom)).toBe(true)
  })
})

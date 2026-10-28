import type { ResponseOf } from '~/contracts'

export type DataScopeValue = ResponseOf<'roles.get'>['role']['dataScope']
export type DepartmentNode = ResponseOf<'departments.tree'>['items'][number]

/** 自定义数据范围里的一个部门：是否连同下级一起 */
export interface ScopeDepartment {
  departmentId: string
  includeChildren: boolean
}

/** 编辑中的数据范围：档位与自定义部门（角色的档位恒有值，6.0 没有「跟随角色」） */
export interface DataScopeDraft {
  dataScope: DataScopeValue
  departments: ScopeDepartment[]
}

/** 成员（用户级覆盖）的数据范围草稿：档位为空即「跟随角色」（4.x 的成员覆盖） */
export interface MemberDataScopeDraft {
  dataScope: DataScopeValue | null
  departments: ScopeDepartment[]
}

/** 档位的展示顺序：从宽到窄，自定义垫底（与 4.x 一致） */
export const DATA_SCOPE_LEVELS: readonly DataScopeValue[] = ['All', 'DepartmentAndChildren', 'DepartmentOnly', 'SelfOnly', 'Custom']

/** 勾选变化后同步部门明细：留下的部门保留各自的含下级，新勾选的默认含下级 */
export function syncPickedDepartments(current: readonly ScopeDepartment[], pickedIds: readonly string[]): ScopeDepartment[] {
  const byId = new Map(current.map(item => [item.departmentId, item]))
  return pickedIds.map(id => byId.get(id) ?? { departmentId: id, includeChildren: true })
}

/** 改某个部门的含下级 */
export function setIncludeChildren(current: readonly ScopeDepartment[], departmentId: string, includeChildren: boolean): ScopeDepartment[] {
  return current.map(item => (item.departmentId === departmentId ? { ...item, includeChildren } : item))
}

/** 部门树拍平成 部门 → 上级 */
export function buildParentMap(nodes: readonly DepartmentNode[], parentId: null | string = null, map = new Map<string, null | string>()): Map<string, null | string> {
  for (const node of nodes) {
    map.set(node.department.id, parentId)
    if (node.children.length > 0)
      buildParentMap(node.children, node.department.id, map)
  }
  return map
}

/** 部门树拍平成 部门 → 名称（详情与已选列表按名称显示） */
export function buildNameMap(nodes: readonly DepartmentNode[], map = new Map<string, string>()): Map<string, string> {
  for (const node of nodes) {
    map.set(node.department.id, node.department.name)
    if (node.children.length > 0)
      buildNameMap(node.children, map)
  }
  return map
}

/**
 * 被上级「含下级」覆盖的部门：部门 → 覆盖它的最近一级已选上级。
 * 覆盖的部门选不选结果都一样，编辑器据此提示，而不是替用户删掉
 */
export function findCoveredDepartments(departments: readonly ScopeDepartment[], parentMap: ReadonlyMap<string, null | string>): Map<string, string> {
  const inclusive = new Set(departments.filter(item => item.includeChildren).map(item => item.departmentId))
  const covered = new Map<string, string>()
  for (const item of departments) {
    let ancestor = parentMap.get(item.departmentId) ?? null
    while (ancestor !== null) {
      if (inclusive.has(ancestor)) {
        covered.set(item.departmentId, ancestor)
        break
      }
      ancestor = parentMap.get(ancestor) ?? null
    }
  }
  return covered
}

/** 提交载荷：只有自定义档位带部门 */
export function toDataScopePayload(draft: DataScopeDraft): { dataScope: DataScopeValue, customDepartments: ScopeDepartment[] } {
  return {
    dataScope: draft.dataScope,
    customDepartments: draft.dataScope === 'Custom'
      ? draft.departments.map(({ departmentId, includeChildren }) => ({ departmentId, includeChildren }))
      : [],
  }
}

/** 自定义档位至少一个部门（后端 ACCESS_VALIDATION_005），其它档位随时可提交 */
export function isDataScopeComplete(draft: DataScopeDraft): boolean {
  return draft.dataScope !== 'Custom' || draft.departments.length > 0
}

/** 草稿与原值是否不同（按提交载荷比较，部门顺序无关） */
export function isDataScopeDirty(draft: DataScopeDraft, original: DataScopeDraft): boolean {
  const next = toDataScopePayload(draft)
  const base = toDataScopePayload(original)
  if (next.dataScope !== base.dataScope || next.customDepartments.length !== base.customDepartments.length)
    return true
  const baseById = new Map(base.customDepartments.map(item => [item.departmentId, item.includeChildren]))
  return next.customDepartments.some(item => baseById.get(item.departmentId) !== item.includeChildren)
}

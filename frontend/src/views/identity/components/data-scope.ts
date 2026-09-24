import type { ApiId, DataScopeDepartmentDto, DepartmentTreeNodeDto } from '@/api'
import { DataPermissionScope } from '@/api'

/** 编辑中的数据范围：档位（null 表示跟随角色，仅成员可选）与自定义部门 */
export interface DataScopeDraft {
  dataScope: DataPermissionScope | null
  departments: DataScopeDepartmentDto[]
}

/** 勾选变化后同步部门明细：留下的部门保留各自的含下级，新勾选的默认含下级 */
export function syncPickedDepartments(
  current: readonly DataScopeDepartmentDto[],
  pickedIds: readonly ApiId[],
): DataScopeDepartmentDto[] {
  const byId = new Map(current.map(item => [item.departmentId, item]))
  return pickedIds.map(id => byId.get(id) ?? { departmentId: id, includeChildren: true })
}

/** 改某个部门的含下级 */
export function setIncludeChildren(
  current: readonly DataScopeDepartmentDto[],
  departmentId: ApiId,
  includeChildren: boolean,
): DataScopeDepartmentDto[] {
  return current.map(item => (item.departmentId === departmentId ? { ...item, includeChildren } : item))
}

/** 部门树拍平成 部门 → 上级 */
export function buildParentMap(nodes: readonly DepartmentTreeNodeDto[], parentId: ApiId | null = null, map = new Map<ApiId, ApiId | null>()) {
  for (const node of nodes) {
    map.set(node.basicId, parentId)
    if (node.children?.length)
      buildParentMap(node.children, node.basicId, map)
  }
  return map
}

/**
 * 被上级「含下级」覆盖的部门：部门 → 覆盖它的最近一级已选上级。
 * 覆盖的部门选不选结果都一样，编辑器据此提示，而不是替用户删掉
 */
export function findCoveredDepartments(
  departments: readonly DataScopeDepartmentDto[],
  parentMap: ReadonlyMap<ApiId, ApiId | null>,
): Map<ApiId, ApiId> {
  const inclusive = new Set(departments.filter(item => item.includeChildren).map(item => item.departmentId))
  const covered = new Map<ApiId, ApiId>()
  for (const item of departments) {
    let ancestor = parentMap.get(item.departmentId) ?? null
    while (ancestor != null) {
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
export function toDataScopePayload(draft: DataScopeDraft): DataScopeDraft {
  return {
    dataScope: draft.dataScope,
    departments: draft.dataScope === DataPermissionScope.Custom
      ? draft.departments.map(({ departmentId, includeChildren }) => ({ departmentId, includeChildren }))
      : [],
  }
}

/** 自定义档位至少一个部门，其它档位随时可提交 */
export function isDataScopeComplete(draft: DataScopeDraft) {
  return draft.dataScope !== DataPermissionScope.Custom || draft.departments.length > 0
}

/** 草稿与原值是否不同（按提交载荷比较，部门顺序无关） */
export function isDataScopeDirty(draft: DataScopeDraft, original: DataScopeDraft) {
  const next = toDataScopePayload(draft)
  const base = toDataScopePayload(original)
  if (next.dataScope !== base.dataScope || next.departments.length !== base.departments.length)
    return true

  const baseById = new Map(base.departments.map(item => [item.departmentId, item.includeChildren]))
  return next.departments.some(item => baseById.get(item.departmentId) !== item.includeChildren)
}

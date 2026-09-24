import type { ApiId, RoleDataScopeBatchGrantItemDto } from '@/api'

/** 草稿里的一条部门范围 */
export interface ScopeDraftItem {
  departmentId: ApiId
  includeChildren: boolean
}

/** 当前生效的一条角色数据范围：记录主键用于撤销 */
export interface ScopeGrantRow extends ScopeDraftItem {
  basicId: ApiId
}

/** 草稿里放入一个部门：已在草稿里的只改含下级，不重复添加 */
export function upsertScopeDraft(draft: readonly ScopeDraftItem[], item: ScopeDraftItem): ScopeDraftItem[] {
  return draft.some(entry => entry.departmentId === item.departmentId)
    ? draft.map(entry => (entry.departmentId === item.departmentId ? item : entry))
    : [...draft, item]
}

/**
 * 草稿与当前生效的范围比出差量：新部门与改了含下级的都下发为授予（后端对已授予的部门即改其含下级），
 * 从草稿里移走的按记录主键撤销
 */
export function diffScopeDraft(draft: readonly ScopeDraftItem[], current: readonly ScopeGrantRow[]) {
  const currentByDept = new Map(current.map(row => [row.departmentId, row]))
  const draftDeptIds = new Set(draft.map(item => item.departmentId))
  const grants: RoleDataScopeBatchGrantItemDto[] = draft
    .filter(item => currentByDept.get(item.departmentId)?.includeChildren !== item.includeChildren)
    .map(({ departmentId, includeChildren }) => ({ departmentId, includeChildren }))
  return {
    grants,
    revokeRoleDataScopeIds: current.filter(row => !draftDeptIds.has(row.departmentId)).map(row => row.basicId),
  }
}

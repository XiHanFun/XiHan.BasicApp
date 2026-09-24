import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  RoleHierarchyBatchUpdateDto,
  RoleHierarchyDetailDto,
  RoleHierarchyListItemDto,
} from './role-hierarchy.types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const roleHierarchyQueryApi = createDynamicApiClient('RoleHierarchyQuery')
const roleHierarchyCommandApi = createDynamicApiClient('Role')

function buildIncludeSelfParams(includeSelf: boolean) {
  const params: DynamicApiParams = {}
  appendDynamicApiParam(params, 'IncludeSelf', includeSelf)
  return params
}

export const roleHierarchyApi = {
  ancestors(roleId: ApiId, includeSelf = true) {
    return roleHierarchyQueryApi.get<RoleHierarchyListItemDto[]>(
      'RoleAncestors',
      { ...buildIncludeSelfParams(includeSelf), roleId },
    )
  },
  /** 一次性提交本角色直接父角色的新增与移除（单事务，先移除后新增） */
  batchUpdateParents(input: RoleHierarchyBatchUpdateDto) {
    return roleHierarchyCommandApi.post<void, RoleHierarchyBatchUpdateDto>('BatchUpdateRoleParents', input)
  },
  descendants(roleId: ApiId, includeSelf = true) {
    return roleHierarchyQueryApi.get<RoleHierarchyListItemDto[]>(
      'RoleDescendants',
      { ...buildIncludeSelfParams(includeSelf), roleId },
    )
  },
  detail(id: ApiId) {
    return roleHierarchyQueryApi.get<RoleHierarchyDetailDto | null>(
      'RoleHierarchyDetail',
      { id },
    )
  },
}

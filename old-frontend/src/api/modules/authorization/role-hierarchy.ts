import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  RoleHierarchyCreateDto,
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
  create(input: RoleHierarchyCreateDto) {
    return roleHierarchyCommandApi.post<RoleHierarchyDetailDto, RoleHierarchyCreateDto>('RoleHierarchy', input)
  },
  delete(id: ApiId) {
    return roleHierarchyCommandApi.delete('RoleHierarchy', { id })
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

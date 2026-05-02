import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  RoleHierarchyCreateDto,
  RoleHierarchyDetailDto,
  RoleHierarchyListItemDto,
} from './types'
import { appendDynamicApiParam, createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const roleHierarchyQueryApi = createDynamicApiClient('RoleHierarchyQuery')
const roleHierarchyCommandApi = createDynamicApiClient('RoleHierarchy')

function buildIncludeSelfParams(includeSelf: boolean) {
  const params: DynamicApiParams = {}
  appendDynamicApiParam(params, 'IncludeSelf', includeSelf)
  return params
}

export const roleHierarchyApi = {
  ancestors(roleId: ApiId, includeSelf = true) {
    return roleHierarchyQueryApi.get<RoleHierarchyListItemDto[]>(
      `RoleAncestors/${formatDynamicApiRouteValue(roleId)}`,
      buildIncludeSelfParams(includeSelf),
    )
  },
  create(input: RoleHierarchyCreateDto) {
    return roleHierarchyCommandApi.post<RoleHierarchyDetailDto, RoleHierarchyCreateDto>('RoleHierarchy', input)
  },
  delete(id: ApiId) {
    return roleHierarchyCommandApi.delete(`RoleHierarchy/${formatDynamicApiRouteValue(id)}`)
  },
  descendants(roleId: ApiId, includeSelf = true) {
    return roleHierarchyQueryApi.get<RoleHierarchyListItemDto[]>(
      `RoleDescendants/${formatDynamicApiRouteValue(roleId)}`,
      buildIncludeSelfParams(includeSelf),
    )
  },
  detail(id: ApiId) {
    return roleHierarchyQueryApi.get<RoleHierarchyDetailDto | null>(
      `RoleHierarchyDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
}

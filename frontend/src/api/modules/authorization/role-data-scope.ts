import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  RoleDataScopeDetailDto,
  RoleDataScopeGrantDto,
  RoleDataScopeListItemDto,
  RoleDataScopeStatusUpdateDto,
  RoleDataScopeUpdateDto,
} from './role-data-scope.types'
import { appendDynamicApiParam, createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const roleDataScopeQueryApi = createDynamicApiClient('RoleDataScopeQuery')
const roleDataScopeCommandApi = createDynamicApiClient('RoleDataScope')

export const roleDataScopeApi = {
  detail(id: ApiId) {
    return roleDataScopeQueryApi.get<RoleDataScopeDetailDto | null>(
      `RoleDataScopeDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  grant(input: RoleDataScopeGrantDto) {
    return roleDataScopeCommandApi.post<RoleDataScopeDetailDto, RoleDataScopeGrantDto>('RoleDataScope', input)
  },
  list(roleId: ApiId, onlyValid = false) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'OnlyValid', onlyValid)

    return roleDataScopeQueryApi.get<RoleDataScopeListItemDto[]>(
      `RoleDataScopes/${formatDynamicApiRouteValue(roleId)}`,
      params,
    )
  },
  revoke(id: ApiId) {
    return roleDataScopeCommandApi.delete(`RoleDataScope/${formatDynamicApiRouteValue(id)}`)
  },
  update(input: RoleDataScopeUpdateDto) {
    return roleDataScopeCommandApi.put<RoleDataScopeDetailDto, RoleDataScopeUpdateDto>('RoleDataScope', input)
  },
  updateStatus(input: RoleDataScopeStatusUpdateDto) {
    return roleDataScopeCommandApi.put<RoleDataScopeDetailDto, RoleDataScopeStatusUpdateDto>(
      'RoleDataScopeStatus',
      input,
    )
  },
}

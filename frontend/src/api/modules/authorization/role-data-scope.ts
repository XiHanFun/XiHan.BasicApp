import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  RoleDataScopeBatchUpdateDto,
  RoleDataScopeDetailDto,
  RoleDataScopeListItemDto,
  RoleDataScopeStatusUpdateDto,
  RoleDataScopeUpdateDto,
} from './role-data-scope.types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const roleDataScopeQueryApi = createDynamicApiClient('RoleDataScopeQuery')
const roleDataScopeCommandApi = createDynamicApiClient('Role')

export const roleDataScopeApi = {
  /** 一次性提交本次授予与撤销（单事务） */
  batchUpdate(input: RoleDataScopeBatchUpdateDto) {
    return roleDataScopeCommandApi.post<void, RoleDataScopeBatchUpdateDto>('BatchUpdateRoleDataScopes', input)
  },
  detail(id: ApiId) {
    return roleDataScopeQueryApi.get<RoleDataScopeDetailDto | null>(
      'RoleDataScopeDetail',
      { id },
    )
  },
  list(roleId: ApiId, onlyValid = false) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'OnlyValid', onlyValid)

    return roleDataScopeQueryApi.get<RoleDataScopeListItemDto[]>(
      'RoleDataScopes',
      { ...params, roleId },
    )
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

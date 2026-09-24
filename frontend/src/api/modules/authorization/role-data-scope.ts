import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  RoleDataScopeDetailDto,
  RoleDataScopeListItemDto,
  RoleDataScopeSetDto,
} from './role-data-scope.types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const roleDataScopeQueryApi = createDynamicApiClient('RoleDataScopeQuery')
const roleDataScopeCommandApi = createDynamicApiClient('Role')

export const roleDataScopeApi = {
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
  /** 档位与自定义部门一次提交（单事务） */
  set(input: RoleDataScopeSetDto) {
    return roleDataScopeCommandApi.post<void, RoleDataScopeSetDto>('SetRoleDataScope', input)
  },
}

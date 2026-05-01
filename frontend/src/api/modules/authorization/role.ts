import type { ApiId, PageResult } from '../../types'
import type { DynamicApiParams } from '../../base'
import type {
  RoleCreateDto,
  RoleDetailDto,
  RoleListItemDto,
  RolePageQueryDto,
  RoleSelectItemDto,
  RoleSelectQueryDto,
  RoleStatusUpdateDto,
  RoleUpdateDto,
} from './types'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const roleQueryApi = createDynamicApiClient('RoleQuery')
const roleCommandApi = createDynamicApiClient('Role')
const roleReadApi = createReadApi<RoleListItemDto, RoleDetailDto, RolePageQueryDto>('RoleQuery', 'Role')
const roleBaseCommandApi = createCommandApi<RoleCreateDto, RoleUpdateDto, RoleDetailDto>('Role', 'Role')

export const roleApi = {
  create(input: RoleCreateDto) {
    return roleBaseCommandApi.create(input)
  },
  delete(id: ApiId) {
    return roleCommandApi.delete(`Role/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return roleReadApi.detail(id)
  },
  enabledList(input: RoleSelectQueryDto) {
    return roleQueryApi.get<RoleSelectItemDto[]>('EnabledRoles', toRoleSelectParams(input))
  },
  page(input: RolePageQueryDto) {
    return roleQueryApi.get<PageResult<RoleListItemDto>>('RolePage', toRolePageParams(input))
  },
  update(input: RoleUpdateDto) {
    return roleBaseCommandApi.update(input)
  },
  updateStatus(input: RoleStatusUpdateDto) {
    return roleCommandApi.put<RoleDetailDto, RoleStatusUpdateDto>('RoleStatus', input)
  },
}

function toRolePageParams(input: RolePageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'DataScope', input.dataScope)
  appendDynamicApiParam(params, 'IsGlobal', input.isGlobal)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'RoleType', input.roleType)
  appendDynamicApiParam(params, 'Status', input.status)

  return params
}

function toRoleSelectParams(input: RoleSelectQueryDto) {
  const params: DynamicApiParams = {
    Limit: input.limit,
  }

  appendDynamicApiParam(params, 'IsGlobal', input.isGlobal)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'RoleType', input.roleType)

  return params
}

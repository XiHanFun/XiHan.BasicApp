import type { ApiId, PageResult } from '../../types'
import type {
  PermissionDelegationCreateDto,
  PermissionDelegationDetailDto,
  PermissionDelegationListItemDto,
  PermissionDelegationPageQueryDto,
  PermissionDelegationStatusUpdateDto,
  PermissionDelegationUpdateDto,
} from './permission-delegation.types'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const permissionDelegationQueryApi = createDynamicApiClient('PermissionDelegationQuery')
const permissionDelegationCommandApi = createDynamicApiClient('Permission')
const permissionDelegationReadApi = createReadApi<
  PermissionDelegationListItemDto,
  PermissionDelegationDetailDto,
  PermissionDelegationPageQueryDto
>('PermissionDelegationQuery', 'PermissionDelegation')
const permissionDelegationBaseCommandApi = createCommandApi<
  PermissionDelegationCreateDto,
  PermissionDelegationUpdateDto,
  PermissionDelegationDetailDto
>('Permission', 'PermissionDelegation')

export const permissionDelegationApi = {
  create(input: PermissionDelegationCreateDto) {
    return permissionDelegationBaseCommandApi.create(input)
  },
  delete(id: ApiId) {
    return permissionDelegationCommandApi.delete(`PermissionDelegation/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return permissionDelegationReadApi.detail(id)
  },
  page(input: PermissionDelegationPageQueryDto) {
    return permissionDelegationQueryApi.get<PageResult<PermissionDelegationListItemDto>>(
      'PermissionDelegationPage',
      toPermissionDelegationPageParams(input),
    )
  },
  update(input: PermissionDelegationUpdateDto) {
    return permissionDelegationBaseCommandApi.update(input)
  },
  updateStatus(input: PermissionDelegationStatusUpdateDto) {
    return permissionDelegationCommandApi.put<PermissionDelegationDetailDto, PermissionDelegationStatusUpdateDto>(
      'PermissionDelegationStatus',
      input,
    )
  },
}

function toPermissionDelegationPageParams(input: PermissionDelegationPageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'DelegateeUserId', input.delegateeUserId)
  appendDynamicApiParam(params, 'DelegationStatus', input.delegationStatus)
  appendDynamicApiParam(params, 'DelegatorUserId', input.delegatorUserId)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'PermissionId', input.permissionId)
  appendDynamicApiParam(params, 'RoleId', input.roleId)

  return params
}

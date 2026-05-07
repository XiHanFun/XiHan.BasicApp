import type { ApiId, PageResult } from '../../types'
import type {
  PermissionRequestCreateDto,
  PermissionRequestDetailDto,
  PermissionRequestListItemDto,
  PermissionRequestPageQueryDto,
  PermissionRequestStatusUpdateDto,
  PermissionRequestUpdateDto,
} from './permission-request.types'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const permissionRequestQueryApi = createDynamicApiClient('PermissionRequestQuery')
const permissionRequestCommandApi = createDynamicApiClient('PermissionRequest')
const permissionRequestReadApi = createReadApi<
  PermissionRequestListItemDto,
  PermissionRequestDetailDto,
  PermissionRequestPageQueryDto
>('PermissionRequestQuery', 'PermissionRequest')
const permissionRequestBaseCommandApi = createCommandApi<
  PermissionRequestCreateDto,
  PermissionRequestUpdateDto,
  PermissionRequestDetailDto
>('PermissionRequest', 'PermissionRequest')

export const permissionRequestApi = {
  create(input: PermissionRequestCreateDto) {
    return permissionRequestBaseCommandApi.create(input)
  },
  delete(id: ApiId) {
    return permissionRequestCommandApi.delete(`PermissionRequest/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return permissionRequestReadApi.detail(id)
  },
  page(input: PermissionRequestPageQueryDto) {
    return permissionRequestQueryApi.get<PageResult<PermissionRequestListItemDto>>(
      'PermissionRequestPage',
      toPermissionRequestPageParams(input),
    )
  },
  update(input: PermissionRequestUpdateDto) {
    return permissionRequestBaseCommandApi.update(input)
  },
  updateStatus(input: PermissionRequestStatusUpdateDto) {
    return permissionRequestCommandApi.put<PermissionRequestDetailDto, PermissionRequestStatusUpdateDto>(
      'PermissionRequestStatus',
      input,
    )
  },
}

function toPermissionRequestPageParams(input: PermissionRequestPageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'PermissionId', input.permissionId)
  appendDynamicApiParam(params, 'RequestStatus', input.requestStatus)
  appendDynamicApiParam(params, 'RequestUserId', input.requestUserId)
  appendDynamicApiParam(params, 'ReviewId', input.reviewId)
  appendDynamicApiParam(params, 'RoleId', input.roleId)

  return params
}

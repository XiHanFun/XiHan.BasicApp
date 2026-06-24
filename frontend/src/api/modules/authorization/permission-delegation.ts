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
  createCommandApi,
  createDynamicApiClient,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const permissionDelegationQueryApi = createDynamicApiClient('PermissionDelegationQuery')
const permissionDelegationCommandApi = createDynamicApiClient('PermissionDelegation')
const permissionDelegationReadApi = createReadApi<
  PermissionDelegationListItemDto,
  PermissionDelegationDetailDto,
  PermissionDelegationPageQueryDto
>('PermissionDelegationQuery', 'PermissionDelegation')
const permissionDelegationBaseCommandApi = createCommandApi<
  PermissionDelegationCreateDto,
  PermissionDelegationUpdateDto,
  PermissionDelegationDetailDto
>('PermissionDelegation', 'PermissionDelegation')

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
    return permissionDelegationQueryApi.post<PageResult<PermissionDelegationListItemDto>>(
      'PermissionDelegationPage',
      input,
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

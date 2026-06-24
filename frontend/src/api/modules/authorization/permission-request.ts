import type { ApiId, PageResult } from '../../types'
import type {
  PermissionRequestCreateDto,
  PermissionRequestDetailDto,
  PermissionRequestListItemDto,
  PermissionRequestPageQueryDto,
  PermissionRequestStatusUpdateDto,
  PermissionRequestUpdateDto,
} from './permission-request.types'
import { createCommandApi, createDynamicApiClient, createReadApi, formatDynamicApiRouteValue } from '../../base'

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

export interface PermissionRequestApprovalDto {
  basicId: ApiId
  remark?: string | null
}

export const permissionRequestApi = {
  approve(input: PermissionRequestApprovalDto) {
    return permissionRequestCommandApi.post<PermissionRequestDetailDto, PermissionRequestApprovalDto>(
      'ApprovePermissionRequest',
      input,
    )
  },
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
    return permissionRequestQueryApi.post<PageResult<PermissionRequestListItemDto>>('PermissionRequestPage', input)
  },
  reject(input: PermissionRequestApprovalDto) {
    return permissionRequestCommandApi.post<PermissionRequestDetailDto, PermissionRequestApprovalDto>(
      'RejectPermissionRequest',
      input,
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

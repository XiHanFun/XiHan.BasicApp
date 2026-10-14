import type { ApiId, PageResult } from '../../types'
import type {
  PermissionRequestCreateDto,
  PermissionRequestDetailDto,
  PermissionRequestListItemDto,
  PermissionRequestPageQueryDto,
  PermissionRequestStatusUpdateDto,
  PermissionRequestUpdateDto,
} from './permission-request.types'
import { createCommandApi, createDynamicApiClient, createReadApi } from '../../base'

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

/**
 * 权限申请：提交即发起「权限申请」审批流程，审批在审批中心办理，通过后由流程回写授权。
 * 删除、改内容或改到终态都会撤回仍在进行的审批。
 */
export const permissionRequestApi = {
  create(input: PermissionRequestCreateDto) {
    return permissionRequestBaseCommandApi.create(input)
  },
  delete(id: ApiId) {
    return permissionRequestCommandApi.delete('PermissionRequest', { id })
  },
  detail(id: ApiId) {
    return permissionRequestReadApi.detail(id)
  },
  page(input: PermissionRequestPageQueryDto) {
    return permissionRequestQueryApi.post<PageResult<PermissionRequestListItemDto>>('PermissionRequestPage', input)
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

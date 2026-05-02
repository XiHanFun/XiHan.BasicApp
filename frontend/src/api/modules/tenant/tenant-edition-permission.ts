import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  TenantEditionPermissionDetailDto,
  TenantEditionPermissionGrantDto,
  TenantEditionPermissionListItemDto,
  TenantEditionPermissionStatusUpdateDto,
} from './tenant-edition-permission.types'
import { appendDynamicApiParam, createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const tenantEditionPermissionQueryApi = createDynamicApiClient('TenantEditionPermissionQuery')
const tenantEditionPermissionCommandApi = createDynamicApiClient('TenantEditionPermission')

export const tenantEditionPermissionApi = {
  detail(id: ApiId) {
    return tenantEditionPermissionQueryApi.get<TenantEditionPermissionDetailDto | null>(
      `TenantEditionPermissionDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  grant(input: TenantEditionPermissionGrantDto) {
    return tenantEditionPermissionCommandApi.post<
      TenantEditionPermissionDetailDto,
      TenantEditionPermissionGrantDto
    >('TenantEditionPermission', input)
  },
  list(editionId: ApiId, onlyValid = false) {
    const params: DynamicApiParams = {}

    appendDynamicApiParam(params, 'OnlyValid', onlyValid)

    return tenantEditionPermissionQueryApi.get<TenantEditionPermissionListItemDto[]>(
      `TenantEditionPermissions/${formatDynamicApiRouteValue(editionId)}`,
      params,
    )
  },
  revoke(id: ApiId) {
    return tenantEditionPermissionCommandApi.delete(
      `TenantEditionPermission/${formatDynamicApiRouteValue(id)}`,
    )
  },
  updateStatus(input: TenantEditionPermissionStatusUpdateDto) {
    return tenantEditionPermissionCommandApi.put<
      TenantEditionPermissionDetailDto,
      TenantEditionPermissionStatusUpdateDto
    >('TenantEditionPermissionStatus', input)
  },
}

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
const tenantEditionPermissionCommandApi = createDynamicApiClient('TenantEdition')

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
    >('GrantTenantEditionPermission', input)
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
    // Revoke 前缀不在动态 API 动词表内：路由保留完整方法名且默认 POST
    return tenantEditionPermissionCommandApi.post(
      `RevokeTenantEditionPermission/${formatDynamicApiRouteValue(id)}`,
    )
  },
  updateStatus(input: TenantEditionPermissionStatusUpdateDto) {
    return tenantEditionPermissionCommandApi.put<
      TenantEditionPermissionDetailDto,
      TenantEditionPermissionStatusUpdateDto
    >('TenantEditionPermissionStatus', input)
  },
}

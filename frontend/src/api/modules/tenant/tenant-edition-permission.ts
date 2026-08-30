import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  TenantEditionPermissionBatchUpdateDto,
  TenantEditionPermissionDetailDto,
  TenantEditionPermissionGrantDto,
  TenantEditionPermissionListItemDto,
  TenantEditionPermissionStatusUpdateDto,
} from './tenant-edition-permission.types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const tenantEditionPermissionQueryApi = createDynamicApiClient('TenantEditionPermissionQuery')
const tenantEditionPermissionCommandApi = createDynamicApiClient('TenantEdition')

export const tenantEditionPermissionApi = {
  /** 批量提交版本权限改动（授予/撤销/启停一次性下发，后端单事务） */
  batchUpdate(input: TenantEditionPermissionBatchUpdateDto) {
    return tenantEditionPermissionCommandApi.post<void, TenantEditionPermissionBatchUpdateDto>(
      'BatchUpdateTenantEditionPermissions',
      input,
    )
  },
  detail(id: ApiId) {
    return tenantEditionPermissionQueryApi.get<TenantEditionPermissionDetailDto | null>(
      'TenantEditionPermissionDetail',
      { id },
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
      'TenantEditionPermissions',
      { ...params, editionId },
    )
  },
  revoke(id: ApiId) {
    // Revoke 前缀不在动态 API 动词表内：路由保留完整方法名且默认 POST。
    // RevokeTenantEditionPermissionAsync(long id)：简单类型参数被推断为 query，
    // 放进请求体绑不上（会静默变成 0），故 id 走 query
    return tenantEditionPermissionCommandApi.post(
      'RevokeTenantEditionPermission',
      undefined,
      { params: { id } },
    )
  },
  updateStatus(input: TenantEditionPermissionStatusUpdateDto) {
    return tenantEditionPermissionCommandApi.put<
      TenantEditionPermissionDetailDto,
      TenantEditionPermissionStatusUpdateDto
    >('TenantEditionPermissionStatus', input)
  },
}

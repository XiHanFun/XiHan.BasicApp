import type { PageResult } from '../../types'
import type { DynamicApiParams } from '../../base'
import type {
  TenantCreateDto,
  TenantDetailDto,
  TenantListItemDto,
  TenantPageQueryDto,
  TenantStatusUpdateDto,
  TenantSwitcherDto,
  TenantUpdateDto,
} from './types'
import { createCommandApi, createDynamicApiClient, createReadApi } from '../../base'

const tenantQueryApi = createDynamicApiClient('TenantQuery')
const tenantCommandApi = createDynamicApiClient('Tenant')
const tenantReadApi = createReadApi<TenantListItemDto, TenantDetailDto, TenantPageQueryDto>('TenantQuery', 'Tenant')
const tenantBaseCommandApi = createCommandApi<TenantCreateDto, TenantUpdateDto, TenantDetailDto>('Tenant', 'Tenant')

export const tenantApi = {
  create(input: TenantCreateDto) {
    return tenantBaseCommandApi.create(input)
  },
  detail(id: TenantDetailDto['basicId']) {
    return tenantReadApi.detail(id)
  },
  myAvailableTenants() {
    return tenantQueryApi.get<TenantSwitcherDto[]>('MyAvailableTenants')
  },
  page(input: TenantPageQueryDto) {
    return tenantQueryApi.get<PageResult<TenantListItemDto>>('TenantPage', toTenantPageParams(input))
  },
  update(input: TenantUpdateDto) {
    return tenantBaseCommandApi.update(input)
  },
  updateStatus(input: TenantStatusUpdateDto) {
    return tenantCommandApi.put<TenantDetailDto, TenantStatusUpdateDto>('TenantStatus', input)
  },
}

function toTenantPageParams(input: TenantPageQueryDto): DynamicApiParams {
  const params: DynamicApiParams = {
    'Behavior.DisableDefaultSort': input.behavior.disableDefaultSort,
    'Behavior.DisablePaging': input.behavior.disablePaging,
    'Behavior.EnableSplitQuery': input.behavior.enableSplitQuery,
    'Behavior.IgnoreSoftDelete': input.behavior.ignoreSoftDelete,
    'Behavior.IgnoreTenant': input.behavior.ignoreTenant,
    'Page.PageIndex': input.page.pageIndex,
    'Page.PageSize': input.page.pageSize,
  }

  appendParam(params, 'Behavior.QueryTimeout', input.behavior.queryTimeout)
  appendParam(params, 'ConfigStatus', input.configStatus)
  appendParam(params, 'EditionId', input.editionId)
  appendParam(params, 'ExpireTimeEnd', input.expireTimeEnd)
  appendParam(params, 'ExpireTimeStart', input.expireTimeStart)
  appendParam(params, 'Keyword', input.keyword)
  appendParam(params, 'TenantStatus', input.tenantStatus)

  return params
}

function appendParam(params: DynamicApiParams, key: string, value: boolean | number | string | null | undefined) {
  if (value === undefined || value === null || value === '') {
    return
  }

  params[key] = value
}

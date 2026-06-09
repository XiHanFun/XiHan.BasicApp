import type { LoginToken, SwitchTenantParams } from '~/types'
import type { PageResult } from '../../types'
import type {
  TenantCreateDto,
  TenantDetailDto,
  TenantListItemDto,
  TenantPageQueryDto,
  TenantStatusUpdateDto,
  TenantSwitcherDto,
  TenantUpdateDto,
} from './tenant.types'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
} from '../../base'

const tenantQueryApi = createDynamicApiClient('TenantQuery')
const tenantCommandApi = createDynamicApiClient('Tenant')
const authCommandApi = createDynamicApiClient('Auth')
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
  switchTenant(input: SwitchTenantParams) {
    return authCommandApi.post<LoginToken, SwitchTenantParams>('SwitchTenant', input)
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

function toTenantPageParams(input: TenantPageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'ConfigStatus', input.configStatus)
  appendDynamicApiParam(params, 'EditionId', input.editionId)
  appendDynamicApiParam(params, 'ExpirationTimeEnd', input.expirationTimeEnd)
  appendDynamicApiParam(params, 'ExpirationTimeStart', input.expirationTimeStart)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'TenantStatus', input.tenantStatus)

  return params
}

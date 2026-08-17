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
import type { LoginToken, SwitchTenantParams } from '~/types'
import { createCommandApi, createDynamicApiClient, createReadApi } from '../../base'

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
  /** 删除租户（软删，后端要求租户已停用或暂停） */
  remove(id: TenantDetailDto['basicId']) {
    return tenantCommandApi.delete('Tenant', { id })
  },
  initializeDatabase(id: TenantDetailDto['basicId']) {
    // 仅库隔离租户：建库 → 建表 → 基线种子（POST /api/Tenant/InitializeDatabase/{id}）
    return tenantCommandApi.post<TenantDetailDto>('InitializeDatabase', { id })
  },
  myAvailableTenants() {
    return tenantQueryApi.get<TenantSwitcherDto[]>('MyAvailableTenants')
  },
  switchTenant(input: SwitchTenantParams) {
    return authCommandApi.post<LoginToken, SwitchTenantParams>('SwitchTenant', input)
  },
  page(input: TenantPageQueryDto) {
    return tenantQueryApi.post<PageResult<TenantListItemDto>>('TenantPage', input)
  },
  update(input: TenantUpdateDto) {
    return tenantBaseCommandApi.update(input)
  },
  updateStatus(input: TenantStatusUpdateDto) {
    return tenantCommandApi.put<TenantDetailDto, TenantStatusUpdateDto>('TenantStatus', input)
  },
}

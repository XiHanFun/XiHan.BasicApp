import type { PageResult } from '../../types'
import type {
  TenantCreateDto,
  TenantDetailDto,
  TenantListItemDto,
  TenantOverQuotaDto,
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
    // 仅库隔离租户：建库 → 建表 → 基线种子。
    // InitializeDatabaseAsync(long id)：简单类型参数被动态 API 推断为 query，POST 也不把 id 拼进路由段，
    // 故 id 走 query（同 export Cancel 模式），route 为 /Tenant/InitializeDatabase?id=
    return tenantCommandApi.post<TenantDetailDto>('InitializeDatabase', undefined, { params: { id } })
  },
  /** 已超出配额的租户清单：配额拦截只作用于新增，存量超限的租户需要主动查一次 */
  overQuotaTenants() {
    return tenantQueryApi.get<TenantOverQuotaDto[]>('OverQuotaTenants')
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

import type { PageResult } from '../../types'
import type {
  TenantEditionCreateDto,
  TenantEditionDefaultUpdateDto,
  TenantEditionDetailDto,
  TenantEditionListItemDto,
  TenantEditionPageQueryDto,
  TenantEditionStatusUpdateDto,
  TenantEditionUpdateDto,
} from './tenant-edition.types'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
} from '../../base'

const tenantEditionQueryApi = createDynamicApiClient('TenantEditionQuery')
const tenantEditionCommandApi = createDynamicApiClient('TenantEdition')
const tenantEditionReadApi = createReadApi<TenantEditionListItemDto, TenantEditionDetailDto, TenantEditionPageQueryDto>(
  'TenantEditionQuery',
  'TenantEdition',
)
const tenantEditionBaseCommandApi = createCommandApi<
  TenantEditionCreateDto,
  TenantEditionUpdateDto,
  TenantEditionDetailDto
>('TenantEdition', 'TenantEdition')

export const tenantEditionApi = {
  create(input: TenantEditionCreateDto) {
    return tenantEditionBaseCommandApi.create(input)
  },
  detail(id: TenantEditionDetailDto['basicId']) {
    return tenantEditionReadApi.detail(id)
  },
  enabledList() {
    return tenantEditionQueryApi.get<TenantEditionListItemDto[]>('EnabledTenantEditions')
  },
  page(input: TenantEditionPageQueryDto) {
    return tenantEditionQueryApi.get<PageResult<TenantEditionListItemDto>>(
      'TenantEditionPage',
      toTenantEditionPageParams(input),
    )
  },
  update(input: TenantEditionUpdateDto) {
    return tenantEditionBaseCommandApi.update(input)
  },
  updateDefault(input: TenantEditionDefaultUpdateDto) {
    return tenantEditionCommandApi.put<TenantEditionDetailDto, TenantEditionDefaultUpdateDto>(
      'DefaultTenantEdition',
      input,
    )
  },
  updateStatus(input: TenantEditionStatusUpdateDto) {
    return tenantEditionCommandApi.put<TenantEditionDetailDto, TenantEditionStatusUpdateDto>(
      'TenantEditionStatus',
      input,
    )
  },
}

function toTenantEditionPageParams(input: TenantEditionPageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'IsDefault', input.isDefault)
  appendDynamicApiParam(params, 'IsFree', input.isFree)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'Status', input.status)

  return params
}

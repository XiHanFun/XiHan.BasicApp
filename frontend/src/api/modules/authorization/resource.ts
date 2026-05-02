import type { DynamicApiParams } from '../../base'
import type { ApiId, PageResult } from '../../types'
import type {
  ResourceCreateDto,
  ResourceDetailDto,
  ResourceListItemDto,
  ResourcePageQueryDto,
  ResourceSelectItemDto,
  ResourceSelectQueryDto,
  ResourceStatusUpdateDto,
  ResourceUpdateDto,
} from './types'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const resourceQueryApi = createDynamicApiClient('ResourceQuery')
const resourceCommandApi = createDynamicApiClient('Resource')
const resourceReadApi = createReadApi<ResourceListItemDto, ResourceDetailDto, ResourcePageQueryDto>(
  'ResourceQuery',
  'Resource',
)
const resourceBaseCommandApi = createCommandApi<ResourceCreateDto, ResourceUpdateDto, ResourceDetailDto>(
  'Resource',
  'Resource',
)

export const resourceApi = {
  availableGlobal(input: ResourceSelectQueryDto) {
    return resourceQueryApi.get<ResourceSelectItemDto[]>('AvailableGlobalResources', toResourceSelectParams(input))
  },
  create(input: ResourceCreateDto) {
    return resourceBaseCommandApi.create(input)
  },
  delete(id: ApiId) {
    return resourceCommandApi.delete(`Resource/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return resourceReadApi.detail(id)
  },
  page(input: ResourcePageQueryDto) {
    return resourceQueryApi.get<PageResult<ResourceListItemDto>>('ResourcePage', toResourcePageParams(input))
  },
  update(input: ResourceUpdateDto) {
    return resourceBaseCommandApi.update(input)
  },
  updateStatus(input: ResourceStatusUpdateDto) {
    return resourceCommandApi.put<ResourceDetailDto, ResourceStatusUpdateDto>('ResourceStatus', input)
  },
}

function toResourcePageParams(input: ResourcePageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'AccessLevel', input.accessLevel)
  appendDynamicApiParam(params, 'IsGlobal', input.isGlobal)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'ResourceType', input.resourceType)
  appendDynamicApiParam(params, 'Status', input.status)

  return params
}

function toResourceSelectParams(input: ResourceSelectQueryDto) {
  const params: DynamicApiParams = {
    Limit: input.limit,
  }

  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'ResourceType', input.resourceType)

  return params
}

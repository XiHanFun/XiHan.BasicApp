import type { DynamicApiParams } from '../../base'
import type { ApiId, PageResult } from '../../types'
import type {
  PermissionCreateDto,
  PermissionDetailDto,
  PermissionListItemDto,
  PermissionPageQueryDto,
  PermissionSelectItemDto,
  PermissionSelectQueryDto,
  PermissionStatusUpdateDto,
  PermissionUpdateDto,
} from './types'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const permissionQueryApi = createDynamicApiClient('PermissionQuery')
const permissionCommandApi = createDynamicApiClient('Permission')
const permissionReadApi = createReadApi<PermissionListItemDto, PermissionDetailDto, PermissionPageQueryDto>(
  'PermissionQuery',
  'Permission',
)
const permissionBaseCommandApi = createCommandApi<PermissionCreateDto, PermissionUpdateDto, PermissionDetailDto>(
  'Permission',
  'Permission',
)

export const permissionApi = {
  availableGlobal(input: PermissionSelectQueryDto) {
    return permissionQueryApi.get<PermissionSelectItemDto[]>(
      'AvailableGlobalPermissions',
      toPermissionSelectParams(input),
    )
  },
  create(input: PermissionCreateDto) {
    return permissionBaseCommandApi.create(input)
  },
  delete(id: ApiId) {
    return permissionCommandApi.delete(`Permission/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return permissionReadApi.detail(id)
  },
  page(input: PermissionPageQueryDto) {
    return permissionQueryApi.get<PageResult<PermissionListItemDto>>(
      'PermissionPage',
      toPermissionPageParams(input),
    )
  },
  update(input: PermissionUpdateDto) {
    return permissionBaseCommandApi.update(input)
  },
  updateStatus(input: PermissionStatusUpdateDto) {
    return permissionCommandApi.put<PermissionDetailDto, PermissionStatusUpdateDto>(
      'PermissionStatus',
      input,
    )
  },
}

function toPermissionPageParams(input: PermissionPageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'IsGlobal', input.isGlobal)
  appendDynamicApiParam(params, 'IsRequireAudit', input.isRequireAudit)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'ModuleCode', input.moduleCode)
  appendDynamicApiParam(params, 'OperationId', input.operationId)
  appendDynamicApiParam(params, 'PermissionType', input.permissionType)
  appendDynamicApiParam(params, 'ResourceId', input.resourceId)
  appendDynamicApiParam(params, 'Status', input.status)

  return params
}

function toPermissionSelectParams(input: PermissionSelectQueryDto) {
  const params: DynamicApiParams = {
    Limit: input.limit,
  }

  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'ModuleCode', input.moduleCode)
  appendDynamicApiParam(params, 'PermissionType', input.permissionType)

  return params
}

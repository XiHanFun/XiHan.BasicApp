import type { ApiId, PageResult } from '../../types'
import type {
  PermissionChangeLogDetailDto,
  PermissionChangeLogListItemDto,
  PermissionChangeLogPageQueryDto,
} from './permission-change-log.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const permissionChangeLogQueryApi = createDynamicApiClient('PermissionChangeLogQuery')

export const permissionChangeLogApi = {
  detail(id: ApiId) {
    return permissionChangeLogQueryApi.get<PermissionChangeLogDetailDto | null>(
      `PermissionChangeLogDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: PermissionChangeLogPageQueryDto) {
    return permissionChangeLogQueryApi.get<PageResult<PermissionChangeLogListItemDto>>(
      'PermissionChangeLogPage',
      toPermissionChangeLogPageParams(input),
    )
  },
}

function toPermissionChangeLogPageParams(input: PermissionChangeLogPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'ChangeTimeEnd', input.changeTimeEnd)
  appendDynamicApiParam(params, 'ChangeTimeStart', input.changeTimeStart)
  appendDynamicApiParam(params, 'ChangeType', input.changeType)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'OperatorUserId', input.operatorUserId)
  appendDynamicApiParam(params, 'PermissionId', input.permissionId)
  appendDynamicApiParam(params, 'TargetRoleId', input.targetRoleId)
  appendDynamicApiParam(params, 'TargetUserId', input.targetUserId)
  appendDynamicApiParam(params, 'TraceId', input.traceId)
  return params
}

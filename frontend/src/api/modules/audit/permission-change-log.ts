import type { PageResult } from '../../types'
import type {
  PermissionChangeLogListItemDto,
  PermissionChangeLogPageQueryDto,
} from './permission-change-log.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
} from '../../base'

const permissionChangeLogQueryApi = createDynamicApiClient('PermissionChangeLogQuery')

export const permissionChangeLogApi = {
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

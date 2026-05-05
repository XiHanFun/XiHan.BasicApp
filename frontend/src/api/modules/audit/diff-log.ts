import type { ApiId, PageResult } from '../../types'
import type { DiffLogDetailDto, DiffLogListItemDto, DiffLogPageQueryDto } from './diff-log.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const diffLogQueryApi = createDynamicApiClient('DiffLogQuery')

export const diffLogApi = {
  detail(id: ApiId) {
    return diffLogQueryApi.get<DiffLogDetailDto | null>(
      `DiffLogDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: DiffLogPageQueryDto) {
    return diffLogQueryApi.get<PageResult<DiffLogListItemDto>>(
      'DiffLogPage',
      toDiffLogPageParams(input),
    )
  },
}

function toDiffLogPageParams(input: DiffLogPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'AuditTimeEnd', input.auditTimeEnd)
  appendDynamicApiParam(params, 'AuditTimeStart', input.auditTimeStart)
  appendDynamicApiParam(params, 'AuditType', input.auditType)
  appendDynamicApiParam(params, 'EntityId', input.entityId)
  appendDynamicApiParam(params, 'EntityName', input.entityName)
  appendDynamicApiParam(params, 'EntityType', input.entityType)
  appendDynamicApiParam(params, 'IsSuccess', input.isSuccess)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'MaxExecutionTime', input.maxExecutionTime)
  appendDynamicApiParam(params, 'MinExecutionTime', input.minExecutionTime)
  appendDynamicApiParam(params, 'OperationType', input.operationType)
  appendDynamicApiParam(params, 'RequestId', input.requestId)
  appendDynamicApiParam(params, 'RiskLevel', input.riskLevel)
  appendDynamicApiParam(params, 'SessionId', input.sessionId)
  appendDynamicApiParam(params, 'TableName', input.tableName)
  appendDynamicApiParam(params, 'TraceId', input.traceId)
  appendDynamicApiParam(params, 'UserId', input.userId)
  appendDynamicApiParam(params, 'UserName', input.userName)
  return params
}

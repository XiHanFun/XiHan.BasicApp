import type { PageResult } from '../../types'
import type { AuditLogListItemDto, AuditLogPageQueryDto } from './audit-log.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
} from '../../base'

const auditLogQueryApi = createDynamicApiClient('AuditLogQuery')

export const auditLogApi = {
  page(input: AuditLogPageQueryDto) {
    return auditLogQueryApi.get<PageResult<AuditLogListItemDto>>(
      'AuditLogPage',
      toAuditLogPageParams(input),
    )
  },
}

function toAuditLogPageParams(input: AuditLogPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'AuditTimeEnd', input.auditTimeEnd)
  appendDynamicApiParam(params, 'AuditTimeStart', input.auditTimeStart)
  appendDynamicApiParam(params, 'AuditType', input.auditType)
  appendDynamicApiParam(params, 'EntityId', input.entityId)
  appendDynamicApiParam(params, 'EntityType', input.entityType)
  appendDynamicApiParam(params, 'IsSuccess', input.isSuccess)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'OperationType', input.operationType)
  appendDynamicApiParam(params, 'RiskLevel', input.riskLevel)
  appendDynamicApiParam(params, 'TraceId', input.traceId)
  appendDynamicApiParam(params, 'UserId', input.userId)
  appendDynamicApiParam(params, 'UserName', input.userName)
  return params
}

import type { PageResult } from '../../types'
import type { OperationLogListItemDto, OperationLogPageQueryDto } from './operation-log.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
} from '../../base'

const operationLogQueryApi = createDynamicApiClient('OperationLogQuery')

export const operationLogApi = {
  page(input: OperationLogPageQueryDto) {
    return operationLogQueryApi.get<PageResult<OperationLogListItemDto>>(
      'OperationLogPage',
      toOperationLogPageParams(input),
    )
  },
}

function toOperationLogPageParams(input: OperationLogPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'Function', input.function)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'MaxExecutionTime', input.maxExecutionTime)
  appendDynamicApiParam(params, 'Method', input.method)
  appendDynamicApiParam(params, 'MinExecutionTime', input.minExecutionTime)
  appendDynamicApiParam(params, 'Module', input.module)
  appendDynamicApiParam(params, 'OperationTimeEnd', input.operationTimeEnd)
  appendDynamicApiParam(params, 'OperationTimeStart', input.operationTimeStart)
  appendDynamicApiParam(params, 'OperationType', input.operationType)
  appendDynamicApiParam(params, 'SessionId', input.sessionId)
  appendDynamicApiParam(params, 'Status', input.status)
  appendDynamicApiParam(params, 'Title', input.title)
  appendDynamicApiParam(params, 'TraceId', input.traceId)
  appendDynamicApiParam(params, 'UserId', input.userId)
  appendDynamicApiParam(params, 'UserName', input.userName)
  return params
}

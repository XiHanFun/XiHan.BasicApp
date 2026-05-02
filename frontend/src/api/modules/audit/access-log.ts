import type { PageResult } from '../../types'
import type { AccessLogListItemDto, AccessLogPageQueryDto } from './access-log.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
} from '../../base'

const accessLogQueryApi = createDynamicApiClient('AccessLogQuery')

export const accessLogApi = {
  page(input: AccessLogPageQueryDto) {
    return accessLogQueryApi.get<PageResult<AccessLogListItemDto>>(
      'AccessLogPage',
      toAccessLogPageParams(input),
    )
  },
}

function toAccessLogPageParams(input: AccessLogPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'AccessResult', input.accessResult)
  appendDynamicApiParam(params, 'AccessTimeEnd', input.accessTimeEnd)
  appendDynamicApiParam(params, 'AccessTimeStart', input.accessTimeStart)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'MaxExecutionTime', input.maxExecutionTime)
  appendDynamicApiParam(params, 'Method', input.method)
  appendDynamicApiParam(params, 'MinExecutionTime', input.minExecutionTime)
  appendDynamicApiParam(params, 'ResourcePath', input.resourcePath)
  appendDynamicApiParam(params, 'ResourceType', input.resourceType)
  appendDynamicApiParam(params, 'SessionId', input.sessionId)
  appendDynamicApiParam(params, 'StatusCode', input.statusCode)
  appendDynamicApiParam(params, 'TraceId', input.traceId)
  appendDynamicApiParam(params, 'UserId', input.userId)
  appendDynamicApiParam(params, 'UserName', input.userName)
  return params
}

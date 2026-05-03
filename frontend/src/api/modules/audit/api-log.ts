import type { PageResult } from '../../types'
import type { ApiLogListItemDto, ApiLogPageQueryDto } from './api-log.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
} from '../../base'

const apiLogQueryApi = createDynamicApiClient('ApiLogQuery')

export const apiLogApi = {
  page(input: ApiLogPageQueryDto) {
    return apiLogQueryApi.get<PageResult<ApiLogListItemDto>>(
      'ApiLogPage',
      toApiLogPageParams(input),
    )
  },
}

function toApiLogPageParams(input: ApiLogPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'ActionName', input.actionName)
  appendDynamicApiParam(params, 'ApiPath', input.apiPath)
  appendDynamicApiParam(params, 'ControllerName', input.controllerName)
  appendDynamicApiParam(params, 'IsSuccess', input.isSuccess)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'MaxExecutionTime', input.maxExecutionTime)
  appendDynamicApiParam(params, 'Method', input.method)
  appendDynamicApiParam(params, 'MinExecutionTime', input.minExecutionTime)
  appendDynamicApiParam(params, 'RequestTimeEnd', input.requestTimeEnd)
  appendDynamicApiParam(params, 'RequestTimeStart', input.requestTimeStart)
  appendDynamicApiParam(params, 'SessionId', input.sessionId)
  appendDynamicApiParam(params, 'StatusCode', input.statusCode)
  appendDynamicApiParam(params, 'TraceId', input.traceId)
  appendDynamicApiParam(params, 'UserId', input.userId)
  appendDynamicApiParam(params, 'UserName', input.userName)
  return params
}

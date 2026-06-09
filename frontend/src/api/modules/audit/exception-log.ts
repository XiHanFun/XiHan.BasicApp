import type { ApiId, PageResult } from '../../types'
import type { ExceptionLogDetailDto, ExceptionLogListItemDto, ExceptionLogPageQueryDto } from './exception-log.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const exceptionLogQueryApi = createDynamicApiClient('ExceptionLogQuery')

export const exceptionLogApi = {
  detail(id: ApiId) {
    return exceptionLogQueryApi.get<ExceptionLogDetailDto | null>(
      `ExceptionLogDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: ExceptionLogPageQueryDto) {
    return exceptionLogQueryApi.get<PageResult<ExceptionLogListItemDto>>(
      'ExceptionLogPage',
      toExceptionLogPageParams(input),
    )
  },
}

function toExceptionLogPageParams(input: ExceptionLogPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'ApplicationName', input.applicationName)
  appendDynamicApiParam(params, 'ApplicationVersion', input.applicationVersion)
  appendDynamicApiParam(params, 'DeviceType', input.deviceType)
  appendDynamicApiParam(params, 'EnvironmentName', input.environmentName)
  appendDynamicApiParam(params, 'ErrorCode', input.errorCode)
  appendDynamicApiParam(params, 'ExceptionLocation', input.exceptionLocation)
  appendDynamicApiParam(params, 'ExceptionSource', input.exceptionSource)
  appendDynamicApiParam(params, 'ExceptionTimeEnd', input.exceptionTimeEnd)
  appendDynamicApiParam(params, 'ExceptionTimeStart', input.exceptionTimeStart)
  appendDynamicApiParam(params, 'ExceptionType', input.exceptionType)
  appendDynamicApiParam(params, 'HandledBy', input.handledBy)
  appendDynamicApiParam(params, 'IsHandled', input.isHandled)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'RequestId', input.requestId)
  appendDynamicApiParam(params, 'RequestMethod', input.requestMethod)
  appendDynamicApiParam(params, 'RequestPath', input.requestPath)
  appendDynamicApiParam(params, 'SessionId', input.sessionId)
  appendDynamicApiParam(params, 'SeverityLevel', input.severityLevel)
  appendDynamicApiParam(params, 'StatusCode', input.statusCode)
  appendDynamicApiParam(params, 'TraceId', input.traceId)
  appendDynamicApiParam(params, 'UserId', input.userId)
  appendDynamicApiParam(params, 'UserName', input.userName)
  return params
}

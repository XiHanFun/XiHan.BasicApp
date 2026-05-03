import type { PageResult } from '../../types'
import type { ExceptionLogListItemDto, ExceptionLogPageQueryDto } from './exception-log.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
} from '../../base'

const exceptionLogQueryApi = createDynamicApiClient('ExceptionLogQuery')

export const exceptionLogApi = {
  page(input: ExceptionLogPageQueryDto) {
    return exceptionLogQueryApi.get<PageResult<ExceptionLogListItemDto>>(
      'ExceptionLogPage',
      toExceptionLogPageParams(input),
    )
  },
}

function toExceptionLogPageParams(input: ExceptionLogPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'ControllerName', input.controllerName)
  appendDynamicApiParam(params, 'ExceptionTimeEnd', input.exceptionTimeEnd)
  appendDynamicApiParam(params, 'ExceptionTimeStart', input.exceptionTimeStart)
  appendDynamicApiParam(params, 'ExceptionType', input.exceptionType)
  appendDynamicApiParam(params, 'IsHandled', input.isHandled)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'RequestPath', input.requestPath)
  appendDynamicApiParam(params, 'SeverityLevel', input.severityLevel)
  appendDynamicApiParam(params, 'TraceId', input.traceId)
  appendDynamicApiParam(params, 'UserId', input.userId)
  appendDynamicApiParam(params, 'UserName', input.userName)
  return params
}

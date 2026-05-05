import type { ApiId, PageResult } from '../../types'
import type { ApiLogDetailDto, ApiLogListItemDto, ApiLogPageQueryDto } from './api-log.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const apiLogQueryApi = createDynamicApiClient('ApiLogQuery')

export const apiLogApi = {
  detail(id: ApiId) {
    return apiLogQueryApi.get<ApiLogDetailDto | null>(
      `ApiLogDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: ApiLogPageQueryDto) {
    return apiLogQueryApi.get<PageResult<ApiLogListItemDto>>(
      'ApiLogPage',
      toApiLogPageParams(input),
    )
  },
}

function toApiLogPageParams(input: ApiLogPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'ApiPath', input.apiPath)
  appendDynamicApiParam(params, 'ApiVersion', input.apiVersion)
  appendDynamicApiParam(params, 'AppId', input.appId)
  appendDynamicApiParam(params, 'ClientId', input.clientId)
  appendDynamicApiParam(params, 'IsSignatureValid', input.isSignatureValid)
  appendDynamicApiParam(params, 'IsSuccess', input.isSuccess)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'MaxExecutionTime', input.maxExecutionTime)
  appendDynamicApiParam(params, 'Method', input.method)
  appendDynamicApiParam(params, 'MinExecutionTime', input.minExecutionTime)
  appendDynamicApiParam(params, 'RequestId', input.requestId)
  appendDynamicApiParam(params, 'RequestTimeEnd', input.requestTimeEnd)
  appendDynamicApiParam(params, 'RequestTimeStart', input.requestTimeStart)
  appendDynamicApiParam(params, 'SessionId', input.sessionId)
  appendDynamicApiParam(params, 'SignatureType', input.signatureType)
  appendDynamicApiParam(params, 'StatusCode', input.statusCode)
  appendDynamicApiParam(params, 'TraceId', input.traceId)
  appendDynamicApiParam(params, 'UserId', input.userId)
  appendDynamicApiParam(params, 'UserName', input.userName)
  return params
}

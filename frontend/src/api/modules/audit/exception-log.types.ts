import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export interface ExceptionLogPageQueryDto extends PageRequest {
  controllerName?: string | null
  exceptionTimeEnd?: DateTimeString | null
  exceptionTimeStart?: DateTimeString | null
  exceptionType?: string | null
  isHandled?: boolean | null
  keyword?: string | null
  requestPath?: string | null
  severityLevel?: number | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface ExceptionLogListItemDto extends BasicDto {
  actionName?: string | null
  applicationName?: string | null
  browser?: string | null
  controllerName?: string | null
  createdTime: DateTimeString
  deviceType?: string | null
  environmentName?: string | null
  errorCode?: string | null
  exceptionLocation?: string | null
  exceptionMessage: string
  exceptionTime: DateTimeString
  exceptionType: string
  isHandled: boolean
  operationIp?: string | null
  operationLocation?: string | null
  os?: string | null
  processId?: number | null
  requestId?: string | null
  requestMethod?: string | null
  requestPath?: string | null
  serverHostName?: string | null
  sessionId?: string | null
  severityLevel: number
  statusCode: number
  tenantId?: ApiId | null
  threadId?: number | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

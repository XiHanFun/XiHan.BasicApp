import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { DeviceType } from '../identity/user-session.types'

export interface ExceptionLogPageQueryDto extends PageRequest {
  applicationName?: string | null
  applicationVersion?: string | null
  deviceType?: DeviceType | null
  environmentName?: string | null
  errorCode?: string | null
  exceptionLocation?: string | null
  exceptionSource?: string | null
  exceptionTimeEnd?: DateTimeString | null
  exceptionTimeStart?: DateTimeString | null
  exceptionType?: string | null
  handledBy?: ApiId | null
  isHandled?: boolean | null
  keyword?: string | null
  requestMethod?: string | null
  requestPath?: string | null
  requestId?: string | null
  sessionId?: string | null
  severityLevel?: number | null
  statusCode?: number | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface ExceptionLogListItemDto extends BasicDto {
  actionName?: string | null
  applicationName?: string | null
  applicationVersion?: string | null
  browser?: string | null
  controllerName?: string | null
  createdTime: DateTimeString
  deviceInfo?: string | null
  deviceType: DeviceType
  environmentName?: string | null
  errorCode?: string | null
  exceptionLocation?: string | null
  exceptionMessage: string
  exceptionSource?: string | null
  exceptionStackTrace?: string | null
  exceptionTime: DateTimeString
  exceptionType: string
  handledRemark?: string | null
  handledBy?: ApiId | null
  handledTime?: DateTimeString | null
  extendData?: string | null
  isHandled: boolean
  operationIp?: string | null
  operationLocation?: string | null
  os?: string | null
  processId: number
  remark?: string | null
  requestBody?: string | null
  requestHeaders?: string | null
  requestId?: string | null
  requestMethod?: string | null
  requestParams?: string | null
  requestPath?: string | null
  serverHostName?: string | null
  sessionId?: string | null
  severityLevel: number
  statusCode: number
  threadId: number
  traceId?: string | null
  userAgent?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface ExceptionLogDetailDto extends ExceptionLogListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}

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
  applicationName?: string | null
  applicationVersion?: string | null
  createdTime: DateTimeString
  deviceType: DeviceType
  environmentName?: string | null
  errorCode?: string | null
  exceptionLocation?: string | null
  exceptionSource?: string | null
  exceptionTime: DateTimeString
  exceptionType: string
  handledBy?: ApiId | null
  handledTime?: DateTimeString | null
  isHandled: boolean
  requestId?: string | null
  requestMethod?: string | null
  requestPath?: string | null
  sessionId?: string | null
  severityLevel: number
  statusCode: number
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

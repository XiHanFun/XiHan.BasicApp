import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export enum AccessResult {
  Success = 0,
  Failed = 1,
  Forbidden = 2,
  Unauthorized = 3,
  NotFound = 4,
  ServerError = 5,
}

export interface AccessLogPageQueryDto extends PageRequest {
  accessResult?: AccessResult | null
  accessTimeEnd?: DateTimeString | null
  accessTimeStart?: DateTimeString | null
  keyword?: string | null
  maxExecutionTime?: number | null
  method?: string | null
  minExecutionTime?: number | null
  resourcePath?: string | null
  resourceType?: string | null
  sessionId?: string | null
  statusCode?: number | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface AccessLogListItemDto extends BasicDto {
  accessIp?: string | null
  accessLocation?: string | null
  accessResult: AccessResult
  accessTime: DateTimeString
  browser?: string | null
  createdTime: DateTimeString
  device?: string | null
  errorMessage?: string | null
  executionTime: number
  extendData?: string | null
  method?: string | null
  os?: string | null
  referer?: string | null
  remark?: string | null
  resourceName?: string | null
  resourcePath: string
  resourceType?: string | null
  sessionId?: string | null
  statusCode: number
  traceId?: string | null
  userAgent?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface AccessLogDetailDto extends AccessLogListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}

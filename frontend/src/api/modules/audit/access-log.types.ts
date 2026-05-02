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
  accessResult: AccessResult
  accessTime: DateTimeString
  createdTime: DateTimeString
  executionTime: number
  hasClientContext: boolean
  hasError: boolean
  hasExtension: boolean
  method?: string | null
  resourceName?: string | null
  resourcePath: string
  resourceType?: string | null
  sessionId?: string | null
  statusCode: number
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

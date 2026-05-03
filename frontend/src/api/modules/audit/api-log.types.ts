import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export interface ApiLogPageQueryDto extends PageRequest {
  apiPath?: string | null
  controllerName?: string | null
  actionName?: string | null
  isSuccess?: boolean | null
  keyword?: string | null
  maxExecutionTime?: number | null
  method?: string | null
  minExecutionTime?: number | null
  requestTimeEnd?: DateTimeString | null
  requestTimeStart?: DateTimeString | null
  sessionId?: string | null
  statusCode?: number | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface ApiLogListItemDto extends BasicDto {
  actionName?: string | null
  apiPath: string
  appId?: string | null
  browser?: string | null
  clientId?: string | null
  controllerName?: string | null
  createdTime: DateTimeString
  errorMessage?: string | null
  executionTime: number
  isSignatureValid: boolean
  isSuccess: boolean
  method?: string | null
  referer?: string | null
  requestId?: string | null
  requestIp?: string | null
  requestLocation?: string | null
  requestSize: number
  requestTime: DateTimeString
  responseSize: number
  responseTime: DateTimeString
  sessionId?: string | null
  signatureType?: string | null
  statusCode: number
  tenantId?: ApiId | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

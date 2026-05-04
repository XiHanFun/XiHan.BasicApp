import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export enum SignatureType {
  None = 0,
  HmacSha256 = 1,
  HmacSha512 = 2,
  RsaSha256 = 3,
  RsaSha512 = 4,
  Sm2 = 5,
  Sm3 = 6,
  Ed25519 = 7,
  Md5 = 99,
}

export interface ApiLogPageQueryDto extends PageRequest {
  apiPath?: string | null
  apiVersion?: string | null
  appId?: string | null
  clientId?: string | null
  isSignatureValid?: boolean | null
  isSuccess?: boolean | null
  keyword?: string | null
  maxExecutionTime?: number | null
  method?: string | null
  minExecutionTime?: number | null
  requestId?: string | null
  requestTimeEnd?: DateTimeString | null
  requestTimeStart?: DateTimeString | null
  sessionId?: string | null
  signatureType?: SignatureType | null
  statusCode?: number | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface ApiLogListItemDto extends BasicDto {
  actionName?: string | null
  apiName?: string | null
  apiPath: string
  apiVersion?: string | null
  appId?: string | null
  browser?: string | null
  clientId?: string | null
  controllerName?: string | null
  createdTime: DateTimeString
  errorMessage?: string | null
  executionTime: number
  exceptionStackTrace?: string | null
  extendData?: string | null
  isSignatureValid: boolean
  isSuccess: boolean
  method: string
  referer?: string | null
  requestId?: string | null
  requestBody?: string | null
  requestHeaders?: string | null
  requestIp?: string | null
  requestLocation?: string | null
  requestParams?: string | null
  requestSize: number
  requestTime: DateTimeString
  remark?: string | null
  responseBody?: string | null
  responseHeaders?: string | null
  responseSize: number
  responseTime?: DateTimeString | null
  sessionId?: string | null
  signatureType: SignatureType
  statusCode: number
  traceId?: string | null
  userAgent?: string | null
  userId?: ApiId | null
  userName?: string | null
}

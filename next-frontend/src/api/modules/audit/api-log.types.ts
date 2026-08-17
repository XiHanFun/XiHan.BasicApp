import type { ApiId, BasicDto, DateTimeString, NumericString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum SignatureType {
  None = 'None',
  HmacSha256 = 'HmacSha256',
  HmacSha512 = 'HmacSha512',
  RsaSha256 = 'RsaSha256',
  RsaSha512 = 'RsaSha512',
  Sm2 = 'Sm2',
  Sm3 = 'Sm3',
  Ed25519 = 'Ed25519',
  Md5 = 'Md5',
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
  executionTime: NumericString
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
  requestSize: NumericString
  requestTime: DateTimeString
  remark?: string | null
  responseBody?: string | null
  responseHeaders?: string | null
  responseSize: NumericString
  responseTime?: DateTimeString | null
  sessionId?: string | null
  signatureType: SignatureType
  statusCode: number
  traceId?: string | null
  userAgent?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface ApiLogDetailDto extends ApiLogListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}

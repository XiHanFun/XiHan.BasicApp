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
  apiName?: string | null
  apiPath: string
  apiVersion?: string | null
  appId?: string | null
  clientId?: string | null
  createdTime: DateTimeString
  executionTime: number
  isSignatureValid: boolean
  isSuccess: boolean
  method: string
  requestId?: string | null
  requestSize: number
  requestTime: DateTimeString
  responseSize: number
  responseTime?: DateTimeString | null
  sessionId?: string | null
  signatureType: SignatureType
  statusCode: number
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

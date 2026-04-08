import type { AnyRecord } from '../helpers'
import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId } from '../helpers'

const api = useBaseApi('ApiLog')

export interface SysApiLog {
  basicId?: string
  userId?: string
  userName?: string
  requestId?: string
  sessionId?: string
  traceId?: string
  clientId?: string
  appId?: string
  isSignatureValid?: boolean
  signatureType?: string
  apiPath?: string
  apiName?: string
  apiDescription?: string
  method?: string
  controllerName?: string
  actionName?: string
  requestParams?: string
  requestBody?: string
  responseBody?: string
  statusCode?: number
  requestHeaders?: string
  responseHeaders?: string
  requestIp?: string
  requestLocation?: string
  userAgent?: string
  browser?: string
  os?: string
  referer?: string
  requestTime?: string
  responseTime?: string
  executionTime?: number
  requestSize?: number
  responseSize?: number
  isSuccess?: boolean
  errorMessage?: string
  exceptionStackTrace?: string
  apiVersion?: string
  businessType?: string
  extendData?: string
  remark?: string
  createdTime?: string
}

function normalize(raw: Record<string, any>): SysApiLog {
  return {
    basicId: toId(raw.basicId),
    userId: toId(raw.userId),
    userName: raw.userName ?? '',
    requestId: raw.requestId ?? '',
    sessionId: raw.sessionId ?? '',
    traceId: raw.traceId ?? '',
    clientId: raw.clientId ?? '',
    appId: raw.appId ?? '',
    isSignatureValid: raw.isSignatureValid ?? true,
    signatureType: String(raw.signatureType ?? ''),
    apiPath: raw.apiPath ?? '',
    apiName: raw.apiName ?? '',
    apiDescription: raw.apiDescription ?? '',
    method: raw.method ?? '',
    controllerName: raw.controllerName ?? '',
    actionName: raw.actionName ?? '',
    requestParams: raw.requestParams ?? '',
    requestBody: raw.requestBody ?? '',
    responseBody: raw.responseBody ?? '',
    statusCode: raw.statusCode ?? 0,
    requestHeaders: raw.requestHeaders ?? '',
    responseHeaders: raw.responseHeaders ?? '',
    requestIp: raw.requestIp ?? '',
    requestLocation: raw.requestLocation ?? '',
    userAgent: raw.userAgent ?? '',
    browser: raw.browser ?? '',
    os: raw.os ?? '',
    referer: raw.referer ?? '',
    requestTime: raw.requestTime ?? '',
    responseTime: raw.responseTime ?? '',
    executionTime: raw.executionTime ?? 0,
    requestSize: raw.requestSize ?? 0,
    responseSize: raw.responseSize ?? 0,
    isSuccess: raw.isSuccess ?? true,
    errorMessage: raw.errorMessage ?? '',
    exceptionStackTrace: raw.exceptionStackTrace ?? '',
    apiVersion: raw.apiVersion ?? '',
    businessType: raw.businessType ?? '',
    extendData: raw.extendData ?? '',
    remark: raw.remark ?? '',
    createdTime: raw.createdTime ?? '',
  }
}

export const apiLogApi = {
  page: (params: PageQuery & Record<string, any>) =>
    api.page(params).then(res => ({
      total: res.total,
      items: (res.items as AnyRecord[]).map(item => normalize(item)),
    })),
  clear: () => api.clear(),
}

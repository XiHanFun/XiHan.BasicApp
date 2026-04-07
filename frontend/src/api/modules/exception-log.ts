import type { AnyRecord } from '../helpers'
import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId } from '../helpers'

const api = useBaseApi('ExceptionLog')

// -------- 类型 --------
export interface SysExceptionLog {
  basicId?: string
  userId?: string
  userName?: string
  requestId?: string
  sessionId?: string
  exceptionType?: string
  exceptionMessage?: string
  exceptionStackTrace?: string
  innerExceptionType?: string
  innerExceptionMessage?: string
  innerExceptionStackTrace?: string
  exceptionSource?: string
  exceptionLocation?: string
  severityLevel?: number
  requestPath?: string
  requestMethod?: string
  controllerName?: string
  actionName?: string
  requestParams?: string
  requestBody?: string
  requestHeaders?: string
  statusCode?: number
  operationIp?: string
  operationLocation?: string
  userAgent?: string
  browser?: string
  os?: string
  deviceType?: string
  deviceInfo?: string
  applicationName?: string
  applicationVersion?: string
  environmentName?: string
  serverHostName?: string
  threadId?: number
  processId?: number
  exceptionTime?: string
  isHandled?: boolean
  handledTime?: string
  handledBy?: string
  handledByName?: string
  handledRemark?: string
  businessModule?: string
  businessId?: string
  businessType?: string
  errorCode?: string
  extendData?: string
  remark?: string
  createdTime?: string
}

function normalize(raw: Record<string, any>): SysExceptionLog {
  return {
    basicId: toId(raw.basicId),
    userId: toId(raw.userId),
    userName: raw.userName ?? '',
    requestId: raw.requestId ?? '',
    sessionId: raw.sessionId ?? '',
    exceptionType: raw.exceptionType ?? '',
    exceptionMessage: raw.exceptionMessage ?? '',
    exceptionStackTrace: raw.exceptionStackTrace ?? '',
    innerExceptionType: raw.innerExceptionType ?? '',
    innerExceptionMessage: raw.innerExceptionMessage ?? '',
    innerExceptionStackTrace: raw.innerExceptionStackTrace ?? '',
    exceptionSource: raw.exceptionSource ?? '',
    exceptionLocation: raw.exceptionLocation ?? '',
    severityLevel: raw.severityLevel ?? 0,
    requestPath: raw.requestPath ?? '',
    requestMethod: raw.requestMethod ?? '',
    controllerName: raw.controllerName ?? '',
    actionName: raw.actionName ?? '',
    requestParams: raw.requestParams ?? '',
    requestBody: raw.requestBody ?? '',
    requestHeaders: raw.requestHeaders ?? '',
    statusCode: raw.statusCode ?? 0,
    operationIp: raw.operationIp ?? '',
    operationLocation: raw.operationLocation ?? '',
    userAgent: raw.userAgent ?? '',
    browser: raw.browser ?? '',
    os: raw.os ?? '',
    deviceType: String(raw.deviceType ?? ''),
    deviceInfo: raw.deviceInfo ?? '',
    applicationName: raw.applicationName ?? '',
    applicationVersion: raw.applicationVersion ?? '',
    environmentName: raw.environmentName ?? '',
    serverHostName: raw.serverHostName ?? '',
    threadId: raw.threadId ?? 0,
    processId: raw.processId ?? 0,
    exceptionTime: raw.exceptionTime ?? '',
    isHandled: raw.isHandled ?? false,
    handledTime: raw.handledTime ?? '',
    handledBy: toId(raw.handledBy),
    handledByName: raw.handledByName ?? '',
    handledRemark: raw.handledRemark ?? '',
    businessModule: raw.businessModule ?? '',
    businessId: raw.businessId ?? '',
    businessType: raw.businessType ?? '',
    errorCode: raw.errorCode ?? '',
    extendData: raw.extendData ?? '',
    remark: raw.remark ?? '',
    createdTime: raw.createdTime ?? '',
  }
}

// -------- API --------
export const exceptionLogApi = {
  page: (params: PageQuery & Record<string, any>) =>
    api.page(params).then(res => ({
      total: res.total,
      items: (res.items as AnyRecord[]).map(item => normalize(item)),
    })),
  clear: () => api.clear(),
}

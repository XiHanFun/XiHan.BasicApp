import type { PageResult, SysExceptionLog } from '~/types'
import { buildPageRequest, normalizePageResult, toId } from '../helpers'
import requestClient from '../request'

const API = '/api/ExceptionLog'

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

export function getExceptionLogPageApi(params: Record<string, any>): Promise<PageResult<SysExceptionLog>> {
  return requestClient
    .post<any>(`${API}/Page`, buildPageRequest(params))
    .then(data => normalizePageResult(data, normalize))
}

export function clearExceptionLogApi() {
  return requestClient.delete<boolean>(`${API}/Clear`)
}

import type { PageResult, SysAuditLog } from '~/types'
import { buildPageRequest, normalizePageResult, toId } from '../helpers'
import requestClient from '../request'

const API = '/api/AuditLog'

function normalize(raw: Record<string, any>): SysAuditLog {
  return {
    basicId: toId(raw.basicId),
    userId: toId(raw.userId),
    userName: raw.userName ?? '',
    realName: raw.realName ?? '',
    departmentId: toId(raw.departmentId),
    departmentName: raw.departmentName ?? '',
    auditType: raw.auditType ?? '',
    operationType: String(raw.operationType ?? ''),
    entityType: raw.entityType ?? '',
    entityId: raw.entityId ?? '',
    entityName: raw.entityName ?? '',
    tableName: raw.tableName ?? '',
    primaryKey: raw.primaryKey ?? '',
    primaryKeyValue: raw.primaryKeyValue ?? '',
    module: raw.module ?? '',
    function: raw.function ?? '',
    description: raw.description ?? '',
    beforeData: raw.beforeData ?? '',
    afterData: raw.afterData ?? '',
    changedFields: raw.changedFields ?? '',
    changeDescription: raw.changeDescription ?? '',
    requestPath: raw.requestPath ?? '',
    requestMethod: raw.requestMethod ?? '',
    requestParams: raw.requestParams ?? '',
    responseResult: raw.responseResult ?? '',
    executionTime: raw.executionTime ?? 0,
    operationIp: raw.operationIp ?? '',
    operationLocation: raw.operationLocation ?? '',
    browser: raw.browser ?? '',
    os: raw.os ?? '',
    deviceType: raw.deviceType ?? '',
    deviceInfo: raw.deviceInfo ?? '',
    userAgent: raw.userAgent ?? '',
    sessionId: raw.sessionId ?? '',
    requestId: raw.requestId ?? '',
    businessId: raw.businessId ?? '',
    businessType: raw.businessType ?? '',
    isSuccess: raw.isSuccess ?? false,
    exceptionMessage: raw.exceptionMessage ?? '',
    exceptionStackTrace: raw.exceptionStackTrace ?? '',
    riskLevel: raw.riskLevel ?? 0,
    auditTime: raw.auditTime ?? '',
    extendData: raw.extendData ?? '',
    remark: raw.remark ?? '',
    createdTime: raw.createdTime ?? '',
  }
}

export function getAuditLogPageApi(params: Record<string, any>): Promise<PageResult<SysAuditLog>> {
  return requestClient
    .post<any>(`${API}/Page`, buildPageRequest(params))
    .then(data => normalizePageResult(data, normalize))
}

export function clearAuditLogApi() {
  return requestClient.delete<boolean>(`${API}/Clear`)
}

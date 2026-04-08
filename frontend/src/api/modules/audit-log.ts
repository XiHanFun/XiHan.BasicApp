import type { AnyRecord } from '../helpers'
import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId } from '../helpers'

const api = useBaseApi('AuditLog')

// -------- 类型 --------
export interface SysAuditLog {
  basicId?: string
  userId?: string
  userName?: string
  departmentId?: string
  auditType?: string
  operationType?: string
  entityType?: string
  entityId?: string
  entityName?: string
  tableName?: string
  primaryKey?: string
  primaryKeyValue?: string
  module?: string
  function?: string
  description?: string
  beforeData?: string
  afterData?: string
  changedFields?: string
  changeDescription?: string
  requestPath?: string
  requestMethod?: string
  requestParams?: string
  responseResult?: string
  executionTime?: number
  operationIp?: string
  operationLocation?: string
  browser?: string
  os?: string
  deviceType?: string
  deviceInfo?: string
  userAgent?: string
  sessionId?: string
  requestId?: string
  traceId?: string
  businessId?: string
  businessType?: string
  isSuccess?: boolean
  exceptionMessage?: string
  exceptionStackTrace?: string
  riskLevel?: number
  auditTime?: string
  extendData?: string
  remark?: string
  createdTime?: string
}

function normalize(raw: Record<string, any>): SysAuditLog {
  return {
    basicId: toId(raw.basicId),
    userId: toId(raw.userId),
    userName: raw.userName ?? '',
    departmentId: toId(raw.departmentId),
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
    traceId: raw.traceId ?? '',
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

// -------- API --------
export const auditLogApi = {
  page: (params: PageQuery & Record<string, any>) =>
    api.page(params).then((res) => ({
      total: res.total,
      items: (res.items as AnyRecord[]).map((item) => normalize(item)),
    })),
  clear: () => api.clear(),
}

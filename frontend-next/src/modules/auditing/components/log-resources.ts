import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, ResponseOf } from '~/contracts'
import {
  accessLogs,
  conditionHits,
  diffLogs,
  exceptionLogs,
  loginLogs,
  openApiLogs,
  operationLogs,
  permissionChangeLogs,
  toPageRequest,
  toSchemaPageResult,
} from '~/contracts'

export type AccessLogRow = ResponseOf<'access-logs.list'>['items'][number]
export type ExceptionLogRow = ResponseOf<'exception-logs.list'>['items'][number]
export type LoginLogRow = ResponseOf<'login-logs.list'>['items'][number]
export type OperationLogRow = ResponseOf<'operation-logs.list'>['items'][number]
export type DiffLogRow = ResponseOf<'diff-logs.list'>['items'][number]
export type PermissionChangeLogRow = ResponseOf<'permission-change-logs.list'>['items'][number]
export type ConditionHitRow = ResponseOf<'condition-hits.list'>['items'][number]
export type OpenApiLogRow = ResponseOf<'open-api-logs.list'>['items'][number]

/** 各日志共有的筛选：发生时间按区间（拆成 ge / le，后端据此限定扫描的月表），链路、会话、用户、地址精确匹配 */
const COMMON: PageQueryMapping = {
  ranges: ['occurredAt'],
  filters: { traceId: 'eq', sessionId: 'eq', userId: 'eq', userName: 'contains', clientIp: 'eq' },
}

function mapping(extra: PageQueryMapping): PageQueryMapping {
  return {
    ranges: [...(COMMON.ranges ?? []), ...(extra.ranges ?? [])],
    multiples: extra.multiples ?? [],
    filters: { ...COMMON.filters, ...extra.filters },
  }
}

/** 只读列表的资源：分页即可（导出由 SchemaPage 按当前筛选逐页取回再在本地生成） */
function readonly<TRow>(list: (params: SchemaQueryParams) => Promise<{ items: TRow[], total: number | string, page: { index?: number, size?: number } }>): SchemaResource<TRow> {
  return { page: async params => toSchemaPageResult(await list(params), params) }
}

export const ACCESS_LOG_QUERY = mapping({ multiples: ['result'], filters: { operationId: 'contains', method: 'eq', path: 'contains', statusCode: 'eq', errorCode: 'eq' } })
export const EXCEPTION_LOG_QUERY = mapping({ filters: { exceptionType: 'contains', operationId: 'eq', path: 'contains', machineName: 'eq', environmentName: 'eq' } })
export const LOGIN_LOG_QUERY = mapping({ multiples: ['event'], filters: { succeeded: 'eq', errorCode: 'eq', accountTenantId: 'eq' } })
export const DIFF_LOG_QUERY = mapping({ multiples: ['kind'], filters: { entityType: 'contains', tableName: 'eq', entityId: 'eq', operationId: 'eq' } })
export const PERMISSION_CHANGE_LOG_QUERY = mapping({ multiples: ['changeType'], filters: { roleId: 'eq', targetUserName: 'contains', targetTenantId: 'eq', permissionCodes: 'contains' } })
export const CONDITION_HIT_QUERY = mapping({ multiples: ['action'], filters: { source: 'eq', phase: 'eq', isBlocked: 'eq', policyCode: 'contains', path: 'contains' } })
export const OPEN_API_LOG_QUERY = mapping({ filters: { clientId: 'eq', method: 'eq', path: 'contains', statusCode: 'eq', succeeded: 'eq', signatureValid: 'eq' } })
/** 操作日志查的是框架的审计表：没有会话与地址列，按用户主键筛 */
export const OPERATION_LOG_QUERY: PageQueryMapping = { ranges: ['occurredAt'], filters: { operationId: 'contains', succeeded: 'eq', errorCode: 'eq', traceId: 'eq', userId: 'eq' } }

export const accessLogsResource = () => readonly<AccessLogRow>(params => accessLogs.list(toPageRequest(params, ACCESS_LOG_QUERY)))
export const exceptionLogsResource = () => readonly<ExceptionLogRow>(params => exceptionLogs.list(toPageRequest(params, EXCEPTION_LOG_QUERY)))
export const loginLogsResource = () => readonly<LoginLogRow>(params => loginLogs.list(toPageRequest(params, LOGIN_LOG_QUERY)))
export const diffLogsResource = () => readonly<DiffLogRow>(params => diffLogs.list(toPageRequest(params, DIFF_LOG_QUERY)))
export const permissionChangeLogsResource = () => readonly<PermissionChangeLogRow>(params => permissionChangeLogs.list(toPageRequest(params, PERMISSION_CHANGE_LOG_QUERY)))
export const conditionHitsResource = () => readonly<ConditionHitRow>(params => conditionHits.list(toPageRequest(params, CONDITION_HIT_QUERY)))
export const openApiLogsResource = () => readonly<OpenApiLogRow>(params => openApiLogs.list(toPageRequest(params, OPEN_API_LOG_QUERY)))
export const operationLogsResource = () => readonly<OperationLogRow>(params => operationLogs.list(toPageRequest(params, OPERATION_LOG_QUERY)))

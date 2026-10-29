import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { OperationId, PageQueryMapping, PageRequest, ResponseOf } from '~/contracts'
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

type ListOperation<TRow> = (request: PageRequest) => Promise<{ items: TRow[], total: number | string, page: { index?: number, size?: number } }>

/** 只读列表的资源：分页即可（本地导出由 SchemaPage 按当前筛选逐页取回再生成 CSV） */
function readonly<TRow>(list: ListOperation<TRow>, query: PageQueryMapping): SchemaResource<TRow> {
  return { page: async params => toSchemaPageResult(await list(toPageRequest(params, query)), params) }
}

/**
 * 另能提交到导出中心的只读列表（4.x 有服务端导出的那几种日志）：业务类型就是列表操作 ID，
 * 后台以发起人的身份逐页调它，查询快照与列表用同一份分页请求（同一份筛选登记）。
 */
function exportable<TRow>(businessType: Extract<OperationId, `${string}.list`>, list: ListOperation<TRow>, query: PageQueryMapping): SchemaResource<TRow> {
  return { ...readonly(list, query), export: { businessType, buildQuery: (params: SchemaQueryParams) => toPageRequest(params, query) } }
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

export const accessLogsResource = () => exportable<AccessLogRow>('access-logs.list', request => accessLogs.list(request), ACCESS_LOG_QUERY)
export const exceptionLogsResource = () => exportable<ExceptionLogRow>('exception-logs.list', request => exceptionLogs.list(request), EXCEPTION_LOG_QUERY)
export const loginLogsResource = () => exportable<LoginLogRow>('login-logs.list', request => loginLogs.list(request), LOGIN_LOG_QUERY)
export const diffLogsResource = () => exportable<DiffLogRow>('diff-logs.list', request => diffLogs.list(request), DIFF_LOG_QUERY)
// 权限变更日志与条件命中在 4.x 没有服务端导出，只留本地导出
export const permissionChangeLogsResource = () => readonly<PermissionChangeLogRow>(request => permissionChangeLogs.list(request), PERMISSION_CHANGE_LOG_QUERY)
export const conditionHitsResource = () => readonly<ConditionHitRow>(request => conditionHits.list(request), CONDITION_HIT_QUERY)
export const openApiLogsResource = () => exportable<OpenApiLogRow>('open-api-logs.list', request => openApiLogs.list(request), OPEN_API_LOG_QUERY)
export const operationLogsResource = () => exportable<OperationLogRow>('operation-logs.list', request => operationLogs.list(request), OPERATION_LOG_QUERY)

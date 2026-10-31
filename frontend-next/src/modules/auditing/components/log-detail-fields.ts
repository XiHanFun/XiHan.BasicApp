import type { LogDetailField, LogDetailSchema } from './log-detail.types'
import type { Translate } from './log-options'
import {
  accessResultOptions,
  conditionActionOptions,
  conditionPhaseOptions,
  conditionSourceOptions,
  diffKindOptions,
  loginEventOptions,
  permissionChangeOptions,
} from './log-options'
import {
  accessResultTone,
  booleanTone,
  conditionActionTone,
  diffKindTone,
  loginEventTone,
  permissionChangeTone,
} from './log-tones'

/**
 * 各日志类型「详情」的单一事实源：抬头放什么、字段怎么分组、哪些值可复制或可追踪。
 *
 * 每个日志页与链路追踪的详情抽屉共用同一套定义（标签、枚举选项、取值类型），工厂接收 i18n 的 t，在 computed 中调用即可随语言切换。
 * 字段键就是 6.0 响应 DTO 的属性名；枚举是契约字面量，文案由 log-options 按字面量给出。
 */

/** 几种日志共有的追踪标识：可复制，trace-id 与会话点开即追踪 */
function traceFields(t: Translate, keys: { sessionId?: boolean, userId?: boolean } = {}): LogDetailField[] {
  const { sessionId = true, userId = true } = keys
  return [
    { key: 'traceId', label: t('auditing.common.trace_id'), group: 'trace', mono: true, trace: 'TraceId' },
    ...(sessionId ? [{ key: 'sessionId', label: t('auditing.common.session_id'), group: 'trace', mono: true, trace: 'SessionId' } satisfies LogDetailField] : []),
    { key: 'id', label: t('auditing.common.id'), group: 'trace', mono: true },
    ...(userId ? [{ key: 'userId', label: t('auditing.common.user_id'), group: 'trace', mono: true, trace: 'UserId' } satisfies LogDetailField] : []),
  ]
}

/** 客户端一组：地址可追踪，浏览器、系统、设备由后端从 User-Agent 解析 */
function clientFields(t: Translate, parsed: boolean): LogDetailField[] {
  return parsed
    ? [
        { key: 'browser', label: t('auditing.common.browser'), group: 'client' },
        { key: 'os', label: t('auditing.common.os'), group: 'client' },
        { key: 'device', label: t('auditing.common.device'), group: 'client' },
      ]
    : []
}

export function operationLogDetailSchema(t: Translate): LogDetailSchema {
  return {
    header: {
      title: ['operationId'],
      status: 'succeeded',
      badges: ['durationMs'],
      meta: ['userName', 'occurredAt', 'clientIp'],
      timeKey: 'occurredAt',
    },
    fields: [
      { key: 'operationId', label: t('auditing.common.operation_id'), mono: true },
      { key: 'description', label: t('auditing.operation_logs.description') },
      { key: 'succeeded', label: t('auditing.common.succeeded'), type: 'boolean', trueText: t('auditing.common.success'), falseText: t('auditing.common.failed'), tone: booleanTone },
      { key: 'durationMs', label: t('auditing.common.duration'), type: 'duration' },
      { key: 'userName', label: t('auditing.common.user_name'), trace: 'UserName' },
      { key: 'occurredAt', label: t('auditing.common.occurred_at'), type: 'date' },
      { key: 'errorCode', label: t('auditing.common.error_code'), mono: true },
      { key: 'resource', label: t('auditing.operation_logs.resource'), mono: true, span: 'full' },
      { key: 'clientIp', label: t('auditing.common.client_ip'), trace: 'ClientIp' },
      { key: 'location', label: t('auditing.operation_logs.location'), group: 'client' },
      ...clientFields(t, true),
      { key: 'sequence', label: t('auditing.operation_logs.sequence'), group: 'trace', mono: true },
      ...traceFields(t),
      { key: 'errorMessage', label: t('auditing.common.error_message'), type: 'code' },
      { key: 'userAgent', label: t('auditing.common.user_agent'), type: 'code' },
      { key: 'details', label: t('auditing.operation_logs.details'), type: 'code' },
    ],
  }
}

export function accessLogDetailSchema(t: Translate): LogDetailSchema {
  return {
    header: {
      title: ['method', 'operationId'],
      status: 'result',
      badges: ['statusCode', 'durationMs'],
      meta: ['userName', 'occurredAt', 'clientIp'],
      timeKey: 'occurredAt',
    },
    fields: [
      { key: 'method', label: t('auditing.common.method') },
      { key: 'operationId', label: t('auditing.common.operation_id'), mono: true },
      { key: 'result', label: t('auditing.access_logs.result'), options: accessResultOptions(t), type: 'enum', tone: accessResultTone },
      { key: 'statusCode', label: t('auditing.common.status_code') },
      { key: 'durationMs', label: t('auditing.common.duration'), type: 'duration' },
      { key: 'userName', label: t('auditing.common.user_name'), trace: 'UserName' },
      { key: 'occurredAt', label: t('auditing.common.occurred_at'), type: 'date' },
      { key: 'clientIp', label: t('auditing.common.client_ip'), trace: 'ClientIp' },
      { key: 'errorCode', label: t('auditing.common.error_code'), mono: true },
      { key: 'protocol', label: t('auditing.common.protocol'), group: 'request' },
      { key: 'path', label: t('auditing.common.path'), group: 'request', mono: true, span: 'full' },
      { key: 'referer', label: t('auditing.common.referer'), group: 'request', mono: true, span: 'full' },
      ...clientFields(t, true),
      ...traceFields(t),
      { key: 'errorMessage', label: t('auditing.common.error_message'), type: 'code' },
      { key: 'userAgent', label: t('auditing.common.user_agent'), type: 'code' },
    ],
  }
}

export function openApiLogDetailSchema(t: Translate): LogDetailSchema {
  return {
    header: {
      title: ['method', 'path'],
      status: 'succeeded',
      badges: ['statusCode', 'durationMs'],
      meta: ['clientId', 'userName', 'occurredAt', 'clientIp'],
      timeKey: 'occurredAt',
    },
    fields: [
      { key: 'method', label: t('auditing.common.method') },
      { key: 'path', label: t('auditing.common.path'), mono: true },
      { key: 'succeeded', label: t('auditing.common.succeeded'), type: 'boolean', trueText: t('auditing.common.success'), falseText: t('auditing.common.failed'), tone: booleanTone },
      { key: 'statusCode', label: t('auditing.common.status_code') },
      { key: 'durationMs', label: t('auditing.common.duration'), type: 'duration' },
      { key: 'clientId', label: t('auditing.open_api_logs.client_id'), mono: true },
      { key: 'userName', label: t('auditing.common.user_name'), trace: 'UserName' },
      { key: 'occurredAt', label: t('auditing.common.occurred_at'), type: 'date' },
      { key: 'clientIp', label: t('auditing.common.client_ip'), trace: 'ClientIp' },
      { key: 'appName', label: t('auditing.open_api_logs.app_name') },
      { key: 'operationId', label: t('auditing.common.operation_id'), mono: true },
      { key: 'signatureValid', label: t('auditing.open_api_logs.signature_valid'), type: 'boolean', trueText: t('auditing.open_api_logs.signature_valid_yes'), falseText: t('auditing.open_api_logs.signature_valid_no') },
      { key: 'signatureAlgorithm', label: t('auditing.open_api_logs.signature_algorithm') },
      { key: 'errorCode', label: t('auditing.common.error_code'), mono: true },
      { key: 'requestSize', label: t('auditing.open_api_logs.request_size'), group: 'request', type: 'bytes' },
      { key: 'responseSize', label: t('auditing.open_api_logs.response_size'), group: 'request', type: 'bytes' },
      ...traceFields(t),
      { key: 'errorMessage', label: t('auditing.common.error_message'), type: 'code' },
      { key: 'requestBody', label: t('auditing.open_api_logs.request_body'), type: 'code' },
      { key: 'responseBody', label: t('auditing.open_api_logs.response_body'), type: 'code' },
      { key: 'userAgent', label: t('auditing.common.user_agent'), type: 'code' },
    ],
  }
}

export function loginLogDetailSchema(t: Translate): LogDetailSchema {
  return {
    header: {
      title: ['userName', 'event'],
      status: 'succeeded',
      meta: ['occurredAt', 'clientIp'],
      timeKey: 'occurredAt',
    },
    fields: [
      { key: 'userName', label: t('auditing.common.user_name'), trace: 'UserName' },
      { key: 'event', label: t('auditing.login_logs.event'), options: loginEventOptions(t), type: 'enum', tone: loginEventTone },
      { key: 'succeeded', label: t('auditing.common.succeeded'), type: 'boolean', trueText: t('auditing.common.success'), falseText: t('auditing.common.failed'), tone: booleanTone },
      { key: 'occurredAt', label: t('auditing.common.occurred_at'), type: 'date' },
      { key: 'clientIp', label: t('auditing.common.client_ip'), trace: 'ClientIp' },
      { key: 'accountTenantId', label: t('auditing.login_logs.account_tenant'), mono: true },
      { key: 'errorCode', label: t('auditing.common.error_code'), mono: true },
      { key: 'detail', label: t('auditing.login_logs.detail'), span: 'full' },
      { key: 'impersonatorUserId', label: t('auditing.login_logs.impersonator'), group: 'trace', mono: true, trace: 'UserId' },
      ...clientFields(t, true),
      ...traceFields(t),
      { key: 'errorMessage', label: t('auditing.common.error_message'), type: 'code' },
      { key: 'userAgent', label: t('auditing.common.user_agent'), type: 'code' },
    ],
  }
}

export function exceptionLogDetailSchema(t: Translate): LogDetailSchema {
  return {
    header: {
      title: ['exceptionType'],
      meta: ['userName', 'occurredAt', 'clientIp'],
      timeKey: 'occurredAt',
    },
    fields: [
      { key: 'exceptionType', label: t('auditing.exception_logs.exception_type'), mono: true },
      { key: 'userName', label: t('auditing.common.user_name'), trace: 'UserName' },
      { key: 'occurredAt', label: t('auditing.common.occurred_at'), type: 'date' },
      { key: 'clientIp', label: t('auditing.common.client_ip'), trace: 'ClientIp' },
      { key: 'operationId', label: t('auditing.common.operation_id'), mono: true },
      { key: 'source', label: t('auditing.exception_logs.source') },
      { key: 'location', label: t('auditing.exception_logs.location'), mono: true, span: 'full' },
      { key: 'method', label: t('auditing.common.method'), group: 'request' },
      { key: 'protocol', label: t('auditing.common.protocol'), group: 'request' },
      { key: 'path', label: t('auditing.common.path'), group: 'request', mono: true, span: 'full' },
      { key: 'machineName', label: t('auditing.exception_logs.machine_name'), group: 'runtime' },
      { key: 'environmentName', label: t('auditing.exception_logs.environment_name'), group: 'runtime' },
      { key: 'appVersion', label: t('auditing.exception_logs.app_version'), group: 'runtime' },
      ...traceFields(t),
      // 异常消息与堆栈是排查的主角，排在代码块最前
      { key: 'message', label: t('auditing.exception_logs.message'), type: 'code' },
      { key: 'stackTrace', label: t('auditing.exception_logs.stack_trace'), type: 'code' },
      { key: 'userAgent', label: t('auditing.common.user_agent'), type: 'code' },
    ],
  }
}

export function diffLogDetailSchema(t: Translate): LogDetailSchema {
  return {
    header: {
      title: ['kind', 'entityType'],
      meta: ['userName', 'occurredAt', 'clientIp'],
      timeKey: 'occurredAt',
    },
    fields: [
      { key: 'kind', label: t('auditing.diff_logs.kind'), options: diffKindOptions(t), type: 'enum', tone: diffKindTone },
      { key: 'entityType', label: t('auditing.diff_logs.entity_type'), mono: true },
      { key: 'userName', label: t('auditing.common.user_name'), trace: 'UserName' },
      { key: 'occurredAt', label: t('auditing.common.occurred_at'), type: 'date' },
      { key: 'clientIp', label: t('auditing.common.client_ip'), trace: 'ClientIp' },
      { key: 'tableName', label: t('auditing.diff_logs.table_name'), mono: true },
      { key: 'entityId', label: t('auditing.diff_logs.entity_id'), mono: true },
      { key: 'operationId', label: t('auditing.common.operation_id'), mono: true },
      { key: 'changedFields', label: t('auditing.diff_logs.changed_fields'), span: 'full' },
      ...traceFields(t),
      { key: 'before', label: t('auditing.diff_logs.before'), type: 'code' },
      { key: 'after', label: t('auditing.diff_logs.after'), type: 'code' },
    ],
  }
}

export function permissionChangeLogDetailSchema(t: Translate): LogDetailSchema {
  return {
    header: {
      title: ['roleName', 'targetUserName', 'editionCode'],
      status: 'changeType',
      meta: ['userName', 'occurredAt', 'clientIp'],
      timeKey: 'occurredAt',
    },
    fields: [
      { key: 'changeType', label: t('auditing.permission_change_logs.change_type'), options: permissionChangeOptions(t), type: 'enum', tone: permissionChangeTone },
      { key: 'roleName', label: t('auditing.permission_change_logs.role_name') },
      { key: 'targetUserName', label: t('auditing.permission_change_logs.target_user_name') },
      { key: 'editionCode', label: t('auditing.permission_change_logs.edition_code'), mono: true },
      { key: 'userName', label: t('auditing.permission_change_logs.operator'), trace: 'UserName' },
      { key: 'occurredAt', label: t('auditing.common.occurred_at'), type: 'date' },
      { key: 'clientIp', label: t('auditing.common.client_ip'), trace: 'ClientIp' },
      { key: 'targetTenantId', label: t('auditing.permission_change_logs.target_tenant'), mono: true },
      { key: 'permissionCodes', label: t('auditing.permission_change_logs.permission_codes'), span: 'full' },
      { key: 'detail', label: t('auditing.permission_change_logs.detail'), span: 'full' },
      { key: 'roleId', label: t('auditing.permission_change_logs.role_id'), group: 'trace', mono: true },
      { key: 'targetUserId', label: t('auditing.permission_change_logs.target_user_id'), group: 'trace', mono: true, trace: 'UserId' },
      { key: 'editionId', label: t('auditing.permission_change_logs.edition_id'), group: 'trace', mono: true },
      ...traceFields(t),
    ],
  }
}

export function conditionHitDetailSchema(t: Translate): LogDetailSchema {
  return {
    header: {
      title: ['policyName', 'policyCode'],
      status: 'action',
      badges: ['hitCount'],
      meta: ['userName', 'occurredAt', 'clientIp'],
      timeKey: 'occurredAt',
    },
    fields: [
      { key: 'action', label: t('auditing.condition_hits.action'), options: conditionActionOptions(t), type: 'enum', tone: conditionActionTone },
      { key: 'policyName', label: t('auditing.condition_hits.policy_name') },
      { key: 'policyCode', label: t('auditing.condition_hits.policy_code'), mono: true },
      { key: 'hitCount', label: t('auditing.condition_hits.hit_count') },
      { key: 'userName', label: t('auditing.common.user_name'), trace: 'UserName' },
      { key: 'occurredAt', label: t('auditing.common.occurred_at'), type: 'date' },
      { key: 'clientIp', label: t('auditing.common.client_ip'), trace: 'ClientIp' },
      { key: 'isBlocked', label: t('auditing.condition_hits.is_blocked'), type: 'boolean', trueText: t('auditing.condition_hits.blocked_yes'), falseText: t('auditing.condition_hits.blocked_no') },
      { key: 'source', label: t('auditing.condition_hits.source'), options: conditionSourceOptions(t), type: 'enum' },
      { key: 'phase', label: t('auditing.condition_hits.phase'), options: conditionPhaseOptions(t), type: 'enum' },
      { key: 'operationId', label: t('auditing.common.operation_id'), group: 'request', mono: true },
      { key: 'method', label: t('auditing.common.method'), group: 'request' },
      { key: 'path', label: t('auditing.common.path'), group: 'request', mono: true, span: 'full' },
      { key: 'permissionCodes', label: t('auditing.condition_hits.permission_codes'), group: 'request', span: 'full' },
      { key: 'policyId', label: t('auditing.condition_hits.policy_id'), group: 'trace', mono: true },
      ...traceFields(t),
      { key: 'userAgent', label: t('auditing.common.user_agent'), type: 'code' },
    ],
  }
}

import type { LogDetailField, LogDetailOption, LogDetailSchema } from './log-detail.types.ts'
import {
  AccessResult,
  AuditOperationType,
  AuditRiskLevel,
  DeviceType,
  OperationExecuteResult,
  PermissionChangeType,
  SignatureType,
} from '@/api'
import {
  accessResultTone,
  booleanTone,
  loginResultTone,
  operationResultTone,
  operationTypeTone,
  permissionChangeTone,
  riskLevelTone,
  severityTone,
} from './log-tones'

/**
 * 各日志类型「详情」的单一事实源：抬头放什么、字段怎么分组、哪些值可复制或可追踪。
 *
 * 每个日志页与链路追踪详情抽屉共用同一套定义（标签/枚举选项/取值类型），
 * 避免链路追踪重新定义标签而漏国际化。工厂接收 i18n 的 t，在 computed 中调用即可随语言切换响应式更新。
 * 审计列（创建时间/创建人/创建人主键）与日志自身的发生时间、操作人重复，详情里不再列出。
 */
type Translate = (key: string) => string

/** 几种日志共有的追踪标识：可复制，链路追踪 ID 与会话标识点开即追踪 */
function traceFields(t: Translate, keys: { requestId?: boolean, sessionId?: boolean, userId?: boolean } = {}): LogDetailField[] {
  const { requestId = false, sessionId = true, userId = true } = keys
  return [
    { key: 'traceId', label: t('log.common.trace_id'), group: 'trace', mono: true, trace: 'TraceId' },
    ...(sessionId ? [{ key: 'sessionId', label: t('log.common.session_id'), group: 'trace', mono: true, trace: 'SessionId' } satisfies LogDetailField] : []),
    ...(requestId ? [{ key: 'requestId', label: t('log.common.request_id'), group: 'trace', mono: true } satisfies LogDetailField] : []),
    { key: 'basicId', label: t('log.common.basic_id'), group: 'trace', mono: true },
    ...(userId ? [{ key: 'userId', label: t('log.common.user_id'), group: 'trace', mono: true } satisfies LogDetailField] : []),
  ]
}

export function operationLogDetailSchema(t: Translate): LogDetailSchema {
  const resultOptions: LogDetailOption[] = [
    { label: t('log.operation.result_success'), value: OperationExecuteResult.Success },
    { label: t('log.operation.result_failed'), value: OperationExecuteResult.Failed },
    { label: t('log.operation.result_partial_success'), value: OperationExecuteResult.PartialSuccess },
  ]

  return {
    header: {
      title: ['operationType', 'title'],
      status: 'result',
      badges: ['executionTime'],
      meta: ['userName', 'operationTime', 'operationIp', 'operationLocation'],
      timeKey: 'operationTime',
    },
    fields: [
      // 操作类型直接取后端枚举元数据（前端曾只抄了其中 11 个）
      { key: 'operationType', label: t('log.operation.operation_type'), enumName: 'OperationType', type: 'enum', tone: operationTypeTone },
      { key: 'title', label: t('log.operation.title') },
      { key: 'result', label: t('log.operation.result'), options: resultOptions, type: 'enum', tone: operationResultTone },
      { key: 'executionTime', label: t('log.common.execution_time'), type: 'duration' },
      { key: 'userName', label: t('log.common.user_name'), trace: 'UserName' },
      { key: 'operationTime', label: t('log.operation.operation_time'), type: 'date' },
      { key: 'operationIp', label: t('log.operation.operation_ip'), trace: 'Ip' },
      { key: 'operationLocation', label: t('log.operation.operation_location') },
      { key: 'module', label: t('log.operation.module') },
      { key: 'function', label: t('log.operation.function') },
      { key: 'method', label: t('log.common.method'), group: 'request' },
      { key: 'requestUrl', label: t('log.operation.request_url'), group: 'request', mono: true, span: 'full' },
      // 描述里带着状态码与模仿者，没有别的列承载，保留
      { key: 'description', label: t('log.operation.description'), group: 'request', span: 'full' },
      { key: 'browser', label: t('log.common.browser'), group: 'client' },
      { key: 'os', label: t('log.common.os'), group: 'client' },
      ...traceFields(t),
      { key: 'errorMessage', label: t('log.common.error_message'), type: 'code' },
      { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
    ],
  }
}

export function accessLogDetailSchema(t: Translate): LogDetailSchema {
  const accessResultOptions: LogDetailOption[] = [
    { label: t('log.access.result_success'), value: AccessResult.Success },
    { label: t('log.access.result_failed'), value: AccessResult.Failed },
    { label: t('log.access.result_forbidden'), value: AccessResult.Forbidden },
    { label: t('log.access.result_unauthorized'), value: AccessResult.Unauthorized },
    { label: t('log.access.result_not_found'), value: AccessResult.NotFound },
    { label: t('log.access.result_server_error'), value: AccessResult.ServerError },
  ]

  return {
    header: {
      title: ['method', 'resourcePath'],
      status: 'accessResult',
      badges: ['statusCode', 'executionTime'],
      meta: ['userName', 'accessTime', 'accessIp', 'accessLocation'],
      timeKey: 'accessTime',
    },
    fields: [
      { key: 'method', label: t('log.common.method') },
      { key: 'resourcePath', label: t('log.access.resource_path') },
      { key: 'accessResult', label: t('log.access.access_result'), options: accessResultOptions, type: 'enum', tone: accessResultTone },
      { key: 'statusCode', label: t('log.common.status_code') },
      { key: 'executionTime', label: t('log.common.execution_time'), type: 'duration' },
      { key: 'userName', label: t('log.common.user_name'), trace: 'UserName' },
      { key: 'accessTime', label: t('log.access.access_time'), type: 'date' },
      { key: 'accessIp', label: t('log.access.access_ip'), trace: 'Ip' },
      { key: 'accessLocation', label: t('log.access.access_location') },
      { key: 'resourceName', label: t('log.access.resource_name') },
      { key: 'resourceType', label: t('log.access.resource_type') },
      { key: 'remark', label: t('common.fields.remark'), span: 'full' },
      { key: 'referer', label: t('log.common.referer'), group: 'request', mono: true, span: 'full' },
      { key: 'browser', label: t('log.common.browser'), group: 'client' },
      { key: 'os', label: t('log.common.os'), group: 'client' },
      { key: 'device', label: t('log.common.device'), group: 'client' },
      ...traceFields(t),
      { key: 'errorMessage', label: t('log.common.error_message'), type: 'code' },
      { key: 'extendData', label: t('log.common.extend_data'), type: 'code' },
      { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
    ],
  }
}

export function apiLogDetailSchema(t: Translate): LogDetailSchema {
  const signatureTypeOptions: LogDetailOption[] = [
    { label: t('log.api.signature_type_none'), value: SignatureType.None },
    { label: 'HMAC-SHA256', value: SignatureType.HmacSha256 },
    { label: 'HMAC-SHA512', value: SignatureType.HmacSha512 },
    { label: 'RSA-SHA256', value: SignatureType.RsaSha256 },
    { label: 'RSA-SHA512', value: SignatureType.RsaSha512 },
    { label: 'SM2', value: SignatureType.Sm2 },
    { label: 'SM3', value: SignatureType.Sm3 },
    { label: 'Ed25519', value: SignatureType.Ed25519 },
    { label: 'MD5', value: SignatureType.Md5 },
  ]

  return {
    header: {
      title: ['method', 'apiPath'],
      status: 'isSuccess',
      badges: ['statusCode', 'executionTime'],
      meta: ['clientId', 'userName', 'requestTime', 'requestIp', 'requestLocation'],
      timeKey: 'requestTime',
    },
    fields: [
      { key: 'method', label: t('log.common.method') },
      { key: 'apiPath', label: t('log.api.api_path') },
      { key: 'isSuccess', falseText: t('common.statuses.failed'), label: t('log.api.is_success'), trueText: t('common.statuses.success'), type: 'boolean', tone: booleanTone },
      { key: 'statusCode', label: t('log.common.status_code') },
      { key: 'executionTime', label: t('log.common.execution_time'), type: 'duration' },
      { key: 'clientId', label: t('log.api.client_id') },
      { key: 'userName', label: t('log.common.user_name'), trace: 'UserName' },
      { key: 'requestTime', label: t('log.api.request_time'), type: 'date' },
      { key: 'requestIp', label: t('log.api.request_ip'), trace: 'Ip' },
      { key: 'requestLocation', label: t('log.api.request_location') },
      { key: 'apiName', label: t('log.api.api_name') },
      { key: 'apiVersion', label: t('log.api.api_version') },
      { key: 'appId', label: t('log.api.app_id'), mono: true },
      { key: 'isSignatureValid', falseText: t('log.api.signature_invalid'), label: t('log.api.is_signature_valid'), trueText: t('log.api.signature_valid'), type: 'boolean' },
      { key: 'signatureType', label: t('log.api.signature_type'), options: signatureTypeOptions, type: 'enum' },
      { key: 'remark', label: t('common.fields.remark'), span: 'full' },
      { key: 'controllerName', label: t('log.common.controller_name'), group: 'request' },
      { key: 'actionName', label: t('log.common.action_name'), group: 'request' },
      { key: 'responseTime', label: t('log.api.response_time'), group: 'request', type: 'date' },
      { key: 'requestSize', label: t('log.api.request_size'), group: 'request', type: 'bytes' },
      { key: 'responseSize', label: t('log.api.response_size'), group: 'request', type: 'bytes' },
      { key: 'referer', label: t('log.common.referer'), group: 'request', mono: true, span: 'full' },
      { key: 'browser', label: t('log.common.browser'), group: 'client' },
      ...traceFields(t, { requestId: true }),
      { key: 'errorMessage', label: t('log.common.error_message'), type: 'code' },
      { key: 'exceptionStackTrace', label: t('log.common.exception_stack_trace'), type: 'code' },
      { key: 'requestParams', label: t('log.common.request_params'), type: 'code' },
      { key: 'requestBody', label: t('log.common.request_body'), type: 'code' },
      { key: 'responseBody', label: t('log.api.response_body'), type: 'code' },
      { key: 'requestHeaders', label: t('log.common.request_headers'), type: 'code' },
      { key: 'responseHeaders', label: t('log.common.response_headers'), type: 'code' },
      { key: 'extendData', label: t('log.common.extend_data'), type: 'code' },
      { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
    ],
  }
}

export function loginLogDetailSchema(t: Translate): LogDetailSchema {
  return {
    header: {
      title: ['userName'],
      status: 'loginResult',
      meta: ['loginTime', 'loginIp', 'loginLocation'],
      timeKey: 'loginTime',
    },
    fields: [
      { key: 'userName', label: t('log.common.user_name') },
      // 登录结果随认证审计事件增长（模仿登录等），标签直接取后端枚举元数据
      { key: 'loginResult', label: t('log.login.login_result'), enumName: 'LoginResult', type: 'enum', tone: loginResultTone },
      { key: 'loginTime', label: t('log.login.login_time'), type: 'date' },
      { key: 'loginIp', label: t('log.login.login_ip'), trace: 'Ip' },
      { key: 'loginLocation', label: t('log.login.login_location') },
      { key: 'isRiskLogin', falseText: t('common.statuses.no'), label: t('log.login.is_risk_login'), trueText: t('common.statuses.yes'), type: 'boolean' },
      { key: 'browser', label: t('log.common.browser'), group: 'client' },
      { key: 'os', label: t('log.common.os'), group: 'client' },
      { key: 'device', label: t('log.common.device'), group: 'client' },
      { key: 'deviceId', label: t('log.common.device_id'), group: 'client', mono: true },
      ...traceFields(t),
      { key: 'message', label: t('log.login.message'), type: 'code' },
      { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
    ],
  }
}

export function exceptionLogDetailSchema(t: Translate): LogDetailSchema {
  const severityOptions: LogDetailOption[] = [
    { label: t('log.exception.severity_low'), value: 1 },
    { label: t('log.exception.severity_medium'), value: 2 },
    { label: t('log.exception.severity_high'), value: 3 },
    { label: t('log.exception.severity_critical'), value: 4 },
    { label: t('log.exception.severity_fatal'), value: 5 },
  ]
  const deviceTypeOptions: LogDetailOption[] = [
    { label: t('log.exception.device_type_unknown'), value: DeviceType.Unknown },
    { label: t('log.exception.device_type_web'), value: DeviceType.Web },
    { label: 'iOS', value: DeviceType.iOS },
    { label: 'Android', value: DeviceType.Android },
    { label: 'Windows', value: DeviceType.Windows },
    { label: 'macOS', value: DeviceType.macOS },
    { label: 'Linux', value: DeviceType.Linux },
    { label: t('log.exception.device_type_tablet'), value: DeviceType.Tablet },
    { label: t('log.exception.device_type_mini_program'), value: DeviceType.MiniProgram },
    { label: 'API', value: DeviceType.Api },
  ]

  return {
    header: {
      title: ['exceptionType'],
      status: 'severityLevel',
      badges: ['statusCode'],
      meta: ['userName', 'exceptionTime', 'operationIp', 'operationLocation'],
      timeKey: 'exceptionTime',
    },
    fields: [
      { key: 'exceptionType', label: t('log.exception.exception_type') },
      { key: 'severityLevel', label: t('log.exception.severity_level'), options: severityOptions, type: 'enum', tone: severityTone },
      { key: 'statusCode', label: t('log.common.status_code') },
      { key: 'userName', label: t('log.common.user_name'), trace: 'UserName' },
      { key: 'exceptionTime', label: t('log.exception.exception_time'), type: 'date' },
      { key: 'operationIp', label: t('log.exception.operation_ip'), trace: 'Ip' },
      { key: 'operationLocation', label: t('log.exception.operation_location') },
      { key: 'errorCode', label: t('log.exception.error_code') },
      { key: 'exceptionSource', label: t('log.exception.exception_source') },
      { key: 'exceptionLocation', label: t('log.exception.exception_location'), mono: true, span: 'full' },
      { key: 'remark', label: t('common.fields.remark'), span: 'full' },
      { key: 'requestMethod', label: t('log.exception.request_method'), group: 'request' },
      { key: 'controllerName', label: t('log.common.controller_name'), group: 'request' },
      { key: 'actionName', label: t('log.common.action_name'), group: 'request' },
      { key: 'requestPath', label: t('log.exception.request_path'), group: 'request', mono: true, span: 'full' },
      { key: 'browser', label: t('log.common.browser'), group: 'client' },
      { key: 'os', label: t('log.common.os'), group: 'client' },
      { key: 'deviceType', label: t('log.exception.device_type'), group: 'client', options: deviceTypeOptions, type: 'enum' },
      { key: 'applicationName', label: t('log.exception.application_name'), group: 'runtime' },
      { key: 'applicationVersion', label: t('log.exception.application_version'), group: 'runtime' },
      { key: 'environmentName', label: t('log.exception.environment_name'), group: 'runtime' },
      { key: 'serverHostName', label: t('log.exception.server_host_name'), group: 'runtime' },
      { key: 'threadId', label: t('log.exception.thread_id'), group: 'runtime' },
      { key: 'processId', label: t('log.exception.process_id'), group: 'runtime' },
      { key: 'isHandled', falseText: t('log.exception.unhandled'), label: t('log.exception.is_handled'), trueText: t('log.exception.handled'), type: 'boolean', group: 'handling' },
      { key: 'handledBy', label: t('log.exception.handled_by'), group: 'handling' },
      { key: 'handledTime', label: t('log.exception.handled_time'), type: 'date', group: 'handling' },
      { key: 'handledRemark', label: t('log.exception.handled_remark'), group: 'handling', span: 'full' },
      ...traceFields(t, { requestId: true }),
      // 异常信息与堆栈是排查的主角，排在代码块最前
      { key: 'exceptionMessage', label: t('log.exception.exception_message'), type: 'code' },
      { key: 'exceptionStackTrace', label: t('log.common.exception_stack_trace'), type: 'code' },
      { key: 'requestParams', label: t('log.common.request_params'), type: 'code' },
      { key: 'requestBody', label: t('log.common.request_body'), type: 'code' },
      { key: 'requestHeaders', label: t('log.common.request_headers'), type: 'code' },
      { key: 'deviceInfo', label: t('log.exception.device_info'), type: 'code' },
      { key: 'extendData', label: t('log.common.extend_data'), type: 'code' },
      { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
    ],
  }
}

export function diffLogDetailSchema(t: Translate): LogDetailSchema {
  const operationTypeOptions: LogDetailOption[] = [
    { label: t('log.diff.type_create'), value: AuditOperationType.Create },
    { label: t('log.diff.type_update'), value: AuditOperationType.Update },
    { label: t('log.diff.type_delete'), value: AuditOperationType.Delete },
    { label: t('log.diff.type_restore'), value: AuditOperationType.Restore },
    { label: t('log.diff.type_import'), value: AuditOperationType.Import },
    { label: t('log.diff.type_export'), value: AuditOperationType.Export },
    { label: t('log.diff.type_query'), value: AuditOperationType.Query },
    { label: t('log.diff.type_other'), value: AuditOperationType.Other },
  ]
  const riskLevelOptions: LogDetailOption[] = [
    { label: t('log.diff.risk_low'), value: AuditRiskLevel.Low },
    { label: t('log.diff.risk_medium'), value: AuditRiskLevel.Medium },
    { label: t('log.diff.risk_high'), value: AuditRiskLevel.High },
    { label: t('log.diff.risk_very_high'), value: AuditRiskLevel.VeryHigh },
    { label: t('log.diff.risk_critical'), value: AuditRiskLevel.Critical },
  ]

  return {
    header: {
      title: ['operationType', 'entityName'],
      status: 'riskLevel',
      badges: ['executionTime'],
      meta: ['userName', 'auditTime', 'operationIp'],
      timeKey: 'auditTime',
    },
    fields: [
      { key: 'operationType', label: t('log.diff.operation_type'), options: operationTypeOptions, type: 'enum' },
      { key: 'entityName', label: t('log.diff.entity_name') },
      { key: 'riskLevel', label: t('log.diff.risk_level'), options: riskLevelOptions, type: 'enum', tone: riskLevelTone },
      { key: 'executionTime', label: t('log.common.execution_time'), type: 'duration' },
      { key: 'userName', label: t('log.common.user_name'), trace: 'UserName' },
      { key: 'auditTime', label: t('log.diff.audit_time'), type: 'date' },
      { key: 'operationIp', label: t('log.diff.operation_ip'), trace: 'Ip' },
      { key: 'auditType', label: t('log.diff.audit_type') },
      { key: 'entityType', label: t('log.diff.entity_type') },
      { key: 'tableName', label: t('log.diff.table_name') },
      { key: 'entityId', label: t('log.diff.entity_id'), mono: true },
      { key: 'primaryKey', label: t('log.diff.primary_key') },
      { key: 'primaryKeyValue', label: t('log.diff.primary_key_value'), mono: true },
      { key: 'changeDescription', label: t('log.diff.change_description'), span: 'full' },
      { key: 'description', label: t('log.diff.description'), span: 'full' },
      ...traceFields(t, { requestId: true }),
      { key: 'changedFields', label: t('log.diff.changed_fields'), type: 'code' },
      { key: 'beforeData', label: t('log.diff.before_data'), type: 'code' },
      { key: 'afterData', label: t('log.diff.after_data'), type: 'code' },
      { key: 'exceptionMessage', label: t('log.common.exception_message'), type: 'code' },
      { key: 'exceptionStackTrace', label: t('log.common.exception_stack_trace'), type: 'code' },
      { key: 'extendData', label: t('log.common.extend_data'), type: 'code' },
    ],
  }
}

export function permissionChangeLogDetailSchema(t: Translate): LogDetailSchema {
  const changeTypeOptions: LogDetailOption[] = [
    { label: t('log.permission_change.type_role_grant'), value: PermissionChangeType.RoleGrantPermission },
    { label: t('log.permission_change.type_role_revoke'), value: PermissionChangeType.RoleRevokePermission },
    { label: t('log.permission_change.type_user_grant'), value: PermissionChangeType.UserGrantPermission },
    { label: t('log.permission_change.type_user_revoke'), value: PermissionChangeType.UserRevokePermission },
    { label: t('log.permission_change.type_user_assign_role'), value: PermissionChangeType.UserAssignRole },
    { label: t('log.permission_change.type_user_remove_role'), value: PermissionChangeType.UserRemoveRole },
    { label: t('log.permission_change.type_user_deny'), value: PermissionChangeType.UserDenyPermission },
    { label: t('log.permission_change.type_role_deny'), value: PermissionChangeType.RoleDenyPermission },
    { label: t('log.permission_change.type_user_delegate_grant'), value: PermissionChangeType.UserDelegateGrant },
    { label: t('log.permission_change.type_user_delegate_revoke'), value: PermissionChangeType.UserDelegateRevoke },
    { label: t('log.permission_change.type_role_add_parent'), value: PermissionChangeType.RoleAddParent },
    { label: t('log.permission_change.type_role_remove_parent'), value: PermissionChangeType.RoleRemoveParent },
    { label: t('log.permission_change.type_condition_change'), value: PermissionChangeType.ConditionChange },
  ]

  return {
    header: {
      title: ['targetUserName', 'targetRoleName', 'permissionName'],
      status: 'changeType',
      meta: ['operatorUserName', 'changeTime', 'operationIp'],
      timeKey: 'changeTime',
    },
    fields: [
      { key: 'changeType', label: t('log.permission_change.change_type'), options: changeTypeOptions, type: 'enum', tone: permissionChangeTone },
      { key: 'targetUserName', label: t('log.permission_change.target_user_name') },
      { key: 'targetRoleName', label: t('log.permission_change.target_role_name') },
      { key: 'permissionName', label: t('log.permission_change.permission_name') },
      { key: 'operatorUserName', label: t('log.permission_change.operator_user_name'), trace: 'UserName' },
      { key: 'changeTime', label: t('log.permission_change.change_time'), type: 'date' },
      { key: 'operationIp', label: t('log.permission_change.operation_ip'), trace: 'Ip' },
      { key: 'changeReason', label: t('log.permission_change.change_reason'), span: 'full' },
      { key: 'description', label: t('log.permission_change.description'), span: 'full' },
      { key: 'traceId', label: t('log.common.trace_id'), group: 'trace', mono: true, trace: 'TraceId' },
      { key: 'basicId', label: t('log.common.basic_id'), group: 'trace', mono: true },
      { key: 'operatorUserId', label: t('log.permission_change.operator_user_id'), group: 'trace', mono: true },
      { key: 'targetUserId', label: t('log.permission_change.target_user_id'), group: 'trace', mono: true },
      { key: 'targetRoleId', label: t('log.permission_change.target_role_id'), group: 'trace', mono: true },
      { key: 'permissionId', label: t('log.permission_change.permission_id'), group: 'trace', mono: true },
    ],
  }
}

/**
 * 升级记录（数据库升级脚本执行台账）详情。
 */
export function migrationHistoryDetailSchema(t: Translate): LogDetailSchema {
  return {
    header: {
      title: ['version', 'scriptName'],
      status: 'success',
      meta: ['executedTime', 'nodeName'],
    },
    fields: [
      { key: 'version', label: t('log.migration.version') },
      { key: 'scriptName', label: t('log.migration.script_name') },
      { key: 'success', label: t('log.migration.success'), type: 'boolean', trueText: t('log.migration.success_yes'), falseText: t('log.migration.success_no'), tone: booleanTone },
      { key: 'executedTime', label: t('log.migration.executed_time'), type: 'date' },
      { key: 'nodeName', label: t('log.migration.node_name') },
      { key: 'errorMessage', label: t('log.common.error_message'), type: 'code' },
    ],
  }
}

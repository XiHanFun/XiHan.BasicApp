import type { LogDetailField, LogDetailOption } from './log-detail.types.ts'
import {
  AccessResult,
  AuditOperationType,
  AuditRiskLevel,
  DeviceType,
  LoginResult,
  OperationExecuteResult,
  OperationType,
  PermissionChangeType,
  SignatureType,
} from '@/api'

/**
 * 各日志类型「详情字段」的单一事实源。
 *
 * 每个日志页与链路追踪详情抽屉共用同一套字段定义（标签/枚举选项/取值类型），
 * 避免链路追踪重新定义标签而漏国际化。工厂接收 i18n 的 t，返回 LogDetailField[]；
 * 在 computed 中调用即可随语言切换响应式更新。
 */
type Translate = (key: string) => string

export function operationLogDetailFields(t: Translate): LogDetailField[] {
  const operationTypeOptions: LogDetailOption[] = [
    { label: t('log.operation.type_create'), value: OperationType.Create },
    { label: t('log.operation.type_update'), value: OperationType.Update },
    { label: t('log.operation.type_delete'), value: OperationType.Delete },
    { label: t('log.operation.type_review'), value: OperationType.Review },
    { label: t('log.operation.type_import'), value: OperationType.Import },
    { label: t('log.operation.type_export'), value: OperationType.Export },
    { label: t('log.operation.type_approve'), value: OperationType.Approve },
    { label: t('log.operation.type_start_task'), value: OperationType.StartTask },
    { label: t('log.operation.type_execute'), value: OperationType.Execute },
    { label: t('log.operation.type_restore'), value: OperationType.Restore },
    { label: t('log.operation.type_other'), value: OperationType.Other },
  ]
  const resultOptions: LogDetailOption[] = [
    { label: t('log.operation.result_success'), value: OperationExecuteResult.Success },
    { label: t('log.operation.result_failed'), value: OperationExecuteResult.Failed },
    { label: t('log.operation.result_partial_success'), value: OperationExecuteResult.PartialSuccess },
  ]

  return [
    { key: 'basicId', label: t('log.common.basic_id') },
    { key: 'sessionId', label: t('log.common.session_id') },
    { key: 'traceId', label: t('log.common.trace_id') },
    { key: 'userName', label: t('log.common.user_name') },
    { key: 'userId', label: t('log.common.user_id') },
    { key: 'operationType', label: t('log.operation.operation_type'), options: operationTypeOptions, type: 'enum' },
    { key: 'result', label: t('log.operation.result'), options: resultOptions, type: 'enum' },
    { key: 'module', label: t('log.operation.module') },
    { key: 'function', label: t('log.operation.function') },
    { key: 'title', label: t('log.operation.title'), span: 2 },
    { key: 'description', label: t('log.operation.description'), span: 2 },
    { key: 'method', label: t('log.common.method') },
    { key: 'requestUrl', label: t('log.operation.request_url'), span: 2 },
    { key: 'executionTime', label: t('log.common.execution_time'), type: 'duration' },
    { key: 'operationIp', label: t('log.operation.operation_ip') },
    { key: 'operationLocation', label: t('log.operation.operation_location') },
    { key: 'browser', label: t('log.common.browser') },
    { key: 'os', label: t('log.common.os') },
    { key: 'operationTime', label: t('log.operation.operation_time'), type: 'date' },
    { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
    { key: 'createdId', label: t('log.common.created_id') },
    { key: 'createdBy', label: t('common.fields.created_by') },
    { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
    { key: 'errorMessage', label: t('log.common.error_message'), type: 'code' },
  ]
}

export function accessLogDetailFields(t: Translate): LogDetailField[] {
  const accessResultOptions: LogDetailOption[] = [
    { label: t('log.access.result_success'), value: AccessResult.Success },
    { label: t('log.access.result_failed'), value: AccessResult.Failed },
    { label: t('log.access.result_forbidden'), value: AccessResult.Forbidden },
    { label: t('log.access.result_unauthorized'), value: AccessResult.Unauthorized },
    { label: t('log.access.result_not_found'), value: AccessResult.NotFound },
    { label: t('log.access.result_server_error'), value: AccessResult.ServerError },
  ]

  return [
    { key: 'basicId', label: t('log.common.basic_id') },
    { key: 'sessionId', label: t('log.common.session_id') },
    { key: 'traceId', label: t('log.common.trace_id') },
    { key: 'userName', label: t('log.common.user_name') },
    { key: 'userId', label: t('log.common.user_id') },
    { key: 'resourcePath', label: t('log.access.resource_path'), span: 2 },
    { key: 'resourceName', label: t('log.access.resource_name') },
    { key: 'resourceType', label: t('log.access.resource_type') },
    { key: 'method', label: t('log.common.method') },
    { key: 'statusCode', label: t('log.common.status_code') },
    { key: 'accessResult', label: t('log.access.access_result'), options: accessResultOptions, type: 'enum' },
    { key: 'executionTime', label: t('log.common.execution_time'), type: 'duration' },
    { key: 'accessIp', label: t('log.access.access_ip') },
    { key: 'accessLocation', label: t('log.access.access_location') },
    { key: 'browser', label: t('log.common.browser') },
    { key: 'os', label: t('log.common.os') },
    { key: 'device', label: t('log.common.device') },
    { key: 'referer', label: t('log.common.referer'), span: 2 },
    { key: 'accessTime', label: t('log.access.access_time'), type: 'date' },
    { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
    { key: 'createdId', label: t('log.common.created_id') },
    { key: 'createdBy', label: t('common.fields.created_by') },
    { key: 'remark', label: t('common.fields.remark'), span: 2 },
    { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
    { key: 'errorMessage', label: t('log.common.error_message'), type: 'code' },
    { key: 'extendData', label: t('log.common.extend_data'), type: 'code' },
  ]
}

export function apiLogDetailFields(t: Translate): LogDetailField[] {
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

  return [
    { key: 'basicId', label: t('log.common.basic_id') },
    { key: 'sessionId', label: t('log.common.session_id') },
    { key: 'requestId', label: t('log.common.request_id') },
    { key: 'traceId', label: t('log.common.trace_id') },
    { key: 'userName', label: t('log.common.user_name') },
    { key: 'userId', label: t('log.common.user_id') },
    { key: 'clientId', label: t('log.api.client_id') },
    { key: 'appId', label: t('log.api.app_id') },
    { key: 'apiPath', label: t('log.api.api_path'), span: 2 },
    { key: 'apiName', label: t('log.api.api_name') },
    { key: 'apiVersion', label: t('log.api.api_version') },
    { key: 'controllerName', label: t('log.common.controller_name') },
    { key: 'actionName', label: t('log.common.action_name') },
    { key: 'method', label: t('log.common.method') },
    { key: 'statusCode', label: t('log.common.status_code') },
    { key: 'isSuccess', falseText: t('common.statuses.failed'), label: t('log.api.is_success'), trueText: t('common.statuses.success'), type: 'boolean' },
    { key: 'isSignatureValid', falseText: t('log.api.signature_invalid'), label: t('log.api.is_signature_valid'), trueText: t('log.api.signature_valid'), type: 'boolean' },
    { key: 'signatureType', label: t('log.api.signature_type'), options: signatureTypeOptions, type: 'enum' },
    { key: 'executionTime', label: t('log.common.execution_time'), type: 'duration' },
    { key: 'requestSize', label: t('log.api.request_size'), type: 'bytes' },
    { key: 'responseSize', label: t('log.api.response_size'), type: 'bytes' },
    { key: 'requestIp', label: t('log.api.request_ip') },
    { key: 'requestLocation', label: t('log.api.request_location') },
    { key: 'browser', label: t('log.common.browser') },
    { key: 'referer', label: t('log.common.referer'), span: 2 },
    { key: 'requestTime', label: t('log.api.request_time'), type: 'date' },
    { key: 'responseTime', label: t('log.api.response_time'), type: 'date' },
    { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
    { key: 'createdId', label: t('log.common.created_id') },
    { key: 'createdBy', label: t('common.fields.created_by') },
    { key: 'remark', label: t('common.fields.remark'), span: 2 },
    { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
    { key: 'requestParams', label: t('log.common.request_params'), type: 'code' },
    { key: 'requestBody', label: t('log.common.request_body'), type: 'code' },
    { key: 'responseBody', label: t('log.api.response_body'), type: 'code' },
    { key: 'requestHeaders', label: t('log.common.request_headers'), type: 'code' },
    { key: 'responseHeaders', label: t('log.common.response_headers'), type: 'code' },
    { key: 'errorMessage', label: t('log.common.error_message'), type: 'code' },
    { key: 'exceptionStackTrace', label: t('log.common.exception_stack_trace'), type: 'code' },
    { key: 'extendData', label: t('log.common.extend_data'), type: 'code' },
  ]
}

export function loginLogDetailFields(t: Translate): LogDetailField[] {
  const loginResultOptions: LogDetailOption[] = [
    { label: t('log.login.result_success'), value: LoginResult.Success },
    { label: t('log.login.result_invalid_credentials'), value: LoginResult.InvalidCredentials },
    { label: t('log.login.result_account_locked'), value: LoginResult.AccountLocked },
    { label: t('log.login.result_account_disabled'), value: LoginResult.AccountDisabled },
    { label: t('log.login.result_requires_two_factor'), value: LoginResult.RequiresTwoFactor },
    { label: t('log.login.result_two_factor_failed'), value: LoginResult.TwoFactorFailed },
    { label: t('log.login.result_logout'), value: LoginResult.Logout },
    { label: t('log.login.result_token_refreshed'), value: LoginResult.TokenRefreshed },
    { label: t('log.login.result_password_changed'), value: LoginResult.PasswordChanged },
    { label: t('log.login.result_password_reset'), value: LoginResult.PasswordReset },
    { label: t('log.login.result_mfa_bound'), value: LoginResult.MfaBound },
    { label: t('log.login.result_mfa_unbound'), value: LoginResult.MfaUnbound },
    { label: t('log.login.result_tenant_switched'), value: LoginResult.TenantSwitched },
    { label: t('log.login.result_session_revoked'), value: LoginResult.SessionRevoked },
    { label: t('log.login.result_failed'), value: LoginResult.Failed },
  ]

  return [
    { key: 'basicId', label: t('log.common.basic_id') },
    { key: 'sessionId', label: t('log.common.session_id') },
    { key: 'traceId', label: t('log.common.trace_id') },
    { key: 'userName', label: t('log.common.user_name') },
    { key: 'userId', label: t('log.common.user_id') },
    { key: 'loginIp', label: t('log.login.login_ip') },
    { key: 'loginLocation', label: t('log.login.login_location') },
    { key: 'browser', label: t('log.common.browser') },
    { key: 'os', label: t('log.common.os') },
    { key: 'device', label: t('log.common.device') },
    { key: 'deviceId', label: t('log.common.device_id') },
    { key: 'loginResult', label: t('log.login.login_result'), options: loginResultOptions, type: 'enum' },
    { key: 'isRiskLogin', falseText: t('common.statuses.no'), label: t('log.login.is_risk_login'), trueText: t('common.statuses.yes'), type: 'boolean' },
    { key: 'loginTime', label: t('log.login.login_time'), type: 'date' },
    { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
    { key: 'createdId', label: t('log.common.created_id') },
    { key: 'createdBy', label: t('common.fields.created_by') },
    { key: 'message', label: t('log.login.message'), type: 'code' },
    { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
  ]
}

export function exceptionLogDetailFields(t: Translate): LogDetailField[] {
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

  return [
    { key: 'basicId', label: t('log.common.basic_id') },
    { key: 'sessionId', label: t('log.common.session_id') },
    { key: 'requestId', label: t('log.common.request_id') },
    { key: 'traceId', label: t('log.common.trace_id') },
    { key: 'userName', label: t('log.common.user_name') },
    { key: 'userId', label: t('log.common.user_id') },
    { key: 'exceptionType', label: t('log.exception.exception_type') },
    { key: 'errorCode', label: t('log.exception.error_code') },
    { key: 'exceptionSource', label: t('log.exception.exception_source') },
    { key: 'exceptionLocation', label: t('log.exception.exception_location'), span: 2 },
    { key: 'severityLevel', label: t('log.exception.severity_level'), options: severityOptions, type: 'enum' },
    { key: 'statusCode', label: t('log.common.status_code') },
    { key: 'requestPath', label: t('log.exception.request_path'), span: 2 },
    { key: 'requestMethod', label: t('log.exception.request_method') },
    { key: 'controllerName', label: t('log.common.controller_name') },
    { key: 'actionName', label: t('log.common.action_name') },
    { key: 'operationIp', label: t('log.exception.operation_ip') },
    { key: 'operationLocation', label: t('log.exception.operation_location') },
    { key: 'browser', label: t('log.common.browser') },
    { key: 'os', label: t('log.common.os') },
    { key: 'deviceType', label: t('log.exception.device_type'), options: deviceTypeOptions, type: 'enum' },
    { key: 'applicationName', label: t('log.exception.application_name') },
    { key: 'applicationVersion', label: t('log.exception.application_version') },
    { key: 'environmentName', label: t('log.exception.environment_name') },
    { key: 'serverHostName', label: t('log.exception.server_host_name') },
    { key: 'threadId', label: t('log.exception.thread_id') },
    { key: 'processId', label: t('log.exception.process_id') },
    { key: 'isHandled', falseText: t('log.exception.unhandled'), label: t('log.exception.is_handled'), trueText: t('log.exception.handled'), type: 'boolean' },
    { key: 'handledBy', label: t('log.exception.handled_by') },
    { key: 'handledTime', label: t('log.exception.handled_time'), type: 'date' },
    { key: 'exceptionTime', label: t('log.exception.exception_time'), type: 'date' },
    { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
    { key: 'createdId', label: t('log.common.created_id') },
    { key: 'createdBy', label: t('common.fields.created_by') },
    { key: 'handledRemark', label: t('log.exception.handled_remark'), span: 2 },
    { key: 'remark', label: t('common.fields.remark'), span: 2 },
    { key: 'exceptionMessage', label: t('log.exception.exception_message'), type: 'code' },
    { key: 'exceptionStackTrace', label: t('log.common.exception_stack_trace'), type: 'code' },
    { key: 'requestParams', label: t('log.common.request_params'), type: 'code' },
    { key: 'requestBody', label: t('log.common.request_body'), type: 'code' },
    { key: 'requestHeaders', label: t('log.common.request_headers'), type: 'code' },
    { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
    { key: 'deviceInfo', label: t('log.exception.device_info'), type: 'code' },
    { key: 'extendData', label: t('log.common.extend_data'), type: 'code' },
  ]
}

export function diffLogDetailFields(t: Translate): LogDetailField[] {
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

  return [
    { key: 'basicId', label: t('log.common.basic_id') },
    { key: 'requestId', label: t('log.common.request_id') },
    { key: 'sessionId', label: t('log.common.session_id') },
    { key: 'traceId', label: t('log.common.trace_id') },
    { key: 'userName', label: t('log.common.user_name') },
    { key: 'userId', label: t('log.common.user_id') },
    { key: 'auditType', label: t('log.diff.audit_type') },
    { key: 'operationType', label: t('log.diff.operation_type'), options: operationTypeOptions, type: 'enum' },
    { key: 'riskLevel', label: t('log.diff.risk_level'), options: riskLevelOptions, type: 'enum' },
    { key: 'entityType', label: t('log.diff.entity_type') },
    { key: 'entityName', label: t('log.diff.entity_name') },
    { key: 'tableName', label: t('log.diff.table_name') },
    { key: 'entityId', label: t('log.diff.entity_id') },
    { key: 'primaryKey', label: t('log.diff.primary_key') },
    { key: 'primaryKeyValue', label: t('log.diff.primary_key_value') },
    { key: 'changeDescription', label: t('log.diff.change_description'), span: 2 },
    { key: 'description', label: t('log.diff.description'), span: 2 },
    { key: 'executionTime', label: t('log.common.execution_time'), type: 'duration' },
    { key: 'operationIp', label: t('log.diff.operation_ip') },
    { key: 'auditTime', label: t('log.diff.audit_time'), type: 'date' },
    { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
    { key: 'changedFields', label: t('log.diff.changed_fields'), type: 'code' },
    { key: 'beforeData', label: t('log.diff.before_data'), type: 'code' },
    { key: 'afterData', label: t('log.diff.after_data'), type: 'code' },
    { key: 'extendData', label: t('log.common.extend_data'), type: 'code' },
    { key: 'exceptionMessage', label: t('log.common.exception_message'), type: 'code' },
    { key: 'exceptionStackTrace', label: t('log.common.exception_stack_trace'), type: 'code' },
  ]
}

export function permissionChangeLogDetailFields(t: Translate): LogDetailField[] {
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
  ]

  return [
    { key: 'basicId', label: t('log.common.basic_id') },
    { key: 'traceId', label: t('log.common.trace_id') },
    { key: 'changeType', label: t('log.permission_change.change_type'), options: changeTypeOptions, type: 'enum' },
    { key: 'operatorUserName', label: t('log.permission_change.operator_user_name') },
    { key: 'operatorUserId', label: t('log.permission_change.operator_user_id') },
    { key: 'targetUserName', label: t('log.permission_change.target_user_name') },
    { key: 'targetUserId', label: t('log.permission_change.target_user_id') },
    { key: 'targetRoleName', label: t('log.permission_change.target_role_name') },
    { key: 'targetRoleId', label: t('log.permission_change.target_role_id') },
    { key: 'permissionName', label: t('log.permission_change.permission_name') },
    { key: 'permissionId', label: t('log.permission_change.permission_id') },
    { key: 'operationIp', label: t('log.permission_change.operation_ip') },
    { key: 'changeReason', label: t('log.permission_change.change_reason'), span: 2 },
    { key: 'description', label: t('log.permission_change.description'), span: 2 },
    { key: 'changeTime', label: t('log.permission_change.change_time'), type: 'date' },
    { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
    { key: 'createdId', label: t('log.common.created_id') },
    { key: 'createdBy', label: t('common.fields.created_by') },
  ]
}

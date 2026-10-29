/**
 * 日志枚举的文案与选项（列表、搜索、详情、链路共用）。6.0 没有枚举元数据接口，取值是契约字面量，
 * 文案键按字面量逐个静态写出，供 i18n 门禁核对。
 */
import type { LogDetailOption } from './log-detail.types'
import type {
  TraceItemStatus,
} from '~/contracts'
import {
  AccessResult,
  ConditionHitAction,
  ConditionHitPhase,
  ConditionHitSource,
  DiffKind,
  LoginEvent,
  PermissionChangeType,
  TraceDimension,
  TraceLogType,
} from '~/contracts'

export type Translate = (key: string, named?: Record<string, unknown>) => string

export function accessResultLabel(t: Translate, value: AccessResult): string {
  switch (value) {
    case 'Success': return t('auditing.access_logs.results.success')
    case 'Failed': return t('auditing.access_logs.results.failed')
    case 'Forbidden': return t('auditing.access_logs.results.forbidden')
    case 'Unauthorized': return t('auditing.access_logs.results.unauthorized')
    case 'NotFound': return t('auditing.access_logs.results.not_found')
    case 'ServerError': return t('auditing.access_logs.results.server_error')
  }
}

export function loginEventLabel(t: Translate, value: LoginEvent): string {
  switch (value) {
    case 'Login': return t('auditing.login_logs.events.login')
    case 'Refresh': return t('auditing.login_logs.events.refresh')
    case 'Logout': return t('auditing.login_logs.events.logout')
    case 'SwitchTenant': return t('auditing.login_logs.events.switch_tenant')
    case 'ImpersonationStart': return t('auditing.login_logs.events.impersonation_start')
    case 'ImpersonationStop': return t('auditing.login_logs.events.impersonation_stop')
    case 'ChangePassword': return t('auditing.login_logs.events.change_password')
    case 'TwoFactorChallenge': return t('auditing.login_logs.events.two_factor_challenge')
    case 'TwoFactorEnable': return t('auditing.login_logs.events.two_factor_enable')
    case 'TwoFactorDisable': return t('auditing.login_logs.events.two_factor_disable')
    case 'TwoFactorReset': return t('auditing.login_logs.events.two_factor_reset')
    case 'RoleActivation': return t('auditing.login_logs.events.role_activation')
  }
}

export function diffKindLabel(t: Translate, value: DiffKind): string {
  switch (value) {
    case 'Insert': return t('auditing.diff_logs.kinds.insert')
    case 'Update': return t('auditing.diff_logs.kinds.update')
    case 'Delete': return t('auditing.diff_logs.kinds.delete')
  }
}

export function permissionChangeLabel(t: Translate, value: PermissionChangeType): string {
  switch (value) {
    case 'RoleGrant': return t('auditing.permission_change_logs.types.role_grant')
    case 'RoleRevoke': return t('auditing.permission_change_logs.types.role_revoke')
    case 'RoleAssign': return t('auditing.permission_change_logs.types.role_assign')
    case 'RoleUnassign': return t('auditing.permission_change_logs.types.role_unassign')
    case 'RoleDataScope': return t('auditing.permission_change_logs.types.role_data_scope')
    case 'RoleDelete': return t('auditing.permission_change_logs.types.role_delete')
    case 'EditionPermissions': return t('auditing.permission_change_logs.types.edition_permissions')
    case 'TenantEdition': return t('auditing.permission_change_logs.types.tenant_edition')
    case 'RoleDeny': return t('auditing.permission_change_logs.types.role_deny')
    case 'RoleAssignmentWindow': return t('auditing.permission_change_logs.types.role_assignment_window')
    case 'UserGrant': return t('auditing.permission_change_logs.types.user_grant')
    case 'UserDeny': return t('auditing.permission_change_logs.types.user_deny')
    case 'UserRevoke': return t('auditing.permission_change_logs.types.user_revoke')
    case 'UserDataScope': return t('auditing.permission_change_logs.types.user_data_scope')
    case 'FieldSecurity': return t('auditing.permission_change_logs.types.field_security')
    case 'RoleInheritance': return t('auditing.permission_change_logs.types.role_inheritance')
    case 'Delegation': return t('auditing.permission_change_logs.types.delegation')
    case 'ConditionChange': return t('auditing.permission_change_logs.types.condition_change')
    case 'ConstraintViolation': return t('auditing.permission_change_logs.types.constraint_violation')
  }
}

export function conditionSourceLabel(t: Translate, value: ConditionHitSource): string {
  switch (value) {
    case 'AccessPolicy': return t('auditing.condition_hits.sources.access_policy')
    case 'PermissionCondition': return t('auditing.condition_hits.sources.permission_condition')
  }
}

export function conditionActionLabel(t: Translate, value: ConditionHitAction): string {
  switch (value) {
    case 'LogOnly': return t('auditing.condition_hits.actions.log_only')
    case 'Deny': return t('auditing.condition_hits.actions.deny')
    case 'Allow': return t('auditing.condition_hits.actions.allow')
  }
}

export function conditionPhaseLabel(t: Translate, value: ConditionHitPhase): string {
  switch (value) {
    case 'BeforeAuthentication': return t('auditing.condition_hits.phases.before_authentication')
    case 'AfterAuthentication': return t('auditing.condition_hits.phases.after_authentication')
    case 'Login': return t('auditing.condition_hits.phases.login')
    case 'Authorization': return t('auditing.condition_hits.phases.authorization')
  }
}

export function traceDimensionLabel(t: Translate, value: TraceDimension): string {
  switch (value) {
    case 'TraceId': return t('auditing.traces.dimensions.trace_id')
    case 'UserId': return t('auditing.traces.dimensions.user_id')
    case 'UserName': return t('auditing.traces.dimensions.user_name')
    case 'SessionId': return t('auditing.traces.dimensions.session_id')
    case 'ClientIp': return t('auditing.traces.dimensions.client_ip')
  }
}

export function traceLogTypeLabel(t: Translate, value: TraceLogType): string {
  switch (value) {
    case 'Access': return t('auditing.traces.log_types.access')
    case 'OpenApi': return t('auditing.traces.log_types.open_api')
    case 'Operation': return t('auditing.traces.log_types.operation')
    case 'Login': return t('auditing.traces.log_types.login')
    case 'Exception': return t('auditing.traces.log_types.exception')
    case 'Diff': return t('auditing.traces.log_types.diff')
    case 'PermissionChange': return t('auditing.traces.log_types.permission_change')
    case 'ConditionHit': return t('auditing.traces.log_types.condition_hit')
  }
}

export function traceStatusLabel(t: Translate, value: TraceItemStatus): string {
  switch (value) {
    case 'Success': return t('auditing.traces.statuses.success')
    case 'Info': return t('auditing.traces.statuses.info')
    case 'Warning': return t('auditing.traces.statuses.warning')
    case 'Failure': return t('auditing.traces.statuses.failure')
  }
}

function optionsOf<T extends string>(values: readonly T[], label: (value: T) => string): LogDetailOption[] {
  return values.map(value => ({ label: label(value), value }))
}

export const accessResultOptions = (t: Translate) => optionsOf(AccessResult, value => accessResultLabel(t, value))
export const loginEventOptions = (t: Translate) => optionsOf(LoginEvent, value => loginEventLabel(t, value))
export const diffKindOptions = (t: Translate) => optionsOf(DiffKind, value => diffKindLabel(t, value))
export const permissionChangeOptions = (t: Translate) => optionsOf(PermissionChangeType, value => permissionChangeLabel(t, value))
export const conditionSourceOptions = (t: Translate) => optionsOf(ConditionHitSource, value => conditionSourceLabel(t, value))
export const conditionActionOptions = (t: Translate) => optionsOf(ConditionHitAction, value => conditionActionLabel(t, value))
export const conditionPhaseOptions = (t: Translate) => optionsOf(ConditionHitPhase, value => conditionPhaseLabel(t, value))
export const traceDimensionOptions = (t: Translate) => optionsOf(TraceDimension, value => traceDimensionLabel(t, value))
export const traceLogTypeOptions = (t: Translate) => optionsOf(TraceLogType, value => traceLogTypeLabel(t, value))

/** 是 / 否两项（布尔筛选的值是后端接受的 'true' / 'false'；文案由调用方翻好传入） */
export function yesNoOptions(yes: string, no: string): LogDetailOption[] {
  return [{ label: yes, value: 'true' }, { label: no, value: 'false' }]
}

/** HTTP 方法的搜索选项（原文即文案） */
export const HTTP_METHOD_OPTIONS: LogDetailOption[] = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'].map(method => ({ label: method, value: method }))

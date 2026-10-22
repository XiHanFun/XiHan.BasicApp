import type { ApiId, DateTimeString } from '../../types'
import type { ValidityStatus } from '../shared'
import type { ConditionOperator, ConfigDataType } from './resource.types'
import type { PermissionAction } from './role-permission.types'

/** 条件属性的值种类；与后端 JsonStringEnumConverter 序列化值一致 */
export enum ConditionValueKind {
  String = 'String',
  Number = 'Number',
  Boolean = 'Boolean',
  DateTime = 'DateTime',
  Date = 'Date',
  TimeOfDay = 'TimeOfDay',
  IpAddress = 'IpAddress',
}

/** 条件的求值结果 */
export enum ConditionOutcome {
  Matched = 'Matched',
  NotMatched = 'NotMatched',
  Indeterminate = 'Indeterminate',
  Pending = 'Pending',
}

/** 条件盘点的诊断 */
export enum PermissionConditionDiagnostic {
  UnregisteredAttribute = 'UnregisteredAttribute',
  LegacyName = 'LegacyName',
  InvalidCondition = 'InvalidCondition',
  ValueTypeMismatch = 'ValueTypeMismatch',
  BindingInactive = 'BindingInactive',
  AmbiguousBinding = 'AmbiguousBinding',
}

/** 条件组在生效后的结论 */
export enum PermissionConditionGroupConclusion {
  Evaluable = 'Evaluable',
  NeverMatches = 'NeverMatches',
  BindingInactive = 'BindingInactive',
  Disabled = 'Disabled',
}

/** 条件所在授权的类别 */
export enum PermissionConditionBindingKind {
  Role = 'Role',
  User = 'User',
  Ambiguous = 'Ambiguous',
}

/** 试算结论 */
export enum PermissionConditionTrialVerdict {
  NotGranted = 'NotGranted',
  Unconditional = 'Unconditional',
  Granted = 'Granted',
  Denied = 'Denied',
  Pending = 'Pending',
}

export interface PermissionConditionAttributeDto {
  aliases: string[]
  displayName?: string | null
  isFamily: boolean
  isLegacy: boolean
  isMultiValued: boolean
  isRowAttribute: boolean
  kind: ConditionValueKind
  name: string
  operators: ConditionOperator[]
}

export interface PermissionConditionInventoryItemDto {
  attributeName: string
  bindingKind: PermissionConditionBindingKind
  conditionGroup: number
  conditionId: ApiId
  conditionValue: string
  diagnostics: PermissionConditionDiagnostic[]
  groupConclusion: PermissionConditionGroupConclusion
  isNegated: boolean
  message?: string | null
  operator: ConditionOperator
  permissionAction?: PermissionAction | null
  permissionCode?: string | null
  permissionName?: string | null
  roleCode?: string | null
  roleName?: string | null
  rolePermissionId?: ApiId | null
  status: ValidityStatus
  tenantCode?: string | null
  tenantId: ApiId
  userId?: ApiId | null
  userName?: string | null
  userPermissionId?: ApiId | null
  valueType: ConfigDataType
}

export interface PermissionConditionInventoryDto {
  bindingCount: number
  conditionCount: number
  diagnostics: Partial<Record<PermissionConditionDiagnostic, number>>
  generatedTime: DateTimeString
  groupConclusions: Partial<Record<PermissionConditionGroupConclusion, number>>
  items: PermissionConditionInventoryItemDto[]
}

/** 试算的模拟请求上下文；不填的项取当前请求（IP、User-Agent）或当前时刻 */
export interface PermissionConditionTrialContextDto {
  ip?: string | null
  isImpersonating?: boolean
  method?: string | null
  path?: string | null
  queryValues?: Record<string, string> | null
  routeValues?: Record<string, string> | null
  time?: DateTimeString | null
  userAgent?: string | null
}

export interface PermissionConditionTrialInputDto extends PermissionConditionTrialContextDto {
  permissionCode: string
  resourceId?: ApiId | null
  tenantId?: ApiId | null
  userId: ApiId
}

export interface PermissionConditionBatchTrialInputDto extends PermissionConditionTrialContextDto {
  rolePermissionId?: ApiId | null
  userPermissionId?: ApiId | null
}

export interface PermissionConditionTrialClauseDto {
  attributeName: string
  attributeValues: string[]
  conditionId?: string | null
  isNegated: boolean
  message?: string | null
  operator: ConditionOperator
  outcome: ConditionOutcome
  value?: string | null
}

export interface PermissionConditionTrialGroupDto {
  clauses: PermissionConditionTrialClauseDto[]
  group?: string | null
  outcome: ConditionOutcome
}

export interface PermissionConditionTrialSetDto {
  groups: PermissionConditionTrialGroupDto[]
  key: string
  outcome?: ConditionOutcome | null
}

export interface PermissionConditionTrialPathDto {
  denySets: PermissionConditionTrialSetDto[]
  grantSets: PermissionConditionTrialSetDto[]
  hasUnconditionalGrant: boolean
  outcome: ConditionOutcome
  source: string
  sourceLabel?: string | null
}

export interface PermissionConditionTrialResultDto {
  denies: PermissionConditionTrialSetDto[]
  ip?: string | null
  paths: PermissionConditionTrialPathDto[]
  permissionCode: string
  tenantId: ApiId
  time: DateTimeString
  timeZone: string
  userId: ApiId
  userName?: string | null
  verdict: PermissionConditionTrialVerdict
}

export interface PermissionConditionBatchTrialItemDto {
  userId: ApiId
  userName?: string | null
  verdict: PermissionConditionTrialVerdict
}

export interface PermissionConditionBatchTrialResultDto {
  items: PermissionConditionBatchTrialItemDto[]
  permissionCode: string
  totalUsers: number
  truncated: boolean
}

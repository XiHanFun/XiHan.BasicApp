import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'
import type { ConditionOutcome, ConditionValueKind, PermissionConditionTrialGroupDto } from './permission-condition-insight.types'
import type { ConditionOperator, ConfigDataType } from './resource.types'

/** 访问策略动作，与后端 JsonStringEnumConverter 序列化值一致 */
export enum AccessPolicyAction {
  LogOnly = 'LogOnly',
  Deny = 'Deny',
  Allow = 'Allow',
}

/** 条件命中的求值时机 */
export enum AccessPolicyPhase {
  BeforeAuthentication = 'BeforeAuthentication',
  AfterAuthentication = 'AfterAuthentication',
  Login = 'Login',
  Authorization = 'Authorization',
}

/** 命中记录的来源 */
export enum AccessPolicyHitSource {
  AccessPolicy = 'AccessPolicy',
  PermissionCondition = 'PermissionCondition',
}

/** 访问策略的一条条件：组内且、组间或 */
export interface AccessPolicyConditionDto {
  attributeName: string
  conditionGroup: number
  conditionValue: string
  description?: string | null
  isNegated: boolean
  operator: ConditionOperator
  valueType: ConfigDataType
}

export interface AccessPolicyListItemDto extends BasicDto {
  action: AccessPolicyAction
  conditionCount: number
  createdTime: DateTimeString
  description?: string | null
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  isActive: boolean
  modifiedTime?: DateTimeString | null
  phase: AccessPolicyPhase
  policyCode: string
  policyName: string
  priority: number
  recentHitCount: number
  status: EnableStatus
  templateCode?: string | null
  tenantId: ApiId
}

export interface AccessPolicyDetailDto extends AccessPolicyListItemDto {
  conditions: AccessPolicyConditionDto[]
  remark?: string | null
}

export interface AccessPolicyPageQueryDto extends PageRequest {
  action?: AccessPolicyAction | null
  keyword?: string | null
  status?: EnableStatus | null
}

/** 新建一律只记录，启用拦截另走 updateAction */
export interface AccessPolicyCreateDto {
  conditions: AccessPolicyConditionDto[]
  description?: string | null
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  policyCode: string
  policyName: string
  priority: number
  remark?: string | null
  status: EnableStatus
  templateCode?: string | null
}

export interface AccessPolicyUpdateDto extends BasicUpdateDto {
  conditions: AccessPolicyConditionDto[]
  description?: string | null
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  policyName: string
  priority: number
  remark?: string | null
}

export interface AccessPolicyActionUpdateDto extends BasicUpdateDto {
  action: AccessPolicyAction
}

export interface AccessPolicyStatusUpdateDto extends BasicUpdateDto {
  status: EnableStatus
}

/** 条件编辑器用的属性 */
export interface AccessPolicyAttributeDto {
  aliases: string[]
  displayName?: string | null
  isBeforeAuthentication: boolean
  isFamily: boolean
  isLegacy: boolean
  isMultiValued: boolean
  kind: ConditionValueKind
  name: string
  operators: ConditionOperator[]
}

/** 预置模板 */
export interface AccessPolicyTemplateDto {
  code: string
  conditions: AccessPolicyConditionDto[]
  description: string
  name: string
  priority: number
  suggestedAction: AccessPolicyAction
}

/** 试运行的模拟上下文；不填的项取当前请求 */
export interface AccessPolicyTrialContextDto {
  ip?: string | null
  method?: string | null
  path?: string | null
  permissionCodes?: string[] | null
  time?: DateTimeString | null
  userAgent?: string | null
  userId?: ApiId | null
}

export interface AccessPolicyTrialInputDto {
  action?: AccessPolicyAction | null
  conditions?: AccessPolicyConditionDto[] | null
  context: AccessPolicyTrialContextDto
  policyId?: ApiId | null
}

export interface AccessPolicyTrialResultDto {
  action: AccessPolicyAction
  errors: string[]
  groups: PermissionConditionTrialGroupDto[]
  ip?: string | null
  location?: string | null
  outcome?: ConditionOutcome | null
  phase: AccessPolicyPhase
  time: DateTimeString
  timeZone: string
  userAgent?: string | null
  userId?: ApiId | null
  wouldBlock: boolean
}

/** 一条自动封禁 */
export interface LoginBanDto {
  ip: string
  remainingSeconds: number
  until: DateTimeString
}

export interface LoginUnbanDto {
  ip: string
}

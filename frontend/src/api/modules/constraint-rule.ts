import type { ApiId, BasicDto, BasicUpdateDto, DateTimeString, PageRequest, PageResult } from '../types'
import type { EnableStatus } from './shared'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../base'

export enum ConstraintTargetType {
  Role = 0,
  Permission = 1,
  User = 2,
}

export enum ConstraintType {
  Ssd = 0,
  Dsd = 1,
  MutualExclusion = 2,
  Cardinality = 3,
  Prerequisite = 4,
  Temporal = 5,
  Location = 6,
  Custom = 99,
}

export enum ViolationAction {
  Deny = 0,
  Warning = 1,
  Log = 2,
  RequireApproval = 3,
}

export interface ConstraintRulePageQueryDto extends PageRequest {
  constraintType?: ConstraintType | null
  isGlobal?: boolean | null
  keyword?: string | null
  status?: EnableStatus | null
  targetType?: ConstraintTargetType | null
  violationAction?: ViolationAction | null
}

export interface ConstraintRuleListItemDto extends BasicDto {
  constraintType: ConstraintType
  createdTime: DateTimeString
  description?: string | null
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  isActive: boolean
  isGlobal: boolean
  itemCount: number
  modifiedTime?: DateTimeString | null
  priority: number
  ruleCode: string
  ruleName: string
  status: EnableStatus
  targetType: ConstraintTargetType
  violationAction: ViolationAction
}

export interface ConstraintRuleItemDto extends BasicDto {
  constraintGroup: number
  createdTime: DateTimeString
  remark?: string | null
  targetCode?: string | null
  targetId: ApiId
  targetName?: string | null
  targetType: ConstraintTargetType
}

export interface ConstraintRuleItemInputDto {
  constraintGroup: number
  remark?: string | null
  targetId: ApiId
  targetType: ConstraintTargetType
}

export interface ConstraintRuleDetailDto extends Omit<ConstraintRuleListItemDto, 'itemCount'> {
  createdBy?: string | null
  createdId?: ApiId | null
  items: ConstraintRuleItemDto[]
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  parameters?: string | null
  remark?: string | null
}

export interface ConstraintRuleCreateDto {
  constraintType: ConstraintType
  description?: string | null
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  items: ConstraintRuleItemInputDto[]
  parameters?: string | null
  priority: number
  remark?: string | null
  ruleCode: string
  ruleName: string
  status: EnableStatus
  targetType: ConstraintTargetType
  violationAction: ViolationAction
}

export interface ConstraintRuleUpdateDto extends BasicUpdateDto {
  constraintType: ConstraintType
  description?: string | null
  effectiveTime?: DateTimeString | null
  expirationTime?: DateTimeString | null
  items: ConstraintRuleItemInputDto[]
  parameters?: string | null
  priority: number
  remark?: string | null
  ruleName: string
  targetType: ConstraintTargetType
  violationAction: ViolationAction
}

export interface ConstraintRuleStatusUpdateDto extends BasicUpdateDto {
  remark?: string | null
  status: EnableStatus
}

const constraintRuleQueryApi = createDynamicApiClient('ConstraintRuleQuery')
const constraintRuleCommandApi = createDynamicApiClient('ConstraintRule')
const constraintRuleReadApi = createReadApi<
  ConstraintRuleListItemDto,
  ConstraintRuleDetailDto,
  ConstraintRulePageQueryDto
>('ConstraintRuleQuery', 'ConstraintRule')

export const constraintRuleApi = {
  create(input: ConstraintRuleCreateDto) {
    return constraintRuleCommandApi.post<ConstraintRuleDetailDto, ConstraintRuleCreateDto>(
      'ConstraintRule',
      input,
    )
  },
  delete(id: ApiId) {
    return constraintRuleCommandApi.delete(`ConstraintRule/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return constraintRuleReadApi.detail(id)
  },
  page(input: ConstraintRulePageQueryDto) {
    return constraintRuleQueryApi.get<PageResult<ConstraintRuleListItemDto>>(
      'ConstraintRulePage',
      toConstraintRulePageParams(input),
    )
  },
  update(input: ConstraintRuleUpdateDto) {
    return constraintRuleCommandApi.put<ConstraintRuleDetailDto, ConstraintRuleUpdateDto>(
      'ConstraintRule',
      input,
    )
  },
  updateStatus(input: ConstraintRuleStatusUpdateDto) {
    return constraintRuleCommandApi.put<ConstraintRuleDetailDto, ConstraintRuleStatusUpdateDto>(
      'ConstraintRuleStatus',
      input,
    )
  },
}

function toConstraintRulePageParams(input: ConstraintRulePageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'ConstraintType', input.constraintType)
  appendDynamicApiParam(params, 'IsGlobal', input.isGlobal)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'Status', input.status)
  appendDynamicApiParam(params, 'TargetType', input.targetType)
  appendDynamicApiParam(params, 'ViolationAction', input.violationAction)

  return params
}

import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { buildPageRequest, normalizePageResult, toId, toNumber, unwrapPayload } from '../helpers'

const api = useBaseApi('ConstraintRule')

// -------- 类型 --------

export interface SysConstraintRule {
  basicId: string
  tenantId?: string
  ruleCode: string
  ruleName: string
  constraintType: number
  targetType: number
  parameters: string
  isGlobal?: boolean
  isEnabled: boolean
  violationAction: number
  description?: string
  priority: number
  effectiveFrom?: string
  effectiveTo?: string
  status: number
  createTime?: string
  remark?: string
}

export interface ConstraintRulePageQuery extends PageQuery {
  status?: number
  constraintType?: number
  isEnabled?: boolean
}

// -------- 内部 --------

const STATUS_MAP: Record<string, number> = {
  No: 0,
  Yes: 1,
}

function resolveBool(value: unknown, fallback: boolean): boolean {
  if (value === undefined || value === null)
    return fallback
  if (typeof value === 'boolean')
    return value
  if (typeof value === 'number')
    return value !== 0
  if (typeof value === 'string') {
    const normalized = value.trim().toLowerCase()
    if (normalized === 'true' || normalized === '1' || normalized === 'yes')
      return true
    if (normalized === 'false' || normalized === '0' || normalized === 'no')
      return false
  }
  return fallback
}

function resolveEnum(value: unknown, map: Record<string, number>, fallback: number): number {
  if (value === undefined || value === null)
    return fallback
  if (typeof value === 'number')
    return value
  if (typeof value === 'string')
    return map[value] ?? toNumber(value, fallback)
  return fallback
}

function normalizeConstraintRule(raw: Record<string, any>): SysConstraintRule {
  return {
    basicId: toId(raw.basicId ?? raw.BasicId),
    tenantId: toId(raw.tenantId ?? raw.TenantId) || undefined,
    ruleCode: raw.ruleCode ?? raw.RuleCode ?? '',
    ruleName: raw.ruleName ?? raw.RuleName ?? '',
    constraintType: toNumber(raw.constraintType ?? raw.ConstraintType, 0),
    targetType: toNumber(raw.targetType ?? raw.TargetType, 0),
    parameters: raw.parameters ?? raw.Parameters ?? '{}',
    isGlobal: resolveBool(raw.isGlobal ?? raw.IsGlobal, false),
    isEnabled: resolveBool(raw.isEnabled ?? raw.IsEnabled, true),
    violationAction: toNumber(raw.violationAction ?? raw.ViolationAction, 0),
    description: raw.description ?? raw.Description ?? undefined,
    priority: toNumber(raw.priority ?? raw.Priority, 0),
    effectiveFrom: raw.effectiveFrom ?? raw.EffectiveFrom ?? undefined,
    effectiveTo: raw.effectiveTo ?? raw.EffectiveTo ?? undefined,
    status: resolveEnum(raw.status ?? raw.Status, STATUS_MAP, 1),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? raw.CreatedTime ?? undefined,
    remark: raw.remark ?? raw.Remark ?? undefined,
  }
}

function toCreatePayload(data: Partial<SysConstraintRule>) {
  return {
    tenantId: data.tenantId ? toId(data.tenantId) : null,
    ruleCode: (data.ruleCode ?? '').trim(),
    ruleName: (data.ruleName ?? '').trim(),
    constraintType: toNumber(data.constraintType, 0),
    targetType: toNumber(data.targetType, 0),
    parameters: (data.parameters ?? '{}').trim(),
    isGlobal: data.isGlobal ?? false,
    isEnabled: data.isEnabled !== false,
    violationAction: toNumber(data.violationAction, 0),
    description: (data.description ?? '').trim(),
    priority: toNumber(data.priority, 0),
    effectiveFrom: data.effectiveFrom ?? null,
    effectiveTo: data.effectiveTo ?? null,
    remark: (data.remark ?? '').trim(),
  }
}

function toUpdatePayload(id: string, data: Partial<SysConstraintRule>) {
  return {
    ...toCreatePayload(data),
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

const PAGE_OPTIONS = {
  keywordFields: ['RuleCode', 'RuleName', 'TargetType', 'Description'],
  filterFieldMap: { status: 'Status', constraintType: 'ConstraintType', isEnabled: 'IsEnabled' },
}

async function queryConstraintRulePage(params: Record<string, any>) {
  const data = await api.request.post<any>(
    `${api.baseUrl}Page`,
    buildPageRequest(params, PAGE_OPTIONS),
  )
  return normalizePageResult(data, normalizeConstraintRule)
}

// -------- API --------

export const constraintRuleApi = {
  page: (params: Record<string, any>) => queryConstraintRulePage(params),

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(raw => normalizeConstraintRule((unwrapPayload<any>(raw) ?? {}) as Record<string, any>)),

  create: (data: Partial<SysConstraintRule>) =>
    api.request.post(`${api.baseUrl}Create`, toCreatePayload(data)),

  update: (id: string, data: Partial<SysConstraintRule>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.deletePath(id),

  getByCode: (ruleCode: string, tenantId?: number) =>
    api.request.get(`${api.baseUrl}RuleByCode/${tenantId ?? 0}`, { params: { ruleCode } }),
}

export function getConstraintRulePageApi(params: ConstraintRulePageQuery) {
  return queryConstraintRulePage(params as Record<string, any>)
}

export const getConstraintRuleDetailApi = constraintRuleApi.detail
export const createConstraintRuleApi = constraintRuleApi.create
export const updateConstraintRuleApi = constraintRuleApi.update
export const deleteConstraintRuleApi = constraintRuleApi.delete
export const getConstraintRuleByCodeApi = constraintRuleApi.getByCode

import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { RequestOf, ResponseOf } from '~/contracts'
import { constraintRules as constraintRulesApi, toPageRequest, toSchemaPageResult } from '~/contracts'

export type ConstraintRuleRow = ResponseOf<'constraint-rules.list'>['items'][number]
export type ConstraintRuleItem = ConstraintRuleRow['items'][number]
export type ConstraintType = ConstraintRuleRow['type']
export type ConstraintTargetType = ConstraintRuleRow['targetType']
export type ConstraintViolationAction = ConstraintRuleRow['violationAction']
export type ConstraintRuleStatus = ConstraintRuleRow['status']
export type ConstraintItemInput = RequestOf<'constraint-rules.create'>['items'][number]

type Translate = (key: string, values?: Record<string, unknown>) => string

/** 色调，与 XiHan.UI 标签一致 */
export type ConstraintTone = 'danger' | 'info' | 'neutral' | 'success' | 'warning'

export const CONSTRAINT_TYPES: readonly ConstraintType[] = ['Ssd', 'Dsd', 'MutualExclusion', 'Cardinality', 'Condition']
export const CONSTRAINT_TARGET_TYPES: readonly ConstraintTargetType[] = ['Role', 'Permission', 'User']
export const CONSTRAINT_ACTIONS: readonly ConstraintViolationAction[] = ['Deny', 'Warning', 'Log', 'RequireApproval']
export const CONSTRAINT_STATUSES: readonly ConstraintRuleStatus[] = ['Active', 'Disabled']

/** 约束类型用得了哪些目标类型（与后端一致）：职责分离与条件只约束角色，互斥只约束权限，基数约束角色或用户 */
export function targetTypesOf(type: ConstraintType): readonly ConstraintTargetType[] {
  switch (type) {
    case 'MutualExclusion': return ['Permission']
    case 'Cardinality': return ['Role', 'User']
    default: return ['Role']
  }
}

/** 这类约束至少几个规则项（职责分离与互斥按组判定，至少两个） */
export function minItemsOf(type: ConstraintType): number {
  return type === 'Ssd' || type === 'Dsd' || type === 'MutualExclusion' ? 2 : 1
}

/** 用不用数量上限（条件类不用） */
export function usesMaxAllowed(type: ConstraintType): boolean {
  return type !== 'Condition'
}

/** 编辑器里的一个规则项 */
export interface ConstraintItemModel {
  key: number
  targetId: string
  groupNo: number
  remark: string
}

let nextKey = 1

export function emptyItem(groupNo = 0): ConstraintItemModel {
  return { key: nextKey++, targetId: '', groupNo, remark: '' }
}

export function toItemModels(items: readonly ConstraintRuleItem[]): ConstraintItemModel[] {
  return items.map(item => ({ key: nextKey++, targetId: item.targetId, groupNo: item.groupNo, remark: item.remark ?? '' }))
}

export function toItemInputs(items: readonly ConstraintItemModel[]): ConstraintItemInput[] {
  return items.map(item => ({ targetId: item.targetId, groupNo: item.groupNo, remark: item.remark.trim() || null }))
}

/** 规则项的第一处问题（4.x 的提交前校验）；都好为空 */
export type ItemProblem = 'duplicate' | 'no_target' | 'too_few'

export function findItemProblem(type: ConstraintType, items: readonly ConstraintItemModel[]): { problem: ItemProblem, index: number } | null {
  if (items.length < minItemsOf(type))
    return { problem: 'too_few', index: minItemsOf(type) }
  const seen = new Set<string>()
  for (const [index, item] of items.entries()) {
    if (!item.targetId)
      return { problem: 'no_target', index: index + 1 }
    if (seen.has(item.targetId))
      return { problem: 'duplicate', index: index + 1 }
    seen.add(item.targetId)
  }
  return null
}

/** 处理方式的色调 */
export function actionTone(action: ConstraintViolationAction): ConstraintTone {
  switch (action) {
    case 'Deny':
    case 'RequireApproval':
      return 'danger'
    case 'Warning': return 'warning'
    default: return 'neutral'
  }
}

/** 批量删除的结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

/**
 * SchemaPage 的资源：分页（作用侧 = 当前租户或平台，缺省按求值顺序）、删除、启停。
 * 搜索按后端白名单：关键字匹配编码、名称、说明与备注；类型、目标类型、处理方式、状态多选（合成 in）。
 */
export function constraintRuleResource(): SchemaResource<ConstraintRuleRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await constraintRulesApi.list(toPageRequest(params, { multiples: ['type', 'targetType', 'violationAction', 'status'] })),
      params,
    ),
    remove: async id => failedCodes(await constraintRulesApi.delete({ ids: [String(id)] })),
    updateStatus: async (id, enabled) => constraintRulesApi.setStatus({ id: String(id), status: enabled ? 'Active' : 'Disabled' }),
  }
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对

export function typeLabel(t: Translate, type: ConstraintType): string {
  switch (type) {
    case 'Ssd': return t('access.constraint_rules.types.ssd')
    case 'Dsd': return t('access.constraint_rules.types.dsd')
    case 'MutualExclusion': return t('access.constraint_rules.types.mutual_exclusion')
    case 'Cardinality': return t('access.constraint_rules.types.cardinality')
    case 'Condition': return t('access.constraint_rules.types.condition')
  }
}

export function typeHint(t: Translate, type: ConstraintType): string {
  switch (type) {
    case 'Ssd': return t('access.constraint_rules.hints.ssd')
    case 'Dsd': return t('access.constraint_rules.hints.dsd')
    case 'MutualExclusion': return t('access.constraint_rules.hints.mutual_exclusion')
    case 'Cardinality': return t('access.constraint_rules.hints.cardinality')
    case 'Condition': return t('access.constraint_rules.hints.condition')
  }
}

export function targetTypeLabel(t: Translate, targetType: ConstraintTargetType): string {
  switch (targetType) {
    case 'Permission': return t('access.constraint_rules.target_types.permission')
    case 'User': return t('access.constraint_rules.target_types.user')
    default: return t('access.constraint_rules.target_types.role')
  }
}

export function actionLabel(t: Translate, action: ConstraintViolationAction): string {
  switch (action) {
    case 'Warning': return t('access.constraint_rules.actions_kind.warning')
    case 'Log': return t('access.constraint_rules.actions_kind.log')
    case 'RequireApproval': return t('access.constraint_rules.actions_kind.require_approval')
    default: return t('access.constraint_rules.actions_kind.deny')
  }
}

export function statusLabel(t: Translate, status: ConstraintRuleStatus): string {
  return status === 'Disabled' ? t('access.constraint_rules.statuses.disabled') : t('access.constraint_rules.statuses.active')
}

export function itemProblemMessage(t: Translate, found: { problem: ItemProblem, index: number }): string {
  switch (found.problem) {
    case 'too_few': return t('access.constraint_rules.warn_items_too_few', { min: found.index })
    case 'no_target': return t('access.constraint_rules.warn_item_no_target', { index: found.index })
    case 'duplicate': return t('access.constraint_rules.warn_item_duplicate', { index: found.index })
  }
}

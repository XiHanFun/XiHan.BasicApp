import type { ResponseOf } from '~/contracts'

export type ConditionTrialResult = ResponseOf<'permission-conditions.trial'>
export type ConditionTrialPath = ConditionTrialResult['paths'][number]
export type ConditionTrialSet = ConditionTrialResult['denies'][number]
export type ConditionVerdict = ConditionTrialResult['verdict']
export type ConditionOutcome = ConditionTrialSet['groups'][number]['outcome']
export type ConditionInventory = ResponseOf<'permission-conditions.inventory'>
export type ConditionInventoryItem = ConditionInventory['items'][number]
export type ConditionDiagnostic = ConditionInventoryItem['diagnostics'][number]
export type ConditionGroupConclusion = ConditionInventoryItem['groupConclusion']
export type ConditionBatchTrial = ResponseOf<'permission-conditions.batch-trial'>

type Translate = (key: string, values?: Record<string, unknown>) => string

/** 色调，与 XiHan.UI 标签一致 */
export type ConditionTone = 'danger' | 'info' | 'neutral' | 'success' | 'warning'

/** 试算结果里的一块：用户本身有条件的拒绝，或一条授予路径 */
export interface TrialBlock {
  key: string
  source: 'denies' | ConditionTrialPath['source']
  roleName: null | string
  /** 授予路径是否成立；拒绝块没有 */
  holds: boolean | null
  hasUnconditionalGrant: boolean
  sets: ConditionTrialSet[]
}

/** 试算结果排成块：先列用户本身有条件的拒绝（有才列），再逐条授予路径 */
export function trialBlocks(result: ConditionTrialResult): TrialBlock[] {
  const blocks: TrialBlock[] = []
  if (result.denies.length > 0)
    blocks.push({ key: 'denies', source: 'denies', roleName: null, holds: null, hasUnconditionalGrant: false, sets: [...result.denies] })
  result.paths.forEach((path, index) => blocks.push({
    key: `${path.source}-${path.roleId ?? index}`,
    source: path.source,
    roleName: path.roleName ?? null,
    holds: path.holds,
    hasUnconditionalGrant: path.hasUnconditionalGrant,
    sets: [...path.grantSets, ...path.denySets],
  }))
  return blocks
}

/** 求值结果的色调 */
export function outcomeTone(outcome: ConditionOutcome | null | undefined): ConditionTone {
  switch (outcome) {
    case 'Matched': return 'success'
    case 'NotMatched': return 'danger'
    case 'Pending': return 'info'
    default: return 'warning'
  }
}

/** 试算结论的色调 */
export function verdictTone(verdict: ConditionVerdict): ConditionTone {
  switch (verdict) {
    case 'Granted':
    case 'Unconditional':
      return 'success'
    case 'Denied': return 'danger'
    default: return 'neutral'
  }
}

/** 条件组结论的色调 */
export function conclusionTone(conclusion: ConditionGroupConclusion): ConditionTone {
  switch (conclusion) {
    case 'Evaluable': return 'success'
    case 'NeverMatches': return 'danger'
    case 'BindingInactive': return 'warning'
    default: return 'neutral'
  }
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对

export function outcomeLabel(t: Translate, outcome: ConditionOutcome): string {
  switch (outcome) {
    case 'Matched': return t('access.conditions.outcome.matched')
    case 'NotMatched': return t('access.conditions.outcome.not_matched')
    case 'Pending': return t('access.conditions.outcome.pending')
    default: return t('access.conditions.outcome.indeterminate')
  }
}

export function verdictLabel(t: Translate, verdict: ConditionVerdict): string {
  switch (verdict) {
    case 'Unconditional': return t('access.conditions.verdict.unconditional')
    case 'Granted': return t('access.conditions.verdict.granted')
    case 'Denied': return t('access.conditions.verdict.denied')
    default: return t('access.conditions.verdict.not_granted')
  }
}

export function pathLabel(t: Translate, source: TrialBlock['source'], roleName: null | string): string {
  switch (source) {
    case 'denies': return t('access.conditions.path_user_denies')
    case 'user': return t('access.conditions.path_user')
    case 'delegation': return t('access.conditions.path_delegation')
    case 'delegation-role': return t('access.conditions.path_delegation_role', { name: roleName ?? '-' })
    default: return t('access.conditions.path_role', { name: roleName ?? '-' })
  }
}

export function diagnosticLabel(t: Translate, diagnostic: ConditionDiagnostic): string {
  switch (diagnostic) {
    case 'UnregisteredAttribute': return t('access.conditions.diagnostics.unregistered_attribute')
    case 'LegacyName': return t('access.conditions.diagnostics.legacy_name')
    case 'InvalidCondition': return t('access.conditions.diagnostics.invalid_condition')
    default: return t('access.conditions.diagnostics.binding_inactive')
  }
}

export function conclusionLabel(t: Translate, conclusion: ConditionGroupConclusion): string {
  switch (conclusion) {
    case 'Evaluable': return t('access.conditions.conclusion.evaluable')
    case 'NeverMatches': return t('access.conditions.conclusion.never_matches')
    case 'BindingInactive': return t('access.conditions.conclusion.binding_inactive')
    default: return t('access.conditions.conclusion.disabled')
  }
}

/** 盘点明细导出成 CSV：表头与诊断、结论都用调用方给的文案；带 BOM，表格软件按 UTF-8 打开中文不乱码 */
export function inventoryToCsv(items: readonly ConditionInventoryItem[], headers: readonly string[], describe: (item: ConditionInventoryItem) => readonly string[]): string {
  const escape = (value: string) => (/[",\r\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value)
  const lines = [headers, ...items.map(describe)].map(row => row.map(cell => escape(cell ?? '')).join(','))
  return `\uFEFF${lines.join('\r\n')}`
}

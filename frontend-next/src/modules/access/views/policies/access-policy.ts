import type { ConditionAttribute, ConditionOperator } from '../roles/permission-condition'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { RequestOf, ResponseOf } from '~/contracts'
import { policies as policiesApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { composeAttributeName, requiresValue, resolveAttribute } from '../roles/permission-condition'

export type AccessPolicyRow = ResponseOf<'policies.list'>['items'][number]
export type AccessPolicyTemplate = ResponseOf<'policies.templates'>['items'][number]
export type AccessPolicyTrialResult = ResponseOf<'policies.trial'>
export type AccessPolicyAction = AccessPolicyRow['action']
export type AccessPolicyStatus = AccessPolicyRow['status']
export type AccessPolicyPhase = AccessPolicyRow['phase']
export type AccessPolicyConditionInput = RequestOf<'policies.create'>['conditions'][number]

type Translate = (key: string, values?: Record<string, unknown>) => string

/** 色调，与 XiHan.UI 标签一致 */
export type AccessPolicyTone = 'danger' | 'info' | 'neutral' | 'success' | 'warning'

export const ACCESS_POLICY_ACTIONS: readonly AccessPolicyAction[] = ['LogOnly', 'Deny', 'Allow']
export const ACCESS_POLICY_STATUSES: readonly AccessPolicyStatus[] = ['Active', 'Disabled']
/** 访问策略只有认证前与认证后两种时机（登录时机随后一步加入） */
export const ACCESS_POLICY_PHASES: readonly AccessPolicyPhase[] = ['BeforeAuthentication', 'AfterAuthentication']

/** 模板里占位的办公网段（文档保留网段），启用拦截前要换成真实网段 */
export const OFFICE_NETWORK_PLACEHOLDER = '203.0.113.0/24'

/** 已保存或模板里的一条条件的最小形状 */
export interface PolicyConditionLike {
  groupNo: number
  attributeName: string
  operator: ConditionOperator
  isNegated: boolean
  value?: null | string
}

/** 编辑器里的一条条件：属性名拆成目录项与一族属性的后缀，取反用开关 */
export interface ConditionRowModel {
  /** 列表键 */
  key: number
  attribute: string
  familySuffix: string
  operator: ConditionOperator
  isNegated: boolean
  value: string
}

/** 编辑器里的一组条件：组内全部满足这组才满足 */
export interface ConditionGroupModel {
  key: number
  rows: ConditionRowModel[]
}

let nextKey = 1

/** 新的列表键 */
export function newKey() {
  return nextKey++
}

/** 空白条件 */
export function emptyRow(): ConditionRowModel {
  return { key: newKey(), attribute: '', familySuffix: '', operator: 'Equals', isNegated: false, value: '' }
}

/** 已保存的条件按组号转成编辑器的组（组内保持写入顺序） */
export function toGroups(conditions: readonly PolicyConditionLike[], catalog: readonly ConditionAttribute[]): ConditionGroupModel[] {
  const groups = new Map<number, ConditionRowModel[]>()
  for (const condition of conditions) {
    const resolved = resolveAttribute(catalog, condition.attributeName)
    const rows = groups.get(condition.groupNo) ?? []
    rows.push({
      key: newKey(),
      attribute: resolved.attribute?.name ?? condition.attributeName,
      familySuffix: resolved.familySuffix ?? '',
      operator: condition.operator,
      isNegated: condition.isNegated,
      value: condition.value ?? '',
    })
    groups.set(condition.groupNo, rows)
  }
  return [...groups.entries()].sort(([left], [right]) => left - right).map(([, rows]) => ({ key: newKey(), rows }))
}

/** 编辑器的组转回条件：空组去掉，组号按顺序重排；判空类运算符不带值 */
export function toConditions(groups: readonly ConditionGroupModel[], catalog: readonly ConditionAttribute[]): AccessPolicyConditionInput[] {
  return groups
    .filter(group => group.rows.length > 0)
    .flatMap((group, index) => group.rows.map((row) => {
      const attribute = catalog.find(item => item.name === row.attribute)
      return {
        groupNo: index,
        attributeName: attribute ? composeAttributeName(attribute, row.familySuffix) : row.attribute,
        operator: row.operator,
        isNegated: row.isNegated,
        value: requiresValue(row.operator) ? row.value.trim() : null,
        description: null,
      }
    }))
}

/** 编辑器里第一处没填完的地方；都填好时为空 */
export type IncompleteReason = 'attribute_required' | 'conditions_required' | 'family_suffix_required' | 'value_required'

export function findIncomplete(groups: readonly ConditionGroupModel[], catalog: readonly ConditionAttribute[]): { reason: IncompleteReason, index: number } | null {
  const rows = groups.flatMap(group => group.rows)
  if (rows.length === 0)
    return { reason: 'conditions_required', index: 0 }
  for (const [index, row] of rows.entries()) {
    const attribute = catalog.find(item => item.name === row.attribute)
    if (!attribute)
      return { reason: 'attribute_required', index: index + 1 }
    if (attribute.isFamily && !row.familySuffix.trim())
      return { reason: 'family_suffix_required', index: index + 1 }
    if (requiresValue(row.operator) && !row.value.trim())
      return { reason: 'value_required', index: index + 1 }
  }
  return null
}

/** 时机：平台策略只用到认证前取得到的属性即认证前，否则认证后；租户策略一律认证后（与后端一致） */
export function phaseOf(groups: readonly ConditionGroupModel[], catalog: readonly ConditionAttribute[], platform: boolean): AccessPolicyPhase {
  if (!platform)
    return 'AfterAuthentication'
  const attributes = groups.flatMap(group => group.rows).map(row => catalog.find(item => item.name === row.attribute))
  return attributes.every(attribute => attribute?.isBeforeAuthentication ?? true) ? 'BeforeAuthentication' : 'AfterAuthentication'
}

/** 条件里是否还留着占位的办公网段 */
export function usesPlaceholderNetwork(conditions: readonly { value?: null | string }[]): boolean {
  return conditions.some(condition => condition.value?.includes(OFFICE_NETWORK_PLACEHOLDER) ?? false)
}

/** 从模板新建时的编码：去掉 preset. 前缀，免得与平台种子写入的策略同名 */
export function codeFromTemplate(code: string): string {
  return code.replace(/^preset\./, '')
}

/** 动作的色调 */
export function actionTone(action: AccessPolicyAction): AccessPolicyTone {
  switch (action) {
    case 'Deny': return 'danger'
    case 'Allow': return 'success'
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
 * 搜索按后端白名单：关键字匹配编码、名称与说明；动作、状态多选（合成 in）。
 */
export function accessPolicyResource(): SchemaResource<AccessPolicyRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await policiesApi.list(toPageRequest(params, { multiples: ['action', 'status'] })),
      params,
    ),
    remove: async id => failedCodes(await policiesApi.delete({ ids: [String(id)] })),
    updateStatus: async (id, enabled) => policiesApi.setStatus({ id: String(id), status: enabled ? 'Active' : 'Disabled' }),
  }
}

// 枚举值与模板编码是契约字面量，文案键静态写出以便 i18n 门禁核对

export function actionLabel(t: Translate, action: AccessPolicyAction): string {
  switch (action) {
    case 'Deny': return t('access.policies.actions_kind.deny')
    case 'Allow': return t('access.policies.actions_kind.allow')
    default: return t('access.policies.actions_kind.log_only')
  }
}

export function statusLabel(t: Translate, status: AccessPolicyStatus): string {
  return status === 'Disabled' ? t('access.policies.statuses.disabled') : t('access.policies.statuses.active')
}

export function phaseLabel(t: Translate, phase: AccessPolicyPhase): string {
  switch (phase) {
    case 'BeforeAuthentication': return t('access.policies.phases.before_authentication')
    case 'Login': return t('access.policies.phases.login')
    case 'Authorization': return t('access.policies.phases.authorization')
    default: return t('access.policies.phases.after_authentication')
  }
}

export function incompleteMessage(t: Translate, incomplete: { reason: IncompleteReason, index: number }): string {
  switch (incomplete.reason) {
    case 'conditions_required': return t('access.policies.editor.warn_conditions_required')
    case 'attribute_required': return t('access.policies.editor.warn_attribute_required', { index: incomplete.index })
    case 'family_suffix_required': return t('access.policies.editor.warn_family_suffix_required', { index: incomplete.index })
    case 'value_required': return t('access.policies.editor.warn_value_required', { index: incomplete.index })
  }
}

/** 模板的名称与说明按界面语言显示；认不出的模板（后端新增）用后端给的原文 */
export function templateText(t: Translate, template: Pick<AccessPolicyTemplate, 'code' | 'description' | 'name'>): { name: string, description: string } {
  switch (template.code) {
    case 'preset.ip-blacklist': return { name: t('access.policies.templates.ip_blacklist.name'), description: t('access.policies.templates.ip_blacklist.description') }
    case 'preset.admin-office-network': return { name: t('access.policies.templates.admin_office_network.name'), description: t('access.policies.templates.admin_office_network.description') }
    case 'preset.sensitive-ops-network': return { name: t('access.policies.templates.sensitive_ops_network.name'), description: t('access.policies.templates.sensitive_ops_network.description') }
    case 'preset.off-hours': return { name: t('access.policies.templates.off_hours.name'), description: t('access.policies.templates.off_hours.description') }
    case 'preset.overseas': return { name: t('access.policies.templates.overseas.name'), description: t('access.policies.templates.overseas.description') }
    case 'preset.block-scripts': return { name: t('access.policies.templates.block_scripts.name'), description: t('access.policies.templates.block_scripts.description') }
    default: return { name: template.name, description: template.description }
  }
}

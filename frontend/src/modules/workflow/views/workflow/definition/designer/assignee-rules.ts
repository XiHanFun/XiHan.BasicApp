/**
 * 人工任务受理人规则（AssigneeRules / CcRules / FallbackAssigneeRules）的前端模型。
 *
 * 规则 `{ type, values }` 原样写进节点属性，后端按类型交给已注册的解析器：
 * 框架内置 User / Starter / Expression；BasicApp 注册角色、部门、部门负责人、岗位、发起人主管（与后端
 * OrganizationAssigneeRuleTypes 一一对应）。角色、部门、岗位按编码引用，用户按主键引用。
 */

export type AssigneeValueEditor = 'users' | 'roles' | 'departments' | 'positions' | 'level' | 'expression' | 'none'

export interface AssigneeRule {
  type: string
  values: string[]
}

export interface AssigneeRuleTypeMeta {
  /** 规则类型（后端规则类型名） */
  type: string
  /** i18n 键（workflow.designer.rule 下） */
  labelKey: string
  /** 参数编辑器 */
  editor: AssigneeValueEditor
}

export const ASSIGNEE_RULE_TYPES: AssigneeRuleTypeMeta[] = [
  { type: 'User', labelKey: 'user', editor: 'users' },
  { type: 'Role', labelKey: 'role', editor: 'roles' },
  { type: 'Department', labelKey: 'department', editor: 'departments' },
  { type: 'DepartmentLeader', labelKey: 'department_leader', editor: 'departments' },
  { type: 'Position', labelKey: 'position', editor: 'positions' },
  { type: 'Starter', labelKey: 'starter', editor: 'none' },
  { type: 'StarterLeader', labelKey: 'starter_leader', editor: 'level' },
  { type: 'StarterLeaderChain', labelKey: 'starter_leader_chain', editor: 'level' },
  { type: 'Expression', labelKey: 'expression', editor: 'expression' },
]

export const ASSIGNEE_RULE_TYPE_MAP: Record<string, AssigneeRuleTypeMeta> = Object.fromEntries(
  ASSIGNEE_RULE_TYPES.map(meta => [meta.type, meta]),
)

/** 规则属性键（节点属性里存放规则列表的键） */
export const RULE_PROPERTY_KEYS = ['AssigneeRules', 'CcRules', 'FallbackAssigneeRules'] as const

/** 主管层级上限（与后端 StarterLeaderAssigneeProviderBase.MaxLevel 一致） */
export const MAX_LEADER_LEVEL = 10

/** 新规则的默认参数：主管类缺省 1 级，其余为空 */
export function createRule(type: string): AssigneeRule {
  const editor = ASSIGNEE_RULE_TYPE_MAP[type]?.editor
  return { type, values: editor === 'level' ? ['1'] : [] }
}

/**
 * 把节点属性里的规则值归一成 `{ type, values }[]`。
 * 后端经 JSON 往返后是驼峰（type/values），手写 JSON 也可能是帕斯卡（Type/Values），两种都认；无法识别的项丢弃。
 */
export function readRules(value: unknown): AssigneeRule[] {
  if (!Array.isArray(value))
    return []
  const rules: AssigneeRule[] = []
  for (const item of value) {
    if (typeof item !== 'object' || item === null)
      continue
    const record = item as Record<string, unknown>
    const type = record.type ?? record.Type
    const values = record.values ?? record.Values
    if (typeof type !== 'string' || !type.trim())
      continue
    rules.push({
      type: type.trim(),
      values: Array.isArray(values) ? values.map(entry => String(entry)).filter(entry => entry.trim() !== '') : [],
    })
  }
  return rules
}

/** 规则参数是否填全（不全的规则发布时会被后端拒绝） */
export function isRuleComplete(rule: AssigneeRule): boolean {
  const editor = ASSIGNEE_RULE_TYPE_MAP[rule.type]?.editor
  if (editor === undefined)
    return true
  if (editor === 'none')
    return true
  if (editor === 'level') {
    if (rule.values.length === 0)
      return true
    const level = Number(rule.values[0])
    return Number.isInteger(level) && level >= 1 && level <= MAX_LEADER_LEVEL
  }
  return rule.values.some(value => value.trim() !== '')
}

/**
 * 把旧版受理人写法并入规则：Assignees（主键列表）→ User 规则、AssigneesExpression → Expression 规则、
 * CcUserIds → 抄送 User 规则。设计器只编辑规则，打开旧定义时就地改写，保存即落为新写法（已发布版本不受影响）。
 * 返回是否发生了改写。
 */
export function migrateLegacyAssignees(properties: Record<string, unknown>): boolean {
  let changed = false

  const assigneeRules = readRules(properties.AssigneeRules)
  const legacyAssignees = Array.isArray(properties.Assignees)
    ? properties.Assignees.map(value => String(value)).filter(value => value.trim() !== '')
    : []
  if (legacyAssignees.length > 0)
    assigneeRules.unshift({ type: 'User', values: legacyAssignees })
  if ('Assignees' in properties) {
    delete properties.Assignees
    changed = true
  }

  const legacyExpression = typeof properties.AssigneesExpression === 'string' ? properties.AssigneesExpression.trim() : ''
  if (legacyExpression)
    assigneeRules.splice(legacyAssignees.length > 0 ? 1 : 0, 0, { type: 'Expression', values: [legacyExpression] })
  if ('AssigneesExpression' in properties) {
    delete properties.AssigneesExpression
    changed = true
  }

  if (changed) {
    if (assigneeRules.length > 0)
      properties.AssigneeRules = assigneeRules
    else
      delete properties.AssigneeRules
  }

  if ('CcUserIds' in properties) {
    const ccRules = readRules(properties.CcRules)
    const legacyCc = Array.isArray(properties.CcUserIds)
      ? properties.CcUserIds.map(value => String(value)).filter(value => value.trim() !== '')
      : []
    if (legacyCc.length > 0)
      ccRules.unshift({ type: 'User', values: legacyCc })
    delete properties.CcUserIds
    if (ccRules.length > 0)
      properties.CcRules = ccRules
    changed = true
  }

  return changed
}

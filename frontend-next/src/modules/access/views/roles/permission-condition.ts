import type { ResponseOf } from '~/contracts'
import { permissionDefinitions } from '~/contracts'

export type PermissionConditionsResult = ResponseOf<'permission-conditions.list'>
export type PermissionConditionItem = PermissionConditionsResult['conditions'][number]
export type ConditionBinding = PermissionConditionsResult['bindings'][number]
export type ConditionAttribute = ResponseOf<'permission-conditions.attributes'>['items'][number]
export type ConditionOperator = ConditionAttribute['operators'][number]
export type ConditionValueKind = ConditionAttribute['kind']
export type ConditionSubjectKind = PermissionConditionsResult['subjectKind']
export type ConditionAction = ConditionBinding['action']

/** 全部运算符（还没选属性时列出，4.x 同；选了属性只列它可用的） */
export const CONDITION_OPERATORS: readonly ConditionOperator[] = ['Equals', 'NotEquals', 'GreaterThan', 'GreaterThanOrEquals', 'LessThan', 'LessThanOrEquals', 'Contains', 'NotContains', 'In', 'NotIn', 'Between', 'StartsWith', 'EndsWith', 'IsNull', 'IsNotNull']

/** 权限定义表（生成物）：授权的显示名按它取，不在表里的原样显示权限码 */
const definitions: Readonly<Record<string, { displayName: string } | undefined>> = permissionDefinitions

/** 一条可配置条件的授权：主体在一个权限码上的授予或拒绝 */
export interface GrantConditionTarget {
  /** 列表键 */
  key: string
  subjectKind: ConditionSubjectKind
  subjectId: string
  permission: string
  permissionName: string
  action: ConditionAction
  conditionCount: number
}

/** 主体的绑定转成条件抽屉的条目（按权限码排，同码先授予后拒绝），带上每条绑定上的子句数 */
export function toGrantConditionTargets(result: PermissionConditionsResult): GrantConditionTarget[] {
  const counts = new Map<string, number>()
  for (const condition of result.conditions) {
    const key = `${condition.permission}:${condition.action}`
    counts.set(key, (counts.get(key) ?? 0) + 1)
  }
  return result.bindings
    .map(binding => ({
      key: `${binding.permission}:${binding.action}`,
      subjectKind: result.subjectKind,
      subjectId: result.subjectId,
      permission: binding.permission,
      permissionName: definitions[binding.permission]?.displayName ?? binding.permission,
      action: binding.action,
      conditionCount: counts.get(`${binding.permission}:${binding.action}`) ?? 0,
    }))
    .sort((left, right) => left.permission.localeCompare(right.permission) || left.action.localeCompare(right.action) * -1)
}

/** 一条绑定上的子句 */
export function conditionsOf(result: PermissionConditionsResult | null, target: GrantConditionTarget): PermissionConditionItem[] {
  return (result?.conditions ?? []).filter(condition => condition.permission === target.permission && condition.action === target.action)
}

/** 条件按分组排好：组内且、组间或 */
export function groupConditions(conditions: readonly PermissionConditionItem[]) {
  const groups = new Map<number, PermissionConditionItem[]>()
  for (const condition of conditions) {
    const items = groups.get(condition.groupNo) ?? []
    items.push(condition)
    groups.set(condition.groupNo, items)
  }
  return [...groups.entries()]
    .sort(([left], [right]) => left - right)
    .map(([group, items]) => ({ group, items }))
}

/** 新建条件组时用的下一个组号 */
export function nextGroupNumber(conditions: readonly PermissionConditionItem[]) {
  return conditions.length === 0 ? 0 : Math.max(...conditions.map(condition => condition.groupNo)) + 1
}

/** 条件属性目录项的最小形状：权限条件与访问策略的目录项都满足 */
export type ConditionAttributeLike = Pick<ConditionAttribute, 'aliases' | 'isFamily' | 'name'>

/** 按属性名找目录项：精确名、旧名别名、一族属性 */
export function resolveAttribute<T extends ConditionAttributeLike>(catalog: readonly T[], name: string): { attribute: T | null, isAlias: boolean, familySuffix: string | null } {
  const target = name.trim().toLowerCase()
  const exact = catalog.find(item => !item.isFamily && item.name.toLowerCase() === target)
  if (exact)
    return { attribute: exact, isAlias: false, familySuffix: null }

  const aliased = catalog.find(item => item.aliases.some(alias => alias.toLowerCase() === target))
  if (aliased)
    return { attribute: aliased, isAlias: true, familySuffix: null }

  const family = catalog.find(item => item.isFamily && target.startsWith(item.name.slice(0, -1).toLowerCase()) && target.length > item.name.length - 1)
  if (family)
    return { attribute: family, isAlias: false, familySuffix: name.trim().slice(family.name.length - 1) }

  return { attribute: null, isAlias: false, familySuffix: null }
}

/** 一族属性的完整名：前缀加上填写的名字 */
export function composeAttributeName(attribute: ConditionAttributeLike, familySuffix: null | string | undefined) {
  return attribute.isFamily ? `${attribute.name.slice(0, -1)}${(familySuffix ?? '').trim()}` : attribute.name
}

/** 空值判断的运算符不看条件值 */
export function requiresValue(operator: ConditionOperator) {
  return operator !== 'IsNull' && operator !== 'IsNotNull'
}

/** 条件值的书写提示（语言包键的末段） */
export function valueHintKey(kind: ConditionValueKind, operator: ConditionOperator) {
  if (!requiresValue(operator))
    return 'hint_no_value'
  if (operator === 'Between')
    return kind === 'TimeOfDay' ? 'hint_time_range' : 'hint_range'
  if (operator === 'In' || operator === 'NotIn')
    return kind === 'IpAddress' ? 'hint_ip_list' : 'hint_list'
  switch (kind) {
    case 'Boolean': return 'hint_boolean'
    case 'TimeOfDay': return 'hint_time'
    case 'Date': return 'hint_date'
    case 'DateTime': return 'hint_datetime'
    case 'IpAddress': return 'hint_ip'
    case 'Number': return 'hint_number'
    default: return 'hint_text'
  }
}

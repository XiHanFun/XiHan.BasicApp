import type {
  ApiId,
  PermissionConditionAttributeDto,
  PermissionConditionInventoryItemDto,
  PermissionConditionListItemDto,
} from '@/api'
import {
  ConditionOperator,
  ConditionOutcome,
  ConditionValueKind,
  ConfigDataType,
  PermissionAction,
  PermissionConditionGroupConclusion,
  PermissionConditionTrialVerdict,
  ValidityStatus,
} from '@/api'

/** 一条可配置条件的授权：角色权限或用户直授 */
export interface GrantConditionTarget {
  /** 列表键 */
  key: string
  kind: 'role' | 'user'
  /** 角色权限主键或用户直授主键 */
  bindingId: ApiId
  permissionCode: string
  permissionName: string
  action: PermissionAction
  conditionCount: number
}

/** 色调，与 XiHan.UI 标签一致 */
export type ConditionTone = 'danger' | 'info' | 'neutral' | 'success' | 'warning'

/** 授权列表里能挂条件的一行（角色权限与用户直授共有的字段） */
export interface ConditionableGrant {
  basicId: ApiId
  permissionId: ApiId
  permissionCode?: string | null
  permissionName?: string | null
  permissionAction: PermissionAction
  conditionCount: number
  status: ValidityStatus
}

/** 有效授权转成条件抽屉的条目；撤销是软删除，只认有效的 */
export function toGrantConditionTargets(kind: GrantConditionTarget['kind'], grants: readonly ConditionableGrant[]): GrantConditionTarget[] {
  return grants
    .filter(grant => grant.status === ValidityStatus.Valid)
    .map(grant => ({
      key: `${kind}:${grant.basicId}`,
      kind,
      bindingId: grant.basicId,
      permissionCode: grant.permissionCode ?? '',
      permissionName: grant.permissionName || grant.permissionCode || String(grant.permissionId),
      action: grant.permissionAction,
      conditionCount: grant.conditionCount,
    }))
    .sort((left, right) => left.permissionCode.localeCompare(right.permissionCode))
}

/** 条件按分组排好：组内且、组间或 */
export function groupConditions(conditions: readonly PermissionConditionListItemDto[]) {
  const groups = new Map<number, PermissionConditionListItemDto[]>()
  for (const condition of conditions) {
    const items = groups.get(condition.conditionGroup) ?? []
    items.push(condition)
    groups.set(condition.conditionGroup, items)
  }
  return [...groups.entries()]
    .sort(([left], [right]) => left - right)
    .map(([group, items]) => ({ group, items }))
}

/** 新建条件组时用的下一个组号 */
export function nextGroupNumber(conditions: readonly PermissionConditionListItemDto[]) {
  return conditions.length === 0 ? 0 : Math.max(...conditions.map(condition => condition.conditionGroup)) + 1
}

/** 条件属性目录项的最小形状：权限条件与访问策略的目录项都满足 */
export type ConditionAttributeLike = Pick<PermissionConditionAttributeDto, 'aliases' | 'isFamily' | 'name'>

/** 按属性名找目录项：精确名、旧名别名、一族属性（如 resource.query.status） */
export function resolveAttribute<T extends ConditionAttributeLike>(catalog: readonly T[], name: string): { attribute: T | null, isAlias: boolean, familySuffix: string | null } {
  const target = name.trim().toLowerCase()
  const exact = catalog.find(item => !item.isFamily && item.name.toLowerCase() === target)
  if (exact) {
    return { attribute: exact, isAlias: false, familySuffix: null }
  }
  const aliased = catalog.find(item => item.aliases.some(alias => alias.toLowerCase() === target))
  if (aliased) {
    return { attribute: aliased, isAlias: true, familySuffix: null }
  }
  const family = catalog.find(item => item.isFamily && target.startsWith(item.name.slice(0, -1).toLowerCase()) && target.length > item.name.length - 1)
  if (family) {
    return { attribute: family, isAlias: false, familySuffix: name.trim().slice(family.name.length - 1) }
  }
  return { attribute: null, isAlias: false, familySuffix: null }
}

/** 一族属性的完整名：前缀加上填写的名字 */
export function composeAttributeName(attribute: ConditionAttributeLike, familySuffix: string | null | undefined) {
  return attribute.isFamily ? `${attribute.name.slice(0, -1)}${(familySuffix ?? '').trim()}` : attribute.name
}

/** 空值判断的运算符不看条件值 */
export function requiresValue(operator: ConditionOperator) {
  return operator !== ConditionOperator.IsNull && operator !== ConditionOperator.IsNotNull
}

/** 值种类对应的缺省值类型：布尔、数字照搬，其余按字符串书写 */
export function defaultValueType(kind: ConditionValueKind) {
  if (kind === ConditionValueKind.Boolean) {
    return ConfigDataType.Boolean
  }
  return kind === ConditionValueKind.Number ? ConfigDataType.Number : ConfigDataType.String
}

/** 值类型只决定书写格式：布尔、数字两种只能配同种类的属性 */
export function valueTypeOptionsFor(kind: ConditionValueKind) {
  return Object.values(ConfigDataType).filter(type =>
    (type !== ConfigDataType.Boolean || kind === ConditionValueKind.Boolean)
    && (type !== ConfigDataType.Number || kind === ConditionValueKind.Number))
}

/** 条件值的书写提示（语言包键的末段） */
export function valueHintKey(kind: ConditionValueKind, operator: ConditionOperator) {
  if (!requiresValue(operator)) {
    return 'hint_no_value'
  }
  if (operator === ConditionOperator.Between) {
    return kind === ConditionValueKind.TimeOfDay ? 'hint_time_range' : 'hint_range'
  }
  if (operator === ConditionOperator.In || operator === ConditionOperator.NotIn) {
    return kind === ConditionValueKind.IpAddress ? 'hint_ip_list' : 'hint_list'
  }
  switch (kind) {
    case ConditionValueKind.Boolean:
      return 'hint_boolean'
    case ConditionValueKind.TimeOfDay:
      return 'hint_time'
    case ConditionValueKind.Date:
      return 'hint_date'
    case ConditionValueKind.DateTime:
      return 'hint_datetime'
    case ConditionValueKind.IpAddress:
      return 'hint_ip'
    case ConditionValueKind.Number:
      return 'hint_number'
    default:
      return 'hint_text'
  }
}

/** 授权语义：授予上是「满足才授予」，禁止上是「满足才禁止」（语言包键的末段） */
export function semanticsKey(action: PermissionAction) {
  return action === PermissionAction.Deny ? 'semantics_deny' : 'semantics_grant'
}

/** 求值结果的色调 */
export function outcomeTone(outcome: ConditionOutcome | null | undefined): ConditionTone {
  switch (outcome) {
    case ConditionOutcome.Matched:
      return 'success'
    case ConditionOutcome.NotMatched:
      return 'danger'
    case ConditionOutcome.Pending:
      return 'info'
    default:
      return 'warning'
  }
}

/** 试算结论的色调 */
export function verdictTone(verdict: PermissionConditionTrialVerdict): ConditionTone {
  switch (verdict) {
    case PermissionConditionTrialVerdict.Granted:
    case PermissionConditionTrialVerdict.Unconditional:
      return 'success'
    case PermissionConditionTrialVerdict.Denied:
      return 'danger'
    case PermissionConditionTrialVerdict.Pending:
      return 'info'
    default:
      return 'neutral'
  }
}

/** 条件组结论的色调 */
export function conclusionTone(conclusion: PermissionConditionGroupConclusion): ConditionTone {
  switch (conclusion) {
    case PermissionConditionGroupConclusion.Evaluable:
      return 'success'
    case PermissionConditionGroupConclusion.NeverMatches:
      return 'danger'
    case PermissionConditionGroupConclusion.BindingInactive:
      return 'warning'
    default:
      return 'neutral'
  }
}

/** 「名=值」逐行解析成字典，空行与没有等号的行忽略 */
export function parseKeyValueLines(text: string | null | undefined) {
  const result: Record<string, string> = {}
  for (const line of (text ?? '').split(/\r?\n/)) {
    const index = line.indexOf('=')
    const name = index > 0 ? line.slice(0, index).trim() : ''
    if (name) {
      result[name] = line.slice(index + 1).trim()
    }
  }
  return result
}

/** 盘点明细导出成 CSV：表头与诊断、结论都用调用方给的文案 */
export function inventoryToCsv(
  items: readonly PermissionConditionInventoryItemDto[],
  headers: readonly string[],
  describe: (item: PermissionConditionInventoryItemDto) => readonly string[],
) {
  const escape = (value: string) => (/[",\r\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value)
  const lines = [headers, ...items.map(describe)].map(row => row.map(cell => escape(cell ?? '')).join(','))
  // 带 BOM，表格软件按 UTF-8 打开中文不乱码
  return `\uFEFF${lines.join('\r\n')}`
}

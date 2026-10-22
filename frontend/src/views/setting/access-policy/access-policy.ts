import type { AccessPolicyAttributeDto, AccessPolicyConditionDto } from '@/api'
import { AccessPolicyAction, AccessPolicyPhase, ConditionOperator, ConfigDataType } from '@/api'
import { defaultValueType, requiresValue, resolveAttribute } from '../../identity/components/permission-condition'

/** 色调，与 XiHan.UI 标签一致 */
export type AccessPolicyTone = 'danger' | 'info' | 'neutral' | 'success' | 'warning'

/** 编辑器里的一条条件：属性名拆成目录项与一族属性的后缀，取反用开关 */
export interface ConditionRowModel {
  /** 列表键 */
  key: number
  attribute: string
  familySuffix: string
  operator: ConditionOperator
  isNegated: boolean
  conditionValue: string
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
  return { key: newKey(), attribute: '', familySuffix: '', operator: ConditionOperator.Equals, isNegated: false, conditionValue: '' }
}

/** 已保存的条件按组号转成编辑器的组 */
export function toGroups(conditions: readonly AccessPolicyConditionDto[], catalog: readonly AccessPolicyAttributeDto[]): ConditionGroupModel[] {
  const groups = new Map<number, ConditionRowModel[]>()
  for (const condition of [...conditions].sort((left, right) => left.conditionGroup - right.conditionGroup)) {
    const resolved = resolveAttribute(catalog, condition.attributeName)
    const rows = groups.get(condition.conditionGroup) ?? []
    rows.push({
      key: newKey(),
      attribute: resolved.attribute?.name ?? condition.attributeName,
      familySuffix: resolved.familySuffix ?? '',
      operator: condition.operator,
      isNegated: condition.isNegated,
      conditionValue: condition.conditionValue,
    })
    groups.set(condition.conditionGroup, rows)
  }
  return [...groups.values()].map(rows => ({ key: newKey(), rows }))
}

/** 编辑器的组转回条件：组号按顺序重排，值类型按属性的值种类给 */
export function toConditions(groups: readonly ConditionGroupModel[], catalog: readonly AccessPolicyAttributeDto[]): AccessPolicyConditionDto[] {
  return groups
    .filter(group => group.rows.length > 0)
    .flatMap((group, index) => group.rows.map((row) => {
      const attribute = catalog.find(item => item.name === row.attribute)
      return {
        conditionGroup: index,
        attributeName: attribute?.isFamily ? `${attribute.name.slice(0, -1)}${row.familySuffix.trim()}` : row.attribute,
        operator: row.operator,
        isNegated: row.isNegated,
        valueType: attribute ? defaultValueType(attribute.kind) : ConfigDataType.String,
        conditionValue: requiresValue(row.operator) ? row.conditionValue.trim() : '',
        description: null,
      }
    }))
}

/** 编辑器里的第一处没填完的地方（语言包键的末段与参数）；都填好时为空 */
export function findIncomplete(groups: readonly ConditionGroupModel[], catalog: readonly AccessPolicyAttributeDto[]) {
  const rows = groups.flatMap(group => group.rows)
  if (rows.length === 0) {
    return { key: 'warn_conditions_required', index: 0 }
  }
  for (const [index, row] of rows.entries()) {
    const attribute = catalog.find(item => item.name === row.attribute)
    if (!attribute) {
      return { key: 'warn_attribute_required', index: index + 1 }
    }
    if (attribute.isFamily && !row.familySuffix.trim()) {
      return { key: 'warn_family_suffix_required', index: index + 1 }
    }
    if (requiresValue(row.operator) && !row.conditionValue.trim()) {
      return { key: 'warn_value_required', index: index + 1 }
    }
  }
  return null
}

/** 求值时机：只用到认证前可取的属性时在认证前 */
export function phaseOf(groups: readonly ConditionGroupModel[], catalog: readonly AccessPolicyAttributeDto[]) {
  const attributes = groups.flatMap(group => group.rows).map(row => catalog.find(item => item.name === row.attribute))
  return attributes.every(attribute => attribute?.isBeforeAuthentication ?? true)
    ? AccessPolicyPhase.BeforeAuthentication
    : AccessPolicyPhase.AfterAuthentication
}

/** 动作的色调 */
export function actionTone(action: AccessPolicyAction): AccessPolicyTone {
  switch (action) {
    case AccessPolicyAction.Deny:
      return 'danger'
    case AccessPolicyAction.Allow:
      return 'success'
    default:
      return 'neutral'
  }
}

/** 登录失败自动封禁参数的配置键 */
export const LOGIN_AUTO_BAN_CONFIG_KEY = 'saas.security.login-auto-ban'

/** 登录失败自动封禁参数（配置值的 JSON） */
export interface LoginAutoBanSettings {
  isEnabled: boolean
  windowMinutes: number
  maxFailures: number
  banMinutes: number
  maxBanMinutes: number
}

const AUTO_BAN_NUMBER_KEYS = ['windowMinutes', 'maxFailures', 'banMinutes', 'maxBanMinutes'] as const

function parseObject(json: string | null | undefined): Record<string, unknown> | null {
  if (!json?.trim()) {
    return {}
  }
  try {
    const value: unknown = JSON.parse(json)
    return value !== null && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : null
  }
  catch {
    return null
  }
}

/** 读自动封禁参数：当前值逐项覆盖缺省值；解析不了或缺项、类型不对时返回 null */
export function readAutoBanSettings(configValue: string | null | undefined, defaultValue: string | null | undefined): LoginAutoBanSettings | null {
  const defaults = parseObject(defaultValue)
  const current = parseObject(configValue)
  if (!defaults || !current) {
    return null
  }
  const merged = { ...defaults, ...current }
  if (typeof merged.isEnabled !== 'boolean' || AUTO_BAN_NUMBER_KEYS.some(key => !Number.isInteger(merged[key]))) {
    return null
  }
  return {
    isEnabled: merged.isEnabled,
    windowMinutes: merged.windowMinutes as number,
    maxFailures: merged.maxFailures as number,
    banMinutes: merged.banMinutes as number,
    maxBanMinutes: merged.maxBanMinutes as number,
  }
}

/** 写回自动封禁参数的 JSON */
export function writeAutoBanSettings(settings: LoginAutoBanSettings) {
  return JSON.stringify({
    isEnabled: settings.isEnabled,
    windowMinutes: settings.windowMinutes,
    maxFailures: settings.maxFailures,
    banMinutes: settings.banMinutes,
    maxBanMinutes: settings.maxBanMinutes,
  })
}

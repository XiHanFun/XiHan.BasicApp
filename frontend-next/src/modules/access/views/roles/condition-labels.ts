import type { ConditionOperator, ConditionValueKind } from './permission-condition'
import { requiresValue, valueHintKey } from './permission-condition'

type Translate = (key: string, values?: Record<string, unknown>) => string

// 枚举值与登记名是契约字面量，文案键静态写出以便 i18n 门禁核对

/** 运算符的显示名 */
export function operatorLabel(t: Translate, operator: ConditionOperator): string {
  switch (operator) {
    case 'Equals': return t('access.conditions.operators.equals')
    case 'NotEquals': return t('access.conditions.operators.not_equals')
    case 'GreaterThan': return t('access.conditions.operators.greater_than')
    case 'GreaterThanOrEquals': return t('access.conditions.operators.greater_than_or_equals')
    case 'LessThan': return t('access.conditions.operators.less_than')
    case 'LessThanOrEquals': return t('access.conditions.operators.less_than_or_equals')
    case 'Contains': return t('access.conditions.operators.contains')
    case 'NotContains': return t('access.conditions.operators.not_contains')
    case 'In': return t('access.conditions.operators.in')
    case 'NotIn': return t('access.conditions.operators.not_in')
    case 'Between': return t('access.conditions.operators.between')
    case 'StartsWith': return t('access.conditions.operators.starts_with')
    case 'EndsWith': return t('access.conditions.operators.ends_with')
    case 'IsNull': return t('access.conditions.operators.is_null')
    case 'IsNotNull': return t('access.conditions.operators.is_not_null')
  }
}

/** 登记属性的显示名；未登记的原样返回 */
export function attributeLabel(t: Translate, name: string): string {
  switch (name.toLowerCase()) {
    case 'subject.userid': return t('access.conditions.attributes.subject_user_id')
    case 'subject.username': return t('access.conditions.attributes.subject_user_name')
    case 'subject.tenantcode': return t('access.conditions.attributes.subject_tenant_code')
    case 'subject.rolecodes': return t('access.conditions.attributes.subject_role_codes')
    case 'subject.departmentcodes': return t('access.conditions.attributes.subject_department_codes')
    case 'subject.maindepartmentcode': return t('access.conditions.attributes.subject_main_department_code')
    case 'subject.positioncodes': return t('access.conditions.attributes.subject_position_codes')
    case 'subject.isimpersonating': return t('access.conditions.attributes.subject_is_impersonating')
    case 'resource.permissioncodes': return t('access.conditions.attributes.resource_permission_codes')
    case 'resource.path': return t('access.conditions.attributes.resource_path')
    case 'resource.method': return t('access.conditions.attributes.resource_method')
    case 'resource.operation': return t('access.conditions.attributes.resource_operation')
    case 'environment.ip': return t('access.conditions.attributes.environment_ip')
    case 'environment.geo.country': return t('access.conditions.attributes.environment_geo_country')
    case 'environment.geo.province': return t('access.conditions.attributes.environment_geo_province')
    case 'environment.geo.city': return t('access.conditions.attributes.environment_geo_city')
    case 'environment.geo.isp': return t('access.conditions.attributes.environment_geo_isp')
    case 'environment.time': return t('access.conditions.attributes.environment_time')
    case 'environment.hour': return t('access.conditions.attributes.environment_hour')
    case 'environment.weekday': return t('access.conditions.attributes.environment_weekday')
    case 'environment.date': return t('access.conditions.attributes.environment_date')
    case 'environment.client.useragent': return t('access.conditions.attributes.environment_user_agent')
    case 'environment.client.kind': return t('access.conditions.attributes.environment_client_kind')
    default: return name
  }
}

/** 条件值的书写提示 */
export function valueHint(t: Translate, kind: ConditionValueKind, operator: ConditionOperator): string {
  switch (valueHintKey(kind, operator)) {
    case 'hint_no_value': return t('access.conditions.hint_no_value')
    case 'hint_time_range': return t('access.conditions.hint_time_range')
    case 'hint_range': return t('access.conditions.hint_range')
    case 'hint_ip_list': return t('access.conditions.hint_ip_list')
    case 'hint_list': return t('access.conditions.hint_list')
    case 'hint_boolean': return t('access.conditions.hint_boolean')
    case 'hint_time': return t('access.conditions.hint_time')
    case 'hint_date': return t('access.conditions.hint_date')
    case 'hint_datetime': return t('access.conditions.hint_datetime')
    case 'hint_ip': return t('access.conditions.hint_ip')
    case 'hint_number': return t('access.conditions.hint_number')
    default: return t('access.conditions.hint_text')
  }
}

/** 条件的一行文字：属性、运算符（取反加前缀）与值 */
export function describeCondition(t: Translate, condition: { attributeName: string, operator: ConditionOperator, isNegated: boolean, value?: null | string }): string {
  const parts = [attributeLabel(t, condition.attributeName), `${condition.isNegated ? t('access.conditions.negated_prefix') : ''}${operatorLabel(t, condition.operator)}`]
  if (requiresValue(condition.operator) && condition.value)
    parts.push(condition.value)
  return parts.join(' ')
}

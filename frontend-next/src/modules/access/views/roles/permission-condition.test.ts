import { describe, expect, it } from 'vitest'
import { describeCondition, operatorLabel } from './condition-labels'
import { composeAttributeName, conditionsOf, groupConditions, nextGroupNumber, requiresValue, resolveAttribute, toGrantConditionTargets, valueHintKey } from './permission-condition'

function condition(id: string, permission: string, action: 'Deny' | 'Grant', groupNo: number) {
  return { id, permission, action, groupNo, attributeName: 'subject.userName', operator: 'Equals' as const, isNegated: false, value: 'dana', description: null, status: 'Active' as const, createdAt: '2026-10-29T00:00:00Z' }
}

const result = {
  subjectKind: 'Role' as const,
  subjectId: 'r-1',
  bindings: [
    { permission: 'identity.users.update', action: 'Grant' as const, effectiveAt: null, expiresAt: null },
    { permission: 'identity.users.read', action: 'Deny' as const, effectiveAt: null, expiresAt: null },
    { permission: 'identity.users.read', action: 'Grant' as const, effectiveAt: null, expiresAt: null },
  ],
  conditions: [condition('c-1', 'identity.users.read', 'Grant', 0), condition('c-2', 'identity.users.read', 'Grant', 2), condition('c-3', 'identity.users.read', 'Deny', 0)],
}

const catalog = [
  { name: 'subject.roleCodes', aliases: ['subject.role'], isFamily: false },
  { name: 'resource.route.*', aliases: [], isFamily: true },
]

describe('授权条件的条目', () => {
  it('按权限码排、同码先授予后拒绝，带上每条绑定的子句数与显示名', () => {
    const targets = toGrantConditionTargets(result)
    expect(targets.map(t => [t.permission, t.action, t.conditionCount])).toEqual([
      ['identity.users.read', 'Grant', 2],
      ['identity.users.read', 'Deny', 1],
      ['identity.users.update', 'Grant', 0],
    ])
    expect(targets[0]!.permissionName).toBe('Read users')
    expect(conditionsOf(result, targets[1]!).map(c => c.id)).toEqual(['c-3'])
  })

  it('子句按组号分组；新组号接在最大的后面', () => {
    const conditions = conditionsOf(result, toGrantConditionTargets(result)[0]!)
    expect(groupConditions(conditions).map(g => [g.group, g.items.length])).toEqual([[0, 1], [2, 1]])
    expect(nextGroupNumber(conditions)).toBe(3)
    expect(nextGroupNumber([])).toBe(0)
  })
})

describe('属性与运算符', () => {
  it('属性按精确名、别名、族前缀找；族的完整名是前缀加填写的名字', () => {
    expect(resolveAttribute(catalog, 'SUBJECT.ROLECODES')).toMatchObject({ attribute: catalog[0], isAlias: false })
    expect(resolveAttribute(catalog, 'subject.role')).toMatchObject({ attribute: catalog[0], isAlias: true })
    expect(resolveAttribute(catalog, 'resource.route.id')).toMatchObject({ attribute: catalog[1], familySuffix: 'id' })
    expect(resolveAttribute(catalog, 'subject.shoeSize').attribute).toBeNull()
    expect(composeAttributeName(catalog[1]!, ' id ')).toBe('resource.route.id')
    expect(composeAttributeName(catalog[0]!, 'ignored')).toBe('subject.roleCodes')
  })

  it('判空不要值；提示按种类与运算符选', () => {
    expect(requiresValue('IsNull')).toBe(false)
    expect(requiresValue('In')).toBe(true)
    expect(valueHintKey('IpAddress', 'In')).toBe('hint_ip_list')
    expect(valueHintKey('TimeOfDay', 'Between')).toBe('hint_time_range')
    expect(valueHintKey('String', 'IsNotNull')).toBe('hint_no_value')
    expect(valueHintKey('Number', 'GreaterThan')).toBe('hint_number')
  })

  it('一行文字：属性、取反与运算符、值；判空不带值', () => {
    const t = (key: string) => key
    expect(operatorLabel(t, 'NotIn')).toBe('access.conditions.operators.not_in')
    expect(describeCondition(t, { attributeName: 'environment.ip', operator: 'In', isNegated: true, value: '10.0.0.0/8' }))
      .toBe('access.conditions.attributes.environment_ip access.conditions.negated_prefixaccess.conditions.operators.in 10.0.0.0/8')
    expect(describeCondition(t, { attributeName: 'custom.thing', operator: 'IsNull', isNegated: false, value: 'x' })).toBe('custom.thing access.conditions.operators.is_null')
  })
})

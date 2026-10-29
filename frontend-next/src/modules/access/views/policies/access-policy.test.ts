import type { ConditionAttribute } from '../roles/permission-condition'
import { describe, expect, it } from 'vitest'
import { actionTone, codeFromTemplate, emptyRow, findIncomplete, incompleteMessage, phaseOf, templateText, toConditions, toGroups, usesPlaceholderNetwork } from './access-policy'

function attribute(name: string, kind: ConditionAttribute['kind'], isBeforeAuthentication: boolean, isFamily = false): ConditionAttribute {
  return { name, kind, isMultiValued: false, isFamily, aliases: [], operators: ['Equals', 'In', 'IsNull'], isBeforeAuthentication }
}

const catalog = [
  attribute('environment.ip', 'IpAddress', true),
  attribute('subject.userName', 'String', false),
  attribute('resource.route.*', 'String', false, true),
]

describe('访问策略的条件编辑', () => {
  it('已保存的条件按组号分组，转回时空组去掉、组号重排、判空不带值、一族属性拼回全名', () => {
    const groups = toGroups([
      { groupNo: 3, attributeName: 'subject.userName', operator: 'Equals', isNegated: true, value: 'dana' },
      { groupNo: 1, attributeName: 'environment.ip', operator: 'In', isNegated: false, value: '10.0.0.0/8' },
      { groupNo: 1, attributeName: 'resource.route.id', operator: 'IsNull', isNegated: false, value: null },
    ], catalog)
    expect(groups.map(g => g.rows.map(r => [r.attribute, r.familySuffix]))).toEqual([
      [['environment.ip', ''], ['resource.route.*', 'id']],
      [['subject.userName', '']],
    ])
    const withEmpty = [...groups, { key: 999, rows: [] }]
    expect(toConditions(withEmpty, catalog)).toEqual([
      { groupNo: 0, attributeName: 'environment.ip', operator: 'In', isNegated: false, value: '10.0.0.0/8', description: null },
      { groupNo: 0, attributeName: 'resource.route.id', operator: 'IsNull', isNegated: false, value: null, description: null },
      { groupNo: 1, attributeName: 'subject.userName', operator: 'Equals', isNegated: true, value: 'dana', description: null },
    ])
  })

  it('找出第一处没填完的地方', () => {
    expect(findIncomplete([], catalog)).toEqual({ reason: 'conditions_required', index: 0 })
    const row = emptyRow()
    expect(findIncomplete([{ key: 1, rows: [row] }], catalog)).toEqual({ reason: 'attribute_required', index: 1 })
    row.attribute = 'resource.route.*'
    expect(findIncomplete([{ key: 1, rows: [row] }], catalog)?.reason).toBe('family_suffix_required')
    row.familySuffix = 'id'
    expect(findIncomplete([{ key: 1, rows: [row] }], catalog)?.reason).toBe('value_required')
    row.operator = 'IsNull'
    expect(findIncomplete([{ key: 1, rows: [row] }], catalog)).toBeNull()
    expect(incompleteMessage(key => key, { reason: 'value_required', index: 2 })).toBe('access.policies.editor.warn_value_required')
  })

  it('时机：平台策略只用认证前属性即认证前，租户策略一律认证后', () => {
    const ipOnly = toGroups([{ groupNo: 0, attributeName: 'environment.ip', operator: 'In', isNegated: false, value: '10.0.0.0/8' }], catalog)
    const withUser = toGroups([{ groupNo: 0, attributeName: 'subject.userName', operator: 'Equals', isNegated: false, value: 'dana' }], catalog)
    expect(phaseOf(ipOnly, catalog, true)).toBe('BeforeAuthentication')
    expect(phaseOf(withUser, catalog, true)).toBe('AfterAuthentication')
    expect(phaseOf(ipOnly, catalog, false)).toBe('AfterAuthentication')
  })
})

describe('访问策略的展示', () => {
  it('模板：编码去掉 preset. 前缀，名称按界面语言，认不出的模板用原文', () => {
    expect(codeFromTemplate('preset.ip-blacklist')).toBe('ip-blacklist')
    expect(templateText(key => key, { code: 'preset.off-hours', name: 'Off-hours access', description: 'x' }).name).toBe('access.policies.templates.off_hours.name')
    expect(templateText(key => key, { code: 'custom', name: 'Custom', description: 'Kept' })).toEqual({ name: 'Custom', description: 'Kept' })
  })

  it('占位网段提示与动作色调', () => {
    expect(usesPlaceholderNetwork([{ value: '203.0.113.0/24,10.0.0.0/8' }])).toBe(true)
    expect(usesPlaceholderNetwork([{ value: null }, { value: '10.0.0.0/8' }])).toBe(false)
    expect([actionTone('Deny'), actionTone('Allow'), actionTone('LogOnly')]).toEqual(['danger', 'success', 'neutral'])
  })
})

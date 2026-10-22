import type { AccessPolicyAttributeDto } from '@/api'
import { describe, expect, it } from 'vitest'
import { AccessPolicyAction, AccessPolicyPhase, ConditionOperator, ConditionValueKind, ConfigDataType } from '@/api'
import { actionTone, emptyRow, findIncomplete, phaseOf, readAutoBanSettings, toConditions, toGroups, trialUserLabel, writeAutoBanSettings } from './access-policy'

function attribute(name: string, kind: ConditionValueKind, isBeforeAuthentication: boolean, extra: Partial<AccessPolicyAttributeDto> = {}): AccessPolicyAttributeDto {
  return { name, kind, isBeforeAuthentication, aliases: [], isFamily: false, isLegacy: false, isMultiValued: false, operators: [], ...extra }
}

const catalog = [
  attribute('environment.ip', ConditionValueKind.IpAddress, true),
  attribute('environment.weekday', ConditionValueKind.Number, true),
  attribute('subject.roleCodes', ConditionValueKind.String, false, { aliases: ['subject.role'], isMultiValued: true }),
  attribute('resource.query.*', ConditionValueKind.String, false, { isFamily: true }),
]

describe('访问策略条件编辑', () => {
  it('已保存的条件按组号分组，转回时组号按顺序重排、一族属性拼回全名、值类型按值种类给', () => {
    const groups = toGroups([
      { conditionGroup: 5, attributeName: 'environment.weekday', operator: ConditionOperator.In, isNegated: false, valueType: ConfigDataType.Number, conditionValue: '6,7' },
      { conditionGroup: 2, attributeName: 'subject.role', operator: ConditionOperator.In, isNegated: false, valueType: ConfigDataType.String, conditionValue: 'admin' },
      { conditionGroup: 2, attributeName: 'resource.query.status', operator: ConditionOperator.Equals, isNegated: true, valueType: ConfigDataType.String, conditionValue: 'draft' },
    ], catalog)

    expect(groups.map(group => group.rows.map(row => [row.attribute, row.familySuffix]))).toEqual([
      [['subject.roleCodes', ''], ['resource.query.*', 'status']],
      [['environment.weekday', '']],
    ])
    expect(toConditions(groups, catalog)).toEqual([
      { conditionGroup: 0, attributeName: 'subject.roleCodes', operator: ConditionOperator.In, isNegated: false, valueType: ConfigDataType.String, conditionValue: 'admin', description: null },
      { conditionGroup: 0, attributeName: 'resource.query.status', operator: ConditionOperator.Equals, isNegated: true, valueType: ConfigDataType.String, conditionValue: 'draft', description: null },
      { conditionGroup: 1, attributeName: 'environment.weekday', operator: ConditionOperator.In, isNegated: false, valueType: ConfigDataType.Number, conditionValue: '6,7', description: null },
    ])
  })

  it('空值判断不带条件值，空组不提交', () => {
    const row = { ...emptyRow(), attribute: 'environment.ip', operator: ConditionOperator.IsNull, conditionValue: '10.0.0.1' }

    expect(toConditions([{ key: 1, rows: [row] }, { key: 2, rows: [] }], catalog)).toEqual([
      expect.objectContaining({ conditionGroup: 0, conditionValue: '', valueType: ConfigDataType.String }),
    ])
  })

  it('提交前指出第一处没填完的条件', () => {
    expect(findIncomplete([], catalog)).toEqual({ key: 'warn_conditions_required', index: 0 })
    expect(findIncomplete([{ key: 1, rows: [emptyRow()] }], catalog)).toEqual({ key: 'warn_attribute_required', index: 1 })
    expect(findIncomplete([{ key: 1, rows: [{ ...emptyRow(), attribute: 'resource.query.*', conditionValue: 'x' }] }], catalog)).toEqual({ key: 'warn_family_suffix_required', index: 1 })
    expect(findIncomplete([{ key: 1, rows: [{ ...emptyRow(), attribute: 'environment.ip' }] }], catalog)).toEqual({ key: 'warn_value_required', index: 1 })
    expect(findIncomplete([{ key: 1, rows: [{ ...emptyRow(), attribute: 'environment.ip', operator: ConditionOperator.IsNotNull }] }], catalog)).toBeNull()
  })

  it('只用到认证前可取的属性时在认证前求值', () => {
    const ip = { ...emptyRow(), attribute: 'environment.ip' }
    const role = { ...emptyRow(), attribute: 'subject.roleCodes' }

    expect(phaseOf([{ key: 1, rows: [ip] }], catalog)).toBe(AccessPolicyPhase.BeforeAuthentication)
    expect(phaseOf([{ key: 1, rows: [ip] }, { key: 2, rows: [role] }], catalog)).toBe(AccessPolicyPhase.AfterAuthentication)
  })

  it('动作的色调：拒绝红、放行绿、只记录灰', () => {
    expect([AccessPolicyAction.Deny, AccessPolicyAction.Allow, AccessPolicyAction.LogOnly].map(actionTone)).toEqual(['danger', 'success', 'neutral'])
  })

  it('自动封禁参数：当前值逐项覆盖缺省值，写回只带五项', () => {
    const defaults = '{"isEnabled":false,"windowMinutes":10,"maxFailures":20,"banMinutes":30,"maxBanMinutes":1440}'
    const settings = readAutoBanSettings('{"isEnabled":true,"maxFailures":5}', defaults)
    expect(settings).toEqual({ isEnabled: true, windowMinutes: 10, maxFailures: 5, banMinutes: 30, maxBanMinutes: 1440 })
    expect(readAutoBanSettings(null, defaults)?.isEnabled).toBe(false)
    expect(JSON.parse(writeAutoBanSettings({ ...settings!, banMinutes: 60 }))).toEqual({ isEnabled: true, windowMinutes: 10, maxFailures: 5, banMinutes: 60, maxBanMinutes: 1440 })
  })

  it('试运行结果里的用户：选了人显示下拉里的名字，没选显示当前用户，选项里找不到时退回主键', () => {
    const options = [{ label: '张三 (@zhangsan)', value: '7' }]
    expect(trialUserLabel('7', options, '当前用户')).toBe('张三 (@zhangsan)')
    expect(trialUserLabel(null, options, '当前用户')).toBe('当前用户')
    expect(trialUserLabel('9', options, '当前用户')).toBe('9')
  })

  it('自动封禁参数：解析不了或类型不对时返回 null，不拿猜的值顶上', () => {
    const defaults = '{"isEnabled":false,"windowMinutes":10,"maxFailures":20,"banMinutes":30,"maxBanMinutes":1440}'
    expect(readAutoBanSettings('{oops', defaults)).toBeNull()
    expect(readAutoBanSettings('{"isEnabled":"yes"}', defaults)).toBeNull()
    expect(readAutoBanSettings('{"maxFailures":2.5}', defaults)).toBeNull()
    expect(readAutoBanSettings('[]', defaults)).toBeNull()
    expect(readAutoBanSettings('{"isEnabled":true}', null)).toBeNull()
  })
})

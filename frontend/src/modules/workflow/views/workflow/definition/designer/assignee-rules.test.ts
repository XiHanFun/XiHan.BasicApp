/**
 * 受理人规则模型（assignee-rules.ts）单元测试。
 *
 * 职责边界：规则以 `{ type, values }` 原样写进节点属性、由后端解析器解释，前端只负责
 * 读得出（驼峰/帕斯卡两种形状）、判得出参数是否填全、以及把旧版写法一次并入规则。
 */
import { describe, expect, it } from 'vitest'
import {
  ASSIGNEE_RULE_TYPE_MAP,
  ASSIGNEE_RULE_TYPES,
  createRule,
  isRuleComplete,
  MAX_LEADER_LEVEL,
  migrateLegacyAssignees,
  readRules,
} from './assignee-rules'

describe('规则类型目录', () => {
  it('类型唯一且与后端规则类型名同形（大驼峰）', () => {
    const types = ASSIGNEE_RULE_TYPES.map(meta => meta.type)

    expect(new Set(types).size).toBe(types.length)
    expect(types.filter(type => !/^[A-Z][A-Za-z]*$/.test(type))).toEqual([])
  })

  it('覆盖框架内置三种与 BasicApp 组织架构六种规则', () => {
    expect(Object.keys(ASSIGNEE_RULE_TYPE_MAP).sort()).toEqual([
      'Department',
      'DepartmentLeader',
      'Expression',
      'Position',
      'Role',
      'Starter',
      'StarterLeader',
      'StarterLeaderChain',
      'User',
    ])
  })
})

describe('readRules', () => {
  it('驼峰与帕斯卡两种形状都认，参数统一成非空字符串', () => {
    expect(readRules([
      { type: 'Role', values: ['finance', ''] },
      { Type: 'User', Values: [1001, '1002'] },
    ])).toEqual([
      { type: 'Role', values: ['finance'] },
      { type: 'User', values: ['1001', '1002'] },
    ])
  })

  it('非数组、无类型的项与非对象项一律丢弃，不抛错', () => {
    expect(readRules(null)).toEqual([])
    expect(readRules('Role')).toEqual([])
    expect(readRules([null, 'x', { values: ['a'] }, { type: '  ' }])).toEqual([])
  })
})

describe('createRule 与 isRuleComplete', () => {
  it('主管类规则缺省 1 级且视为完整，其余规则缺省无参数', () => {
    expect(createRule('StarterLeader')).toEqual({ type: 'StarterLeader', values: ['1'] })
    expect(createRule('Role')).toEqual({ type: 'Role', values: [] })
  })

  it('发起人规则无参数即完整；编码与用户类规则至少要一个值', () => {
    expect(isRuleComplete({ type: 'Starter', values: [] })).toBe(true)
    expect(isRuleComplete({ type: 'Role', values: [] })).toBe(false)
    expect(isRuleComplete({ type: 'User', values: ['1001'] })).toBe(true)
    expect(isRuleComplete({ type: 'Expression', values: ['  '] })).toBe(false)
  })

  it('主管层级只接受 1 到上限的整数', () => {
    expect(isRuleComplete({ type: 'StarterLeaderChain', values: [String(MAX_LEADER_LEVEL)] })).toBe(true)
    expect(isRuleComplete({ type: 'StarterLeader', values: ['0'] })).toBe(false)
    expect(isRuleComplete({ type: 'StarterLeader', values: ['1.5'] })).toBe(false)
    expect(isRuleComplete({ type: 'StarterLeader', values: [String(MAX_LEADER_LEVEL + 1)] })).toBe(false)
  })

  it('未登记的规则类型（应用自己扩展的）不在前端判定，交给后端发布校验', () => {
    expect(isRuleComplete({ type: 'CostCenterOwner', values: [] })).toBe(true)
  })
})

describe('migrateLegacyAssignees', () => {
  it('旧写法按「静态列表 → 表达式 → 既有规则」的顺序并入，保持依次审批的排队顺序', () => {
    const properties: Record<string, unknown> = {
      Assignees: ['1001'],
      AssigneesExpression: 'approverIds',
      AssigneeRules: [{ type: 'Role', values: ['finance'] }],
    }

    expect(migrateLegacyAssignees(properties)).toBe(true)
    expect(properties).toEqual({
      AssigneeRules: [
        { type: 'User', values: ['1001'] },
        { type: 'Expression', values: ['approverIds'] },
        { type: 'Role', values: ['finance'] },
      ],
    })
  })

  it('空的旧键也删除，且不凭空写出空规则列表', () => {
    const properties: Record<string, unknown> = { Assignees: [], AssigneesExpression: '' }

    expect(migrateLegacyAssignees(properties)).toBe(true)
    expect(properties).toEqual({})
  })

  it('没有旧写法时不改动属性', () => {
    const properties: Record<string, unknown> = { AssigneeRules: [{ type: 'Starter', values: [] }] }

    expect(migrateLegacyAssignees(properties)).toBe(false)
    expect(properties).toEqual({ AssigneeRules: [{ type: 'Starter', values: [] }] })
  })
})

import type { ConstraintRuleItem } from './constraint-rule'
import { describe, expect, it } from 'vitest'
import { actionTone, emptyItem, findItemProblem, itemProblemMessage, minItemsOf, targetTypesOf, toItemInputs, toItemModels, usesMaxAllowed } from './constraint-rule'

describe('约束规则的编辑', () => {
  it('约束类型决定可选的目标类型、最少规则项与是否用数量上限（与后端一致）', () => {
    expect(targetTypesOf('Ssd')).toEqual(['Role'])
    expect(targetTypesOf('Dsd')).toEqual(['Role'])
    expect(targetTypesOf('Condition')).toEqual(['Role'])
    expect(targetTypesOf('MutualExclusion')).toEqual(['Permission'])
    expect(targetTypesOf('Cardinality')).toEqual(['Role', 'User'])
    expect([minItemsOf('Ssd'), minItemsOf('Dsd'), minItemsOf('MutualExclusion'), minItemsOf('Cardinality'), minItemsOf('Condition')]).toEqual([2, 2, 2, 1, 1])
    expect(usesMaxAllowed('Cardinality')).toBe(true)
    expect(usesMaxAllowed('Condition')).toBe(false)
  })

  it('已保存的规则项转成编辑行再转回，备注留空写成 null', () => {
    const saved: ConstraintRuleItem[] = [
      { targetId: 'r1', targetName: '出纳', targetCode: 'cashier', groupNo: 0, remark: '财务' },
      { targetId: 'r2', targetName: null, targetCode: null, groupNo: 1, remark: null },
    ]
    const models = toItemModels(saved)
    expect(new Set(models.map(m => m.key)).size).toBe(2)
    models[1]!.remark = '  '
    expect(toItemInputs(models)).toEqual([
      { targetId: 'r1', groupNo: 0, remark: '财务' },
      { targetId: 'r2', groupNo: 1, remark: null },
    ])
  })

  it('提交前找出第一处规则项问题：太少、没选目标、目标重复', () => {
    const t = (key: string, values?: Record<string, unknown>) => `${key}:${JSON.stringify(values ?? {})}`
    const first = { ...emptyItem(), targetId: 'r1' }
    expect(findItemProblem('Ssd', [first])).toEqual({ problem: 'too_few', index: 2 })
    expect(findItemProblem('Cardinality', [first])).toBeNull()
    const blank = emptyItem()
    expect(findItemProblem('Ssd', [first, blank])).toEqual({ problem: 'no_target', index: 2 })
    const repeat = { ...emptyItem(1), targetId: 'r1' }
    const found = findItemProblem('Dsd', [first, repeat])
    expect(found).toEqual({ problem: 'duplicate', index: 2 })
    expect(itemProblemMessage(t, found!)).toBe('access.constraint_rules.warn_item_duplicate:{"index":2}')
    expect(itemProblemMessage(t, { problem: 'too_few', index: 2 })).toBe('access.constraint_rules.warn_items_too_few:{"min":2}')
  })

  it('拒绝与需审批同为拦截色，警告为警示色，只记录为中性', () => {
    expect([actionTone('Deny'), actionTone('RequireApproval'), actionTone('Warning'), actionTone('Log')]).toEqual(['danger', 'danger', 'warning', 'neutral'])
  })
})

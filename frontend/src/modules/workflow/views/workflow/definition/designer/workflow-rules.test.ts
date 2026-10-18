/**
 * 设计器上 BasicApp 自己的校验规则的单元测试。
 *
 * 职责边界：只测 workflow-rules.ts——BasicApp 注册的受理人类型参数要填全、关联审批业务时的结束与终止节点。
 * 预设与通用校验的规则由流程设计器仓库的测试覆盖。
 */
import type { FlowDocument, FlowNode } from '~/designers/flow'
import { describe, expect, it } from 'vitest'
import { createWorkflowRules } from './workflow-rules'

function node(id: string, type: string, properties?: Record<string, unknown>): FlowNode {
  return { id, type, x: 0, y: 0, width: 160, height: 48, ...(properties ? { data: { properties } } : {}) } as FlowNode
}

function documentOf(nodes: FlowNode[], code = 'leave'): FlowDocument {
  return { version: 1, nodes, edges: [], data: { code, name: '请假' } } as FlowDocument
}

function run(document: FlowDocument, outcomes?: readonly string[]) {
  const rules = createWorkflowRules({ businessOutcomes: code => (code === 'leave' ? outcomes : undefined) })
  return rules.flatMap(rule => rule(document, {} as never)).map(issue => ({ code: issue.code, id: issue.target?.id, severity: issue.severity }))
}

describe('受理人规则参数要填全', () => {
  it('应用注册的类型没填取值时报错，并落在那个人工任务上', () => {
    const issues = run(documentOf([
      node('a', 'UserTask', { AssigneeRules: [{ type: 'Role', values: [] }] }),
      node('b', 'UserTask', { CcRules: [{ type: 'StarterLeader', values: ['11'] }] }),
      node('c', 'UserTask', { FallbackAssigneeRules: [{ type: 'Position', values: ['pm'] }] }),
    ]))
    expect(issues).toEqual([
      { code: 'workflow.assignee-incomplete', id: 'a', severity: 'error' },
      { code: 'workflow.assignee-incomplete', id: 'b', severity: 'error' },
    ])
  })

  it('框架内置的 User / Expression 交给预设检查，这里不重复报', () => {
    const issues = run(documentOf([node('a', 'UserTask', { AssigneeRules: [{ type: 'User', values: [] }, { type: 'Expression', values: [] }] })]))
    expect(issues).toEqual([])
  })
})

describe('关联审批业务时的结束与终止节点', () => {
  it('结束节点没声明结果或结果不被业务认可报警告，终止节点报警告', () => {
    const issues = run(documentOf([
      node('ok', 'End', { Outcome: 'approved' }),
      node('missing', 'End'),
      node('unknown', 'End', { Outcome: 'archived' }),
      node('stop', 'Terminate'),
    ]), ['approved', 'rejected'])
    expect(issues).toEqual([
      { code: 'workflow.business-end-outcome', id: 'missing', severity: 'warning' },
      { code: 'workflow.business-end-outcome', id: 'unknown', severity: 'warning' },
      { code: 'workflow.business-terminate', id: 'stop', severity: 'warning' },
    ])
  })

  it('没有关联业务的流程不检查结束结果', () => {
    expect(run(documentOf([node('missing', 'End'), node('stop', 'Terminate')], 'generic'), ['approved'])).toEqual([])
  })
})

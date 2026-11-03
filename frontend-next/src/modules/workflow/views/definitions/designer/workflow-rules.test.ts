/**
 * 设计器上 BasicApp 自己的校验规则的单元测试
 *
 * 职责边界：只测 workflow-rules.ts——BasicApp 注册的受理人类型参数要填全、关联审批业务时的结束与终止节点、
 * 发布失败时后端给的问题落到节点与连线上
 * 预设与通用校验的规则由流程设计器仓库的测试覆盖
 */
import type { ServerDiagnostic } from './server-diagnostics'
import type { FlowDocument, FlowEdge, FlowNode } from '~/designers/flow'
import { describe, expect, it } from 'vitest'
import { createWorkflowRules, SERVER_DIAGNOSTIC_CODE } from './workflow-rules'

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

describe('发布失败时后端给的问题', () => {
  it('标在对应的节点与连线上，定义级问题不带对象，对象已删掉的不再报', () => {
    const diagnostics: ServerDiagnostic[] = [
      { target: { kind: 'node', id: 'b' }, message: '用户任务没有受理人。' },
      { target: { kind: 'edge', id: 't1' }, message: '条件不是布尔值。' },
      { target: { kind: 'node', id: 'gone' }, message: '已删掉的节点' },
      { message: '工作流定义没有开始节点。' },
    ]
    const document: FlowDocument = { ...documentOf([node('a', 'Start'), node('b', 'UserTask')], 'generic'), edges: [{ id: 't1' } as FlowEdge] }
    const rules = createWorkflowRules({ businessOutcomes: () => undefined, serverDiagnostics: () => diagnostics })
    const issues = rules.flatMap(rule => rule(document, {} as never)).filter(issue => issue.code === SERVER_DIAGNOSTIC_CODE)
    expect(issues.map(issue => ({ path: issue.path, target: issue.target, severity: issue.severity, message: issue.params?.message }))).toEqual([
      { path: ['nodes', 1], target: { kind: 'node', id: 'b' }, severity: 'error', message: '用户任务没有受理人。' },
      { path: ['edges', 0], target: { kind: 'edge', id: 't1' }, severity: 'error', message: '条件不是布尔值。' },
      { path: [], target: undefined, severity: 'error', message: '工作流定义没有开始节点。' },
    ])
  })
})

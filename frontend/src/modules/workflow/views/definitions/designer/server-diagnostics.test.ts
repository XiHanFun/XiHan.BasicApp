import { describe, expect, it } from 'vitest'
import { serverDiagnostics } from './server-diagnostics'

const DEFINITION = JSON.stringify({
  name: 'tests',
  nodes: [
    { id: 'start', activityType: 'Start' },
    { id: 'approve', activityType: 'UserTask' },
    { id: 'end', activityType: 'End' },
  ],
  transitions: [
    { id: 't1', sourceNodeId: 'start', targetNodeId: 'approve' },
    { id: 't2', sourceNodeId: 'approve', targetNodeId: 'end' },
  ],
})

describe('发布失败的字段级问题落到设计器上', () => {
  it('按下标找到节点与连线，定义级问题不带对象', () => {
    expect(serverDiagnostics(DEFINITION, {
      'nodes[1].properties.AssigneeRules': ['用户任务没有受理人。'],
      'transitions[1].condition': ['表达式第 3 个字符不认识。', '条件不是布尔值。'],
      'nodes': ['工作流定义没有开始节点。'],
    })).toEqual([
      {
        target: { kind: 'node', id: 'approve' },
        message: '用户任务没有受理人。',
      },
      {
        target: { kind: 'edge', id: 't2' },
        message: '表达式第 3 个字符不认识。',
      },
      {
        target: { kind: 'edge', id: 't2' },
        message: '条件不是布尔值。',
      },
      { message: '工作流定义没有开始节点。' },
    ])
  })

  it('下标越界或定义解析不了时算定义级问题', () => {
    expect(serverDiagnostics(DEFINITION, {
      'nodes[9]': ['越界'],
    })).toEqual([
      { message: '越界' },
    ])
    expect(serverDiagnostics('not json', {
      'nodes[0]': ['坏的'],
    })).toEqual([
      { message: '坏的' },
    ])
  })
})

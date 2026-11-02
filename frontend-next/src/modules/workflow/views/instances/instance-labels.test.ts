/**
 * 流程实例页展示规则（instance-labels.ts）的单元测试。
 *
 * 锁定：结果色调与标准结果（自定义结果原样显示）；运行轨迹取每个节点最近一次执行的状态、等待中的补 waiting；
 * 执行历史的节点名从实例所用的定义文档里取。
 */
import { describe, expect, it } from 'vitest'
import { graphStatuses, instanceStatusTone, nodeStatusTone, nodeTitles, outcomeTone, standardOutcome } from './instance-labels'

describe('状态与结果', () => {
  it('标准结果有文案，业务自定义结果与空值没有', () => {
    expect(standardOutcome('approved')).toBe('approved')
    expect(standardOutcome('timeout')).toBe('timeout')
    expect(standardOutcome('returned')).toBeNull()
    expect(standardOutcome(null)).toBeNull()
  })

  it('结果色调：通过绿、驳回红、超时黄、其余中性', () => {
    expect(outcomeTone('approved')).toBe('success')
    expect(outcomeTone('rejected')).toBe('danger')
    expect(outcomeTone('timeout')).toBe('warning')
    expect(outcomeTone('returned')).toBe('neutral')
  })

  it('实例与节点状态色调', () => {
    expect(instanceStatusTone('Faulted')).toBe('danger')
    expect(instanceStatusTone('Terminated')).toBe('neutral')
    expect(nodeStatusTone('Suspended')).toBe('warning')
    expect(nodeStatusTone('Compensated')).toBe('neutral')
  })
})

describe('运行轨迹', () => {
  it('同一节点取最近一次执行的状态，等待中且没执行记录的节点补 waiting', () => {
    expect(graphStatuses(
      [
        { nodeId: 'call', status: 'Faulted' },
        { nodeId: 'start', status: 'Completed' },
        { nodeId: 'call', status: 'Completed' },
        { nodeId: 'approve', status: 'Suspended' },
      ],
      [{ nodeId: 'approve' }, { nodeId: 'timer' }],
    )).toEqual({ call: 'completed', start: 'completed', approve: 'waiting', timer: 'waiting' })
  })

  it('节点名取定义文档里的显示名；文档读不出来时为空表', () => {
    const document = JSON.stringify({ name: 'leave', nodes: [{ id: 'start', activityType: 'Start', displayName: '开始' }, { id: 'end', activityType: 'End' }] })
    expect(nodeTitles(document)).toEqual({ start: '开始' })
    expect(nodeTitles('{')).toEqual({})
    expect(nodeTitles(null)).toEqual({})
  })
})

import { describe, expect, it } from 'vitest'
import { WorkflowInstanceStatus, WorkflowTaskRecordAction } from '../../../api'
import { handleOutcomes, instanceStatusLabelKey, instanceStatusTone, isActiveInstance, outcomeLabelKey, outcomeTone, recordActionLabelKey } from './approval-labels'

describe('审批中心展示规则', () => {
  it('未配置白名单时办理结果为同意与驳回', () => {
    expect(handleOutcomes([])).toEqual(['approved', 'rejected'])
    expect(handleOutcomes(['', '  '])).toEqual(['approved', 'rejected'])
  })

  it('白名单按原样给出，去掉超时与重复项', () => {
    expect(handleOutcomes(['approved', 'returned', 'timeout', 'approved'])).toEqual(['approved', 'returned'])
  })

  it('标准结果有文案键，业务自定义结果没有', () => {
    expect(outcomeLabelKey('approved')).toBe('workflow.approval.outcome_approved')
    expect(outcomeLabelKey('timeout')).toBe('workflow.approval.outcome_timeout')
    expect(outcomeLabelKey('returned')).toBeNull()
    expect(outcomeLabelKey(null)).toBeNull()
  })

  it('结果色调：通过绿、驳回红、超时黄、其余中性', () => {
    expect(outcomeTone('approved')).toBe('success')
    expect(outcomeTone('rejected')).toBe('danger')
    expect(outcomeTone('timeout')).toBe('warning')
    expect(outcomeTone('returned')).toBe('neutral')
  })

  it('实例状态文案键复用流程实例页', () => {
    expect(instanceStatusLabelKey(WorkflowInstanceStatus.Running)).toBe('workflow.instance.status_running')
    expect(instanceStatusTone(WorkflowInstanceStatus.Faulted)).toBe('danger')
  })

  it('运行、挂起与故障都算进行中（发起人仍可撤回）', () => {
    expect(isActiveInstance(WorkflowInstanceStatus.Running)).toBe(true)
    expect(isActiveInstance(WorkflowInstanceStatus.Suspended)).toBe(true)
    expect(isActiveInstance(WorkflowInstanceStatus.Faulted)).toBe(true)
    expect(isActiveInstance(WorkflowInstanceStatus.Completed)).toBe(false)
    expect(isActiveInstance(WorkflowInstanceStatus.Canceled)).toBe(false)
  })

  it('每种审批记录动作都有文案键', () => {
    const keys = Object.values(WorkflowTaskRecordAction).map(recordActionLabelKey)
    expect(new Set(keys).size).toBe(keys.length)
  })
})

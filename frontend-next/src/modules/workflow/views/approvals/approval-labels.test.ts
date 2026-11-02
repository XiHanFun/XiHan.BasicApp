/**
 * 审批中心展示规则（approval-labels.ts）的单元测试（照 4.x 的 approval-labels.test）。
 *
 * 锁定：未配置白名单时办理结果为同意与驳回，白名单去掉超时与重复项；标准结果有文案、业务自定义结果原样显示；
 * 实例状态文案与流程实例页同一组键；运行、挂起与故障算进行中；每种审批记录动作都有各自的文案。
 */
import { describe, expect, it } from 'vitest'
import { WorkflowInstanceStatus, WorkflowTaskRecordAction } from '~/contracts'
import { handleOutcomes, instanceStatusLabel, isActiveInstance, outcomeLabel, recordActionLabel } from './approval-labels'

/** 原样返回文案键，断言文案选择而不依赖语言包 */
const t = (key: string) => key

describe('审批中心展示规则', () => {
  it('未配置白名单时办理结果为同意与驳回', () => {
    expect(handleOutcomes([])).toEqual(['approved', 'rejected'])
    expect(handleOutcomes(['', '  '])).toEqual(['approved', 'rejected'])
  })

  it('白名单按原样给出，去掉超时与重复项', () => {
    expect(handleOutcomes(['approved', 'returned', 'timeout', 'approved'])).toEqual(['approved', 'returned'])
  })

  it('标准结果有文案，业务自定义结果原样显示，空值为空串', () => {
    expect(outcomeLabel(t, 'approved')).toBe('workflow.instances.outcome_approved')
    expect(outcomeLabel(t, 'timeout')).toBe('workflow.instances.outcome_timeout')
    expect(outcomeLabel(t, 'returned')).toBe('returned')
    expect(outcomeLabel(t, null)).toBe('')
  })

  it('实例状态文案复用流程实例页，每种状态各有一条', () => {
    expect(instanceStatusLabel(t, 'Running')).toBe('workflow.instances.status_running')
    const labels = WorkflowInstanceStatus.map(status => instanceStatusLabel(t, status))
    expect(new Set(labels).size).toBe(labels.length)
  })

  it('运行、挂起与故障都算进行中（发起人仍可撤回）', () => {
    expect(isActiveInstance('Running')).toBe(true)
    expect(isActiveInstance('Suspended')).toBe(true)
    expect(isActiveInstance('Faulted')).toBe(true)
    expect(isActiveInstance('Completed')).toBe(false)
    expect(isActiveInstance('Canceled')).toBe(false)
  })

  it('每种审批记录动作都有文案键', () => {
    const labels = WorkflowTaskRecordAction.map(action => recordActionLabel(t, action))
    expect(new Set(labels).size).toBe(labels.length)
  })
})

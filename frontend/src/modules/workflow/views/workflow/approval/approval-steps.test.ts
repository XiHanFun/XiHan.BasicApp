import type { WorkflowApprovalTimelineItemDto } from '../../../api'
import { describe, expect, it } from 'vitest'
import { WorkflowApprovalTimelineKind, WorkflowInstanceStatus } from '../../../api'
import { buildApprovalSteps } from './approval-steps'

/** 把键与参数原样拼出来，断言文案选择而不依赖语言包 */
function t(key: string, named?: Record<string, unknown>) {
  return named ? `${key}${JSON.stringify(named)}` : key
}

function item(kind: WorkflowApprovalTimelineKind, patch: Partial<WorkflowApprovalTimelineItemDto> = {}): WorkflowApprovalTimelineItemDto {
  return { kind, targetUserNames: [], time: '2026-10-14 14:34:57', ...patch }
}

const started = item(WorkflowApprovalTimelineKind.Started, { userName: '李管理' })

describe('审批流程步骤条', () => {
  it('进行中：待办那一步是当前步，末尾补「结束」占位', () => {
    const { steps, value } = buildApprovalSteps(t, [
      started,
      item(WorkflowApprovalTimelineKind.Pending, { nodeId: 'admin', nodeName: '管理员审批', userName: '张总' }),
    ])

    expect(steps.map(step => step.title)).toEqual(['workflow.approval.step_started', '管理员审批', 'workflow.approval.step_end'])
    expect(value).toBe(1)
    expect(steps[0]?.lines).toEqual(['李管理'])
    expect(steps[1]?.lines).toEqual(['workflow.approval.tl_pending{"user":"张总"}'])
    expect(steps[2]?.lines).toEqual([])
  })

  it('同一节点的多位待办人合成一步', () => {
    const { steps } = buildApprovalSteps(t, [
      started,
      item(WorkflowApprovalTimelineKind.Pending, { nodeId: 'admin', nodeName: '管理员审批', userName: '张总' }),
      item(WorkflowApprovalTimelineKind.Pending, { nodeId: 'admin', nodeName: '管理员审批', userName: '王主管' }),
    ])

    expect(steps).toHaveLength(3)
    expect(steps[1]?.lines).toEqual(['workflow.approval.tl_pending{"user":"张总、王主管"}'])
  })

  it('通过后全部走完，办理与结束都标成功色，意见单独一行', () => {
    const { steps, value } = buildApprovalSteps(t, [
      started,
      item(WorkflowApprovalTimelineKind.Completed, { nodeId: 'admin', nodeName: '管理员审批', userName: '张总', outcome: 'approved', comment: '同意' }),
      item(WorkflowApprovalTimelineKind.Finished, { instanceStatus: WorkflowInstanceStatus.Completed, outcome: 'approved' }),
    ])

    expect(value).toBe(steps.length)
    expect(steps.map(step => step.tone)).toEqual([undefined, 'success', 'success'])
    expect(steps[1]?.lines).toEqual(['workflow.approval.tl_completed{"user":"张总","outcome":"workflow.approval.outcome_approved"}', '同意'])
    expect(steps[2]?.lines).toEqual(['workflow.approval.outcome_approved'])
  })

  it('驳回标危险色，撤回与终止标中性色', () => {
    const rejected = buildApprovalSteps(t, [
      started,
      item(WorkflowApprovalTimelineKind.Completed, { nodeName: '管理员审批', userName: '张总', outcome: 'rejected' }),
      item(WorkflowApprovalTimelineKind.Finished, { instanceStatus: WorkflowInstanceStatus.Completed, outcome: 'rejected' }),
    ])
    expect(rejected.steps.map(step => step.tone)).toEqual([undefined, 'danger', 'danger'])

    const canceled = buildApprovalSteps(t, [
      started,
      item(WorkflowApprovalTimelineKind.Finished, { instanceStatus: WorkflowInstanceStatus.Canceled }),
    ])
    expect(canceled.value).toBe(2)
    expect(canceled.steps[1]).toMatchObject({ tone: 'neutral', lines: ['workflow.instance.status_canceled'] })
  })

  it('同一节点连续抄送合成一步', () => {
    const { steps } = buildApprovalSteps(t, [
      started,
      item(WorkflowApprovalTimelineKind.CarbonCopied, { nodeId: 'admin', nodeName: '管理员审批', userName: '孙审计' }),
      item(WorkflowApprovalTimelineKind.CarbonCopied, { nodeId: 'admin', nodeName: '管理员审批', userName: '钱人事' }),
    ])

    expect(steps).toHaveLength(3)
    expect(steps[1]).toMatchObject({ tone: 'neutral', lines: ['workflow.approval.tl_cc{"user":"孙审计、钱人事"}'] })
  })

  it('进行中却没有待办（自动推进或故障）时当前步落在「结束」上', () => {
    const { steps, value } = buildApprovalSteps(t, [started])

    expect(value).toBe(steps.length - 1)
    expect(steps.at(-1)?.key).toBe('end')
  })
})

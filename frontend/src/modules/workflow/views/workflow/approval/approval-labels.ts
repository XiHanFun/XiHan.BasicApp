/**
 * 审批中心的状态、结果与动作展示规则（纯函数，页面与抽屉共用）。
 */
import type { Tone } from '@xihan-ui/core'
import { WorkflowInstanceStatus, WorkflowTaskRecordAction } from '../../../api'

/** 标准办理结果（与后端 WorkflowUserTaskOutcomes 一致） */
export const STANDARD_OUTCOMES = ['approved', 'rejected', 'timeout'] as const

/** 实例状态色调 */
export function instanceStatusTone(status: WorkflowInstanceStatus): Tone {
  switch (status) {
    case WorkflowInstanceStatus.Completed:
      return 'success'
    case WorkflowInstanceStatus.Faulted:
      return 'danger'
    case WorkflowInstanceStatus.Running:
      return 'info'
    case WorkflowInstanceStatus.Suspended:
      return 'warning'
    default:
      return 'neutral'
  }
}

/** 实例状态文案键（workflow.instance 下） */
export function instanceStatusLabelKey(status: WorkflowInstanceStatus): string {
  return `workflow.instance.status_${status.toLowerCase()}`
}

/** 结果色调：通过绿、驳回红、其余中性 */
export function outcomeTone(outcome?: string | null): Tone {
  if (outcome === 'approved')
    return 'success'
  if (outcome === 'rejected')
    return 'danger'
  if (outcome === 'timeout')
    return 'warning'
  return 'neutral'
}

/** 结果文案键；业务自定义结果没有文案键，调用方原样显示 */
export function outcomeLabelKey(outcome?: string | null): string | null {
  return outcome && (STANDARD_OUTCOMES as readonly string[]).includes(outcome)
    ? `workflow.approval.outcome_${outcome}`
    : null
}

/** 审批记录动作文案键 */
export function recordActionLabelKey(action: WorkflowTaskRecordAction): string {
  switch (action) {
    case WorkflowTaskRecordAction.Transferred:
      return 'workflow.approval.action_transferred'
    case WorkflowTaskRecordAction.AddedAssignees:
      return 'workflow.approval.action_added'
    case WorkflowTaskRecordAction.CarbonCopied:
      return 'workflow.approval.action_cc'
    default:
      return 'workflow.approval.action_completed'
  }
}

/** 流程是否仍在进行（运行、挂起或故障待处理） */
export function isActiveInstance(status: WorkflowInstanceStatus): boolean {
  return status === WorkflowInstanceStatus.Running
    || status === WorkflowInstanceStatus.Suspended
    || status === WorkflowInstanceStatus.Faulted
}

/**
 * 待办可选的办理结果：未配置白名单时为同意/驳回；配置了白名单按白名单（去掉超时，超时只由引擎产生）。
 */
export function handleOutcomes(allowedOutcomes: readonly string[]): string[] {
  const outcomes = allowedOutcomes.filter(outcome => outcome.trim() !== '' && outcome !== 'timeout')
  return outcomes.length > 0 ? [...new Set(outcomes)] : ['approved', 'rejected']
}

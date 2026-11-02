/**
 * 审批中心的状态、结果与动作展示规则（纯函数，页面、抽屉与步骤条共用）。
 *
 * 照 4.x 的 approval-labels；6.0 没有枚举元数据接口，取值是契约字面量，文案键按字面量逐个静态写出，供 i18n 门禁核对。
 * 实例状态与标准结果的文案、色调与流程实例页同一处（workflow.instances 下、instance-labels），这里不另起一套。
 */
import type { WorkflowInstanceStatus, WorkflowTaskRecordAction } from '~/contracts'
import { standardOutcome } from '../instances/instance-labels'

export type Translate = (key: string, named?: Record<string, unknown>) => string

/** 实例状态文案（与流程实例页同一组键） */
export function instanceStatusLabel(t: Translate, status: WorkflowInstanceStatus): string {
  switch (status) {
    case 'Running': return t('workflow.instances.status_running')
    case 'Suspended': return t('workflow.instances.status_suspended')
    case 'Completed': return t('workflow.instances.status_completed')
    case 'Canceled': return t('workflow.instances.status_canceled')
    case 'Faulted': return t('workflow.instances.status_faulted')
    default: return t('workflow.instances.status_terminated')
  }
}

/** 结果文案：标准结果有文案，业务自定义结果原样显示，空值为空串 */
export function outcomeLabel(t: Translate, outcome?: string | null): string {
  switch (standardOutcome(outcome)) {
    case 'approved': return t('workflow.instances.outcome_approved')
    case 'rejected': return t('workflow.instances.outcome_rejected')
    case 'timeout': return t('workflow.instances.outcome_timeout')
    default: return outcome ?? ''
  }
}

/** 审批记录动作文案 */
export function recordActionLabel(t: Translate, action: WorkflowTaskRecordAction): string {
  switch (action) {
    case 'Transferred': return t('workflow.approvals.action_transferred')
    case 'AddedAssignees': return t('workflow.approvals.action_added')
    case 'CarbonCopied': return t('workflow.approvals.action_cc')
    default: return t('workflow.approvals.action_completed')
  }
}

/** 流程是否仍在进行（运行、挂起或故障待处理） */
export function isActiveInstance(status: WorkflowInstanceStatus): boolean {
  return status === 'Running' || status === 'Suspended' || status === 'Faulted'
}

/**
 * 待办可选的办理结果：未配置白名单时为同意/驳回；配置了白名单按白名单（去掉超时，超时只由引擎产生）。
 * @param allowedOutcomes 节点允许的结果（后端 task.allowedOutcomes）。
 */
export function handleOutcomes(allowedOutcomes: readonly string[]): string[] {
  const outcomes = allowedOutcomes.filter(outcome => outcome.trim() !== '' && outcome !== 'timeout')
  return outcomes.length > 0 ? [...new Set(outcomes)] : ['approved', 'rejected']
}

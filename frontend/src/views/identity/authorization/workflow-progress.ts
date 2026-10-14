/**
 * 权限申请的审批进度展示（授权页与权限中心共用）。
 * 进度来自工作流模块；模块未启用或申请尚未发起流程时 progress 为空。
 */
import type { Tone } from '@xihan-ui/core'
import type { WorkflowProgressDto } from '@/api'
import { WorkflowProgressStatus } from '@/api'

type Translate = (key: string, named?: Record<string, unknown>) => string

/** 进度色调：进行中蓝、通过绿、驳回或故障红、挂起黄，其余中性 */
export function workflowProgressTone(progress?: WorkflowProgressDto | null): Tone {
  if (!progress)
    return 'neutral'
  switch (progress.status) {
    case WorkflowProgressStatus.Running:
      return 'info'
    case WorkflowProgressStatus.Suspended:
      return 'warning'
    case WorkflowProgressStatus.Faulted:
      return 'danger'
    case WorkflowProgressStatus.Completed:
      return progress.outcome === 'rejected' ? 'danger' : 'success'
    default:
      return 'neutral'
  }
}

/** 进度文案：进行中写出停留节点与待办理人，结束后写出结果 */
export function workflowProgressText(t: Translate, progress?: WorkflowProgressDto | null): string {
  if (!progress)
    return t('identity.authorization.progress_none')
  switch (progress.status) {
    case WorkflowProgressStatus.Running: {
      const nodes = progress.pendingNodeNames.join('、')
      const assignees = progress.pendingAssigneeNames.join('、')
      if (!nodes)
        return t('identity.authorization.progress_running')
      return assignees
        ? t('identity.authorization.progress_pending_with', { nodes, assignees })
        : t('identity.authorization.progress_pending', { nodes })
    }
    case WorkflowProgressStatus.Suspended:
      return t('identity.authorization.progress_suspended')
    case WorkflowProgressStatus.Faulted:
      return t('identity.authorization.progress_faulted')
    case WorkflowProgressStatus.Canceled:
      return t('identity.authorization.progress_canceled')
    case WorkflowProgressStatus.Terminated:
      return t('identity.authorization.progress_terminated')
    default:
      if (progress.outcome === 'approved')
        return t('identity.authorization.progress_approved')
      if (progress.outcome === 'rejected')
        return t('identity.authorization.progress_rejected')
      return progress.outcome || t('identity.authorization.progress_completed')
  }
}

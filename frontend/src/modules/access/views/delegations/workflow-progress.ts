/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 授权申请的审批进度展示
 * 进度来自 Workflow 模块：模块关掉时 approvalEnabled 为假、进度为空，标「审批未启用」（方案 W1）；
 * 申请还没发起过流程时进度为空
 */
import type { Tone } from '@xihan-ui/core'
import type { components } from '~/contracts'

export type WorkflowProgress = components['schemas']['PermissionRequestProgress']

type Translate = (key: string, named?: Record<string, unknown>) => string

/** 进度色调：进行中蓝、通过绿、驳回或故障红、挂起黄，其余中性 */
export function workflowProgressTone(progress?: WorkflowProgress | null): Tone {
  if (!progress)
    return 'neutral'
  switch (progress.status) {
    case 'Running':
      return 'info'
    case 'Suspended':
      return 'warning'
    case 'Faulted':
      return 'danger'
    case 'Completed':
      return progress.outcome === 'rejected'
        ? 'danger'
        : 'success'
    default:
      return 'neutral'
  }
}

/** 进度文案：审批未启用先说明；进行中写出停留节点与待办理人，结束后写出结果 */
export function workflowProgressText(t: Translate, progress?: WorkflowProgress | null, approvalEnabled = true): string {
  if (!approvalEnabled)
    return t('access.permission_requests.progress_disabled')
  if (!progress)
    return t('access.permission_requests.progress_none')
  switch (progress.status) {
    case 'Running': {
      const nodes = progress.pendingNodeNames.join('、')
      const assignees = progress.pendingAssignees.map(assignee => assignee.displayName).join('、')
      if (!nodes)
        return t('access.permission_requests.progress_running')
      return assignees
        ? t('access.permission_requests.progress_pending_with', { nodes, assignees })
        : t('access.permission_requests.progress_pending', { nodes })
    }
    case 'Suspended':
      return t('access.permission_requests.progress_suspended')
    case 'Faulted':
      return t('access.permission_requests.progress_faulted')
    case 'Canceled':
      return t('access.permission_requests.progress_canceled')
    case 'Terminated':
      return t('access.permission_requests.progress_terminated')
    default:
      if (progress.outcome === 'approved')
        return t('access.permission_requests.progress_approved')
      if (progress.outcome === 'rejected')
        return t('access.permission_requests.progress_rejected')
      return progress.outcome || t('access.permission_requests.progress_completed')
  }
}

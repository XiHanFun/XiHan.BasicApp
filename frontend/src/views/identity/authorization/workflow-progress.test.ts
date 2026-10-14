import type { WorkflowProgressDto } from '@/api'
import { describe, expect, it } from 'vitest'
import { WorkflowProgressStatus } from '@/api'
import { workflowProgressText, workflowProgressTone } from './workflow-progress'

/** 把键与参数原样拼出来，断言文案选择而不依赖语言包 */
function t(key: string, named?: Record<string, unknown>) {
  return named ? `${key}${JSON.stringify(named)}` : key
}

function progress(patch: Partial<WorkflowProgressDto>): WorkflowProgressDto {
  return {
    instanceId: '1',
    status: WorkflowProgressStatus.Running,
    pendingNodeNames: [],
    pendingAssigneeNames: [],
    ...patch,
  }
}

describe('权限申请的审批进度', () => {
  it('没有流程时显示未发起', () => {
    expect(workflowProgressText(t, null)).toBe('identity.authorization.progress_none')
    expect(workflowProgressTone(null)).toBe('neutral')
  })

  it('进行中写出停留节点与待办理人', () => {
    expect(workflowProgressText(t, progress({ pendingNodeNames: ['管理员审批'], pendingAssigneeNames: ['张三', '李四'] })))
      .toBe('identity.authorization.progress_pending_with{"nodes":"管理员审批","assignees":"张三、李四"}')
    expect(workflowProgressText(t, progress({ pendingNodeNames: ['管理员审批'] })))
      .toBe('identity.authorization.progress_pending{"nodes":"管理员审批"}')
    expect(workflowProgressText(t, progress({}))).toBe('identity.authorization.progress_running')
    expect(workflowProgressTone(progress({}))).toBe('info')
  })

  it('完成后按结果显示，驳回标红', () => {
    const approved = progress({ status: WorkflowProgressStatus.Completed, outcome: 'approved' })
    const rejected = progress({ status: WorkflowProgressStatus.Completed, outcome: 'rejected' })
    expect(workflowProgressText(t, approved)).toBe('identity.authorization.progress_approved')
    expect(workflowProgressTone(approved)).toBe('success')
    expect(workflowProgressText(t, rejected)).toBe('identity.authorization.progress_rejected')
    expect(workflowProgressTone(rejected)).toBe('danger')
    expect(workflowProgressText(t, progress({ status: WorkflowProgressStatus.Completed, outcome: 'returned' }))).toBe('returned')
    expect(workflowProgressText(t, progress({ status: WorkflowProgressStatus.Completed }))).toBe('identity.authorization.progress_completed')
  })

  it('撤回、终止、挂起与故障各有文案', () => {
    expect(workflowProgressText(t, progress({ status: WorkflowProgressStatus.Canceled }))).toBe('identity.authorization.progress_canceled')
    expect(workflowProgressText(t, progress({ status: WorkflowProgressStatus.Terminated }))).toBe('identity.authorization.progress_terminated')
    expect(workflowProgressText(t, progress({ status: WorkflowProgressStatus.Suspended }))).toBe('identity.authorization.progress_suspended')
    expect(workflowProgressText(t, progress({ status: WorkflowProgressStatus.Faulted }))).toBe('identity.authorization.progress_faulted')
    expect(workflowProgressTone(progress({ status: WorkflowProgressStatus.Faulted }))).toBe('danger')
  })
})

import type { WorkflowProgress } from './workflow-progress'
import { describe, expect, it } from 'vitest'
import { workflowProgressText, workflowProgressTone } from './workflow-progress'

/** 把键与参数原样拼出来，断言文案选择而不依赖语言包 */
function t(key: string, named?: Record<string, unknown>) {
  return named ? `${key}${JSON.stringify(named)}` : key
}

function progress(patch: Partial<WorkflowProgress>): WorkflowProgress {
  return {
    instanceId: 'i-1',
    status: 'Running',
    pendingNodeNames: [],
    pendingAssignees: [],
    startedAt: '2026-11-01T00:00:00Z',
    ...patch,
  }
}

describe('授权申请的审批进度', () => {
  it('没有流程时显示未发起，审批未启用时先说明未启用', () => {
    expect(workflowProgressText(t, null)).toBe('access.permission_requests.progress_none')
    expect(workflowProgressTone(null)).toBe('neutral')
    expect(workflowProgressText(t, null, false)).toBe('access.permission_requests.progress_disabled')
    expect(workflowProgressText(t, progress({}), false)).toBe('access.permission_requests.progress_disabled')
  })

  it('进行中写出停留节点与待办理人', () => {
    const assignees = [{ userId: 'u-1', displayName: '张三' }, { userId: 'u-2', displayName: '李四' }]
    expect(workflowProgressText(t, progress({ pendingNodeNames: ['管理员审批'], pendingAssignees: assignees })))
      .toBe('access.permission_requests.progress_pending_with{"nodes":"管理员审批","assignees":"张三、李四"}')
    expect(workflowProgressText(t, progress({ pendingNodeNames: ['管理员审批'] })))
      .toBe('access.permission_requests.progress_pending{"nodes":"管理员审批"}')
    expect(workflowProgressText(t, progress({}))).toBe('access.permission_requests.progress_running')
    expect(workflowProgressTone(progress({}))).toBe('info')
  })

  it('完成后按结果显示，驳回标红', () => {
    const approved = progress({ status: 'Completed', outcome: 'approved' })
    const rejected = progress({ status: 'Completed', outcome: 'rejected' })
    expect(workflowProgressText(t, approved)).toBe('access.permission_requests.progress_approved')
    expect(workflowProgressTone(approved)).toBe('success')
    expect(workflowProgressText(t, rejected)).toBe('access.permission_requests.progress_rejected')
    expect(workflowProgressTone(rejected)).toBe('danger')
    expect(workflowProgressText(t, progress({ status: 'Completed', outcome: 'returned' }))).toBe('returned')
    expect(workflowProgressText(t, progress({ status: 'Completed' }))).toBe('access.permission_requests.progress_completed')
  })

  it('撤回、终止、挂起与故障各有文案', () => {
    expect(workflowProgressText(t, progress({ status: 'Canceled' }))).toBe('access.permission_requests.progress_canceled')
    expect(workflowProgressText(t, progress({ status: 'Terminated' }))).toBe('access.permission_requests.progress_terminated')
    expect(workflowProgressText(t, progress({ status: 'Suspended' }))).toBe('access.permission_requests.progress_suspended')
    expect(workflowProgressTone(progress({ status: 'Suspended' }))).toBe('warning')
    expect(workflowProgressText(t, progress({ status: 'Faulted' }))).toBe('access.permission_requests.progress_faulted')
    expect(workflowProgressTone(progress({ status: 'Faulted' }))).toBe('danger')
  })
})

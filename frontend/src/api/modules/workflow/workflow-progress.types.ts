import type { ApiId, DateTimeString } from '../../types'

/**
 * 业务单据所挂流程实例的状态（与后端 JsonStringEnumConverter 序列化值一致）。
 * 工作流是可选模块，核心 API 不依赖它的前端模块，这里按网络契约单独声明。
 */
export enum WorkflowProgressStatus {
  Running = 'Running',
  Suspended = 'Suspended',
  Completed = 'Completed',
  Canceled = 'Canceled',
  Faulted = 'Faulted',
  Terminated = 'Terminated',
}

/** 业务单据的审批进度（工作流模块未启用或单据未发起流程时为空） */
export interface WorkflowProgressDto {
  instanceId: ApiId
  status: WorkflowProgressStatus
  /** 流程结果（approved / rejected 或业务自定义结果） */
  outcome?: string | null
  /** 当前停留的审批节点 */
  pendingNodeNames: string[]
  /** 当前待办理人 */
  pendingAssigneeNames: string[]
  faultMessage?: string | null
  startTime?: DateTimeString | null
  endTime?: DateTimeString | null
}

import type { WorkflowInstanceStatus } from './instance.types'
import type { ApiId, BasicDto, DateTimeString, NumericString, PageRequest } from '@/api/types'

/** 审批记录动作（与后端 JsonStringEnumConverter 序列化值一致） */
export enum WorkflowTaskRecordAction {
  Completed = 'Completed',
  Transferred = 'Transferred',
  AddedAssignees = 'AddedAssignees',
  CarbonCopied = 'CarbonCopied',
}

/** 审批轨迹项类型（与后端 JsonStringEnumConverter 序列化值一致） */
export enum WorkflowApprovalTimelineKind {
  Started = 'Started',
  Completed = 'Completed',
  Transferred = 'Transferred',
  AddedAssignees = 'AddedAssignees',
  CarbonCopied = 'CarbonCopied',
  AutoDecided = 'AutoDecided',
  Pending = 'Pending',
  Finished = 'Finished',
}

/** 审批中心分页查询（主体服务端锁定为当前用户；status 只对「我发起的」生效） */
export interface WorkflowApprovalPageQueryDto extends PageRequest {
  keyword?: string | null
  status?: WorkflowInstanceStatus | null
}

/** 审批记录列表项（我的已办 / 抄送我的） */
export interface WorkflowApprovalRecordItemDto extends BasicDto {
  instanceId: NumericString
  taskId: string
  instanceName: string
  definitionCode: string
  nodeId: string
  nodeName: string
  title: string
  correlationId?: string | null
  action: WorkflowTaskRecordAction
  outcome?: string | null
  comment?: string | null
  createdTime: DateTimeString
  businessName?: string | null
  starterId?: string | null
  starterName?: string | null
  instanceStatus: WorkflowInstanceStatus
  instanceOutcome?: string | null
}

/** 我发起的列表项 */
export interface WorkflowApprovalInstanceItemDto extends BasicDto {
  definitionCode: string
  definitionVersion: number
  name: string
  status: WorkflowInstanceStatus
  outcome?: string | null
  correlationId?: string | null
  businessName?: string | null
  creationTime: DateTimeString
  endTime?: DateTimeString | null
  faultMessage?: string | null
  pendingNodeNames: string[]
  pendingAssigneeNames: string[]
}

/** 审批业务单据 */
export interface WorkflowApprovalBusinessDto {
  code: string
  name: string
  /** 单据视图组件（相对 src/views 的组件路径，与菜单 Component 同口径） */
  formComponent: string
  businessId: string
  /** 单据数据 JSON（单据已不存在时为空） */
  dataJson?: string | null
}

/** 当前用户待办理的任务 */
export interface WorkflowApprovalTaskDto {
  taskId: string
  nodeId: string
  nodeName: string
  title: string
  creationTime: DateTimeString
  /** 节点允许的办理结果（为空表示同意/驳回） */
  allowedOutcomes: string[]
  formDataJson: string
}

export interface WorkflowApprovalTimelineItemDto {
  kind: WorkflowApprovalTimelineKind
  nodeId?: string | null
  nodeName?: string | null
  userId?: string | null
  userName?: string | null
  outcome?: string | null
  comment?: string | null
  targetUserNames: string[]
  instanceStatus?: WorkflowInstanceStatus | null
  time?: DateTimeString | null
}

/** 审批详情（单据 + 当前任务 + 审批轨迹） */
export interface WorkflowApprovalDetailDto {
  instanceId: NumericString
  instanceName: string
  definitionCode: string
  definitionName: string
  definitionVersion: number
  status: WorkflowInstanceStatus
  outcome?: string | null
  correlationId?: string | null
  starterId?: string | null
  starterName?: string | null
  creationTime: DateTimeString
  startTime?: DateTimeString | null
  endTime?: DateTimeString | null
  faultMessage?: string | null
  cancellationReason?: string | null
  canWithdraw: boolean
  business?: WorkflowApprovalBusinessDto | null
  task?: WorkflowApprovalTaskDto | null
  timeline: WorkflowApprovalTimelineItemDto[]
}

export interface WorkflowApprovalWithdrawDto {
  instanceId: ApiId
  reason?: string | null
}

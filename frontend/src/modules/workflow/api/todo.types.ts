import type { WorkflowInstanceStatus } from './instance.types'
import type { DateTimeString, PageRequest } from '@/api/types'

export interface WorkflowTodoPageQueryDto extends PageRequest {
  keyword?: string | null
}

export interface WorkflowTodoListItemDto {
  taskId: string
  instanceId: string
  instanceName: string
  definitionCode: string
  nodeId: string
  nodeName: string
  title: string
  correlationId?: string | null
  creationTime: DateTimeString
  starterId?: string | null
  starterName?: string | null
  /** 业务名称（非审批业务流程为空） */
  businessName?: string | null
}

export interface WorkflowTodoCompleteDto {
  taskId: string
  /** 办理结果（approved/rejected 或业务自定义） */
  outcome: string
  comment?: string | null
  variablesJson?: string | null
}

export interface WorkflowTodoCompleteResultDto {
  instanceId: string
  instanceStatus: WorkflowInstanceStatus
  /** 办理后实例故障时的故障信息（审批已记录，后续节点或业务回写出错） */
  faultMessage?: string | null
}

export interface WorkflowTodoTransferDto {
  taskId: string
  targetAssigneeId: string
  comment?: string | null
}

export interface WorkflowTodoAddAssigneesDto {
  taskId: string
  assigneeIds: string[]
  comment?: string | null
}

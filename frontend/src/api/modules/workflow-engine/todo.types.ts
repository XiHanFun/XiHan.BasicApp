import type { DateTimeString, PageRequest } from '../../types'
import type { WorkflowInstanceStatus } from './instance.types'

export interface WorkflowTodoPageQueryDto extends PageRequest {
  keyword?: string | null
}

export interface WorkflowTodoListItemDto {
  taskId: string
  instanceId: string
  instanceName: string
  definitionCode: string
  nodeId: string
  title: string
  correlationId?: string | null
  creationTime: DateTimeString
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

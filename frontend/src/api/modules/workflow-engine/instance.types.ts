import type { ApiId, BasicDto, DateTimeString, NumericString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum WorkflowInstanceStatus {
  Running = 'Running',
  Suspended = 'Suspended',
  Completed = 'Completed',
  Canceled = 'Canceled',
  Faulted = 'Faulted',
  Terminated = 'Terminated',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum WorkflowNodeInstanceStatus {
  Running = 'Running',
  Suspended = 'Suspended',
  Completed = 'Completed',
  Canceled = 'Canceled',
  Faulted = 'Faulted',
  Compensated = 'Compensated',
}

export interface WorkflowInstancePageQueryDto extends PageRequest {
  keyword?: string | null
  status?: WorkflowInstanceStatus | null
  definitionCode?: string | null
  correlationId?: string | null
}

export interface WorkflowInstanceListItemDto extends BasicDto {
  definitionCode: string
  definitionVersion: number
  name: string
  status: WorkflowInstanceStatus
  correlationId?: string | null
  starterId?: string | null
  parentInstanceId?: NumericString | null
  depth: number
  creationTime: DateTimeString
  startTime?: DateTimeString | null
  endTime?: DateTimeString | null
  faultNodeId?: string | null
  faultMessage?: string | null
}

export interface WorkflowNodeInstanceDto {
  id: string
  nodeId: string
  name: string
  activityType: string
  status: WorkflowNodeInstanceStatus
  tryCount: number
  startTime: DateTimeString
  endTime?: DateTimeString | null
  faultMessage?: string | null
  outputsJson?: string | null
}

export interface WorkflowBookmarkDto {
  id: string
  nodeId: string
  kind: string
  key?: string | null
  dueTime?: DateTimeString | null
  creationTime: DateTimeString
}

export interface WorkflowInstanceDetailDto extends WorkflowInstanceListItemDto {
  variablesJson: string
  cancellationReason?: string | null
  nodeInstances: WorkflowNodeInstanceDto[]
  pendingBookmarks: WorkflowBookmarkDto[]
}

export interface WorkflowInstanceStartDto {
  definitionCode: string
  definitionVersion?: number | null
  name?: string | null
  correlationId?: string | null
  variablesJson?: string | null
}

export interface WorkflowInstanceIdDto {
  basicId: ApiId
}

export interface WorkflowInstanceOperationDto {
  basicId: ApiId
  reason?: string | null
}

export interface WorkflowSignalPublishDto {
  signalName: string
  correlationId?: string | null
  payloadJson?: string | null
}

export interface WorkflowSignalPublishResultDto {
  resumedCount: number
}

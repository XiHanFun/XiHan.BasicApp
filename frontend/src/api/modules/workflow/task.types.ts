import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export { EnableStatus } from '../shared'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum RunTaskStatus {
  Pending = 'Pending',
  Running = 'Running',
  Success = 'Success',
  Failed = 'Failed',
  Stopped = 'Stopped',
  Paused = 'Paused',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum TriggerType {
  Immediate = 'Immediate',
  Schedule = 'Schedule',
  Recurring = 'Recurring',
  Cron = 'Cron',
}

export interface TaskPageQueryDto extends PageRequest {
  allowConcurrent?: boolean | null
  keyword?: string | null
  lastRunTimeEnd?: DateTimeString | null
  lastRunTimeStart?: DateTimeString | null
  nextRunTimeEnd?: DateTimeString | null
  nextRunTimeStart?: DateTimeString | null
  runTaskStatus?: RunTaskStatus | null
  status?: EnableStatus | null
  taskCode?: string | null
  taskGroup?: string | null
  triggerType?: TriggerType | null
}

export interface TaskListItemDto extends BasicDto {
  allowConcurrent: boolean
  createdTime: DateTimeString
  cronExpression?: string | null
  endTime?: DateTimeString | null
  executedCount: number
  intervalSeconds?: number | null
  lastRunTime?: DateTimeString | null
  maxRetryCount: number
  modifiedTime?: DateTimeString | null
  nextRunTime?: DateTimeString | null
  priority: number
  repeatCount: number
  retryCount: number
  runTaskStatus: RunTaskStatus
  startTime?: DateTimeString | null
  status: EnableStatus
  taskCode: string
  taskDescription?: string | null
  taskGroup?: string | null
  taskName: string
  timeoutSeconds: number
  triggerType: TriggerType
}

export interface TaskDetailDto extends TaskListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
  taskClass: string
  taskMethod?: string | null
  taskParams?: string | null
}

// ---- Command DTOs ----

export interface TaskCreateDto {
  taskCode: string
  taskName: string
  taskDescription?: string | null
  taskGroup?: string | null
  taskClass: string
  taskMethod?: string | null
  taskParams?: string | null
  triggerType: TriggerType
  cronExpression?: string | null
  startTime?: DateTimeString | null
  endTime?: DateTimeString | null
  nextRunTime?: DateTimeString | null
  lastRunTime?: DateTimeString | null
  intervalSeconds?: number | null
  repeatCount: number
  executedCount: number
  timeoutSeconds: number
  runTaskStatus: RunTaskStatus
  priority: number
  allowConcurrent: boolean
  retryCount: number
  maxRetryCount: number
  status: EnableStatus
  remark?: string | null
}

export interface TaskUpdateDto extends BasicDto {
  taskName: string
  taskDescription?: string | null
  taskGroup?: string | null
  taskClass: string
  taskMethod?: string | null
  taskParams?: string | null
  triggerType: TriggerType
  cronExpression?: string | null
  startTime?: DateTimeString | null
  endTime?: DateTimeString | null
  nextRunTime?: DateTimeString | null
  lastRunTime?: DateTimeString | null
  intervalSeconds?: number | null
  repeatCount: number
  executedCount: number
  timeoutSeconds: number
  priority: number
  allowConcurrent: boolean
  retryCount: number
  maxRetryCount: number
  remark?: string | null
}

export interface TaskStatusUpdateDto {
  basicId: ApiId
  status: EnableStatus
  remark?: string | null
}

export interface TaskRunStatusUpdateDto {
  basicId: ApiId
  runTaskStatus: RunTaskStatus
  nextRunTime?: DateTimeString | null
  lastRunTime?: DateTimeString | null
  executedCount?: number | null
  retryCount?: number | null
  remark?: string | null
}

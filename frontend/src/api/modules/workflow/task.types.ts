import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export { EnableStatus } from '../shared'

export enum RunTaskStatus {
  Pending = 0,
  Running = 1,
  Success = 2,
  Failed = 3,
  Stopped = 4,
  Paused = 5,
}

export enum TriggerType {
  Immediate = 0,
  Schedule = 1,
  Recurring = 2,
  Cron = 3,
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

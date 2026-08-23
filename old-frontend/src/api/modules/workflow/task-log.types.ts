import type { ApiId, BasicDto, DateTimeString, NumericString, PageRequest } from '../../types'
import type { RunTaskStatus } from './task.types'

export interface TaskLogPageQueryDto extends PageRequest {
  batchNumber?: string | null
  keyword?: string | null
  maxExecutionTime?: number | null
  maxRetryCount?: number | null
  minExecutionTime?: number | null
  minRetryCount?: number | null
  startTimeEnd?: DateTimeString | null
  startTimeStart?: DateTimeString | null
  taskCode?: string | null
  taskId?: ApiId | null
  taskName?: string | null
  taskStatus?: RunTaskStatus | null
  triggerMode?: string | null
}

export interface TaskLogListItemDto extends BasicDto {
  batchNumber?: string | null
  createdTime: DateTimeString
  endTime?: DateTimeString | null
  /** 执行时长（毫秒，后端 long 序列化为字符串） */
  executionTime: NumericString
  retryCount: number
  startTime: DateTimeString
  taskCode: string
  taskId: ApiId
  taskName: string
  taskStatus: RunTaskStatus
  triggerMode?: string | null
}

export interface TaskLogDetailDto extends TaskLogListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  exceptionMessage?: string | null
  exceptionStackTrace?: string | null
  executionResult?: string | null
  outputLog?: string | null
  remark?: string | null
}

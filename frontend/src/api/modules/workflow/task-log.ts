import type { ApiId, PageResult } from '../../types'
import type {
  TaskLogDetailDto,
  TaskLogListItemDto,
  TaskLogPageQueryDto,
} from './task-log.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const taskLogQueryApi = createDynamicApiClient('TaskLogQuery')

export const taskLogApi = {
  detail(id: ApiId) {
    return taskLogQueryApi.get<TaskLogDetailDto | null>(
      `TaskLogDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: TaskLogPageQueryDto) {
    return taskLogQueryApi.get<PageResult<TaskLogListItemDto>>(
      'TaskLogPage',
      toTaskLogPageParams(input),
    )
  },
}

function toTaskLogPageParams(input: TaskLogPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'BatchNumber', input.batchNumber)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'MaxExecutionTime', input.maxExecutionTime)
  appendDynamicApiParam(params, 'MaxRetryCount', input.maxRetryCount)
  appendDynamicApiParam(params, 'MinExecutionTime', input.minExecutionTime)
  appendDynamicApiParam(params, 'MinRetryCount', input.minRetryCount)
  appendDynamicApiParam(params, 'StartTimeEnd', input.startTimeEnd)
  appendDynamicApiParam(params, 'StartTimeStart', input.startTimeStart)
  appendDynamicApiParam(params, 'TaskCode', input.taskCode)
  appendDynamicApiParam(params, 'TaskId', input.taskId)
  appendDynamicApiParam(params, 'TaskName', input.taskName)
  appendDynamicApiParam(params, 'TaskStatus', input.taskStatus)
  appendDynamicApiParam(params, 'TriggerMode', input.triggerMode)
  return params
}

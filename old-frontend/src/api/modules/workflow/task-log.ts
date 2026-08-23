import type { ApiId, PageResult } from '../../types'
import type {
  TaskLogDetailDto,
  TaskLogListItemDto,
  TaskLogPageQueryDto,
} from './task-log.types'
import {
  createDynamicApiClient,
} from '../../base'

const taskLogQueryApi = createDynamicApiClient('TaskLogQuery')

export const taskLogApi = {
  detail(id: ApiId) {
    return taskLogQueryApi.get<TaskLogDetailDto | null>(
      'TaskLogDetail',
      { id },
    )
  },
  page(input: TaskLogPageQueryDto) {
    return taskLogQueryApi.post<PageResult<TaskLogListItemDto>>(
      'TaskLogPage',
      input,
    )
  },
}

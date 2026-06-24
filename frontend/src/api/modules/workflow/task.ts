import type { ApiId, PageResult } from '../../types'
import type {
  TaskCreateDto,
  TaskDetailDto,
  TaskListItemDto,
  TaskPageQueryDto,
  TaskRunStatusUpdateDto,
  TaskStatusUpdateDto,
  TaskUpdateDto,
} from './task.types'
import {
  createDynamicApiClient,
  formatDynamicApiRouteValue,
} from '../../base'

const taskQueryApi = createDynamicApiClient('TaskQuery')
const taskCommandApi = createDynamicApiClient('Task')

export const taskApi = {
  // Query
  detail(id: ApiId) {
    return taskQueryApi.get<TaskDetailDto | null>(
      `TaskDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: TaskPageQueryDto) {
    return taskQueryApi.post<PageResult<TaskListItemDto>>('TaskPage', input)
  },
  // Commands（动态 API 会剥离方法名动词前缀 Create/Update/Delete：实际路由不含动词）
  create(input: TaskCreateDto) {
    return taskCommandApi.post<TaskDetailDto, TaskCreateDto>('Task', input)
  },
  delete(id: ApiId) {
    return taskCommandApi.delete(`Task/${formatDynamicApiRouteValue(id)}`)
  },
  update(input: TaskUpdateDto) {
    return taskCommandApi.put<TaskDetailDto, TaskUpdateDto>('Task', input)
  },
  updateRunStatus(input: TaskRunStatusUpdateDto) {
    return taskCommandApi.put<TaskDetailDto, TaskRunStatusUpdateDto>('TaskRunStatus', input)
  },
  updateStatus(input: TaskStatusUpdateDto) {
    return taskCommandApi.put<TaskDetailDto, TaskStatusUpdateDto>('TaskStatus', input)
  },
  // Run 不在动词剥离表内：路由保留完整方法名（POST RunTask）
  run(id: ApiId) {
    return taskCommandApi.post<{ instanceId: string }, { basicId: ApiId }>('RunTask', { basicId: id })
  },
}

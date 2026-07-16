import type { ApiId, PageResult } from '../../types'
import type {
  WorkflowInstanceDetailDto,
  WorkflowInstanceIdDto,
  WorkflowInstanceListItemDto,
  WorkflowInstanceOperationDto,
  WorkflowInstancePageQueryDto,
  WorkflowInstanceStartDto,
  WorkflowSignalPublishDto,
  WorkflowSignalPublishResultDto,
} from './instance.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const command = createDynamicApiClient('WorkflowInstance')
const query = createDynamicApiClient('WorkflowInstanceQuery')

export const workflowInstanceApi = {
  // Query（分页统一 POST，整个查询对象作 body）
  page(input: WorkflowInstancePageQueryDto) {
    return query.post<PageResult<WorkflowInstanceListItemDto>>('Page', input)
  },
  detail(id: ApiId) {
    return query.get<WorkflowInstanceDetailDto | null>(`Detail/${formatDynamicApiRouteValue(id)}`)
  },
  // Commands（Start/Cancel 等不在动词剥离表内：路由保留完整方法名，POST）
  start(input: WorkflowInstanceStartDto) {
    return command.post<WorkflowInstanceListItemDto, WorkflowInstanceStartDto>('Start', input)
  },
  cancel(input: WorkflowInstanceOperationDto) {
    return command.post<WorkflowInstanceListItemDto, WorkflowInstanceOperationDto>('Cancel', input)
  },
  terminate(input: WorkflowInstanceOperationDto) {
    return command.post<WorkflowInstanceListItemDto, WorkflowInstanceOperationDto>('Terminate', input)
  },
  retry(input: WorkflowInstanceIdDto) {
    return command.post<WorkflowInstanceListItemDto, WorkflowInstanceIdDto>('Retry', input)
  },
  suspend(input: WorkflowInstanceOperationDto) {
    return command.post<WorkflowInstanceListItemDto, WorkflowInstanceOperationDto>('Suspend', input)
  },
  resume(input: WorkflowInstanceIdDto) {
    return command.post<WorkflowInstanceListItemDto, WorkflowInstanceIdDto>('Resume', input)
  },
  publishSignal(input: WorkflowSignalPublishDto) {
    return command.post<WorkflowSignalPublishResultDto, WorkflowSignalPublishDto>('PublishSignal', input)
  },
}

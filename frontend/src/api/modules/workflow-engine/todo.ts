import type { PageResult } from '../../types'
import type {
  WorkflowTodoAddAssigneesDto,
  WorkflowTodoCompleteDto,
  WorkflowTodoCompleteResultDto,
  WorkflowTodoListItemDto,
  WorkflowTodoPageQueryDto,
  WorkflowTodoTransferDto,
} from './todo.types'
import { createDynamicApiClient } from '../../base'

const command = createDynamicApiClient('WorkflowTodo')
const query = createDynamicApiClient('WorkflowTodoQuery')

export const workflowTodoApi = {
  // Query（受理人服务端锁定为当前用户）
  page(input: WorkflowTodoPageQueryDto) {
    return query.post<PageResult<WorkflowTodoListItemDto>>('Page', input)
  },
  // Commands（Complete/Transfer/AddAssignees 不在动词剥离表内：路由保留完整方法名，POST）
  complete(input: WorkflowTodoCompleteDto) {
    return command.post<WorkflowTodoCompleteResultDto, WorkflowTodoCompleteDto>('Complete', input)
  },
  transfer(input: WorkflowTodoTransferDto) {
    return command.post<void, WorkflowTodoTransferDto>('Transfer', input)
  },
  addAssignees(input: WorkflowTodoAddAssigneesDto) {
    return command.post<void, WorkflowTodoAddAssigneesDto>('AddAssignees', input)
  },
}

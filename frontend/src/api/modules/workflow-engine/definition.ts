import type { ApiId, PageResult } from '../../types'
import type {
  WorkflowDefinitionCreateDto,
  WorkflowDefinitionDetailDto,
  WorkflowDefinitionIdDto,
  WorkflowDefinitionListItemDto,
  WorkflowDefinitionNewVersionDto,
  WorkflowDefinitionPageQueryDto,
  WorkflowDefinitionUpdateDraftDto,
} from './definition.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const command = createDynamicApiClient('WorkflowDefinition')
const query = createDynamicApiClient('WorkflowDefinitionQuery')

export const workflowDefinitionApi = {
  // Query（分页统一 POST，整个查询对象作 body）
  page(input: WorkflowDefinitionPageQueryDto) {
    return query.post<PageResult<WorkflowDefinitionListItemDto>>('Page', input)
  },
  detail(id: ApiId) {
    return query.get<WorkflowDefinitionDetailDto | null>(`Detail/${formatDynamicApiRouteValue(id)}`)
  },
  // Commands（Create/Update/Delete 动词映射 POST/PUT/DELETE，其余动作保留完整方法名走 POST）
  create(input: WorkflowDefinitionCreateDto) {
    return command.post<WorkflowDefinitionDetailDto, WorkflowDefinitionCreateDto>('Create', input)
  },
  updateDraft(input: WorkflowDefinitionUpdateDraftDto) {
    return command.put<WorkflowDefinitionDetailDto, WorkflowDefinitionUpdateDraftDto>('Draft', input)
  },
  publish(input: WorkflowDefinitionIdDto) {
    return command.post<WorkflowDefinitionDetailDto, WorkflowDefinitionIdDto>('Publish', input)
  },
  newVersion(input: WorkflowDefinitionNewVersionDto) {
    return command.post<WorkflowDefinitionDetailDto, WorkflowDefinitionNewVersionDto>('NewVersion', input)
  },
  disable(input: WorkflowDefinitionIdDto) {
    return command.post<WorkflowDefinitionDetailDto, WorkflowDefinitionIdDto>('Disable', input)
  },
  archive(input: WorkflowDefinitionIdDto) {
    return command.post<WorkflowDefinitionDetailDto, WorkflowDefinitionIdDto>('Archive', input)
  },
  delete(id: ApiId) {
    return command.delete(`Delete/${formatDynamicApiRouteValue(id)}`)
  },
}

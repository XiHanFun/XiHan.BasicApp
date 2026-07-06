import type { ApiId, PageResult } from '../../types'
import type {
  AiPromptCreateDto,
  AiPromptDetailDto,
  AiPromptListItemDto,
  AiPromptPageQueryDto,
  AiPromptStatusUpdateDto,
  AiPromptUpdateDto,
} from './prompt.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const command = createDynamicApiClient('AiPrompt')
const query = createDynamicApiClient('AiPromptQuery')

export const aiPromptApi = {
  create(input: AiPromptCreateDto) {
    return command.post<AiPromptDetailDto, AiPromptCreateDto>('Create', input)
  },
  update(input: AiPromptUpdateDto) {
    return command.put<AiPromptDetailDto, AiPromptUpdateDto>('Update', input)
  },
  updateStatus(input: AiPromptStatusUpdateDto) {
    return command.put<AiPromptDetailDto, AiPromptStatusUpdateDto>('Status', input)
  },
  delete(id: ApiId) {
    return command.delete(`Delete/${formatDynamicApiRouteValue(id)}`)
  },
  page(input: AiPromptPageQueryDto) {
    return query.post<PageResult<AiPromptListItemDto>>('Page', input)
  },
  detail(id: ApiId) {
    return query.get<AiPromptDetailDto | null>(`Detail/${formatDynamicApiRouteValue(id)}`)
  },
}

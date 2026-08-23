import type {
  AiAssistantActionDto,
  AiAssistantCreateDto,
  AiAssistantDetailDto,
  AiAssistantListItemDto,
  AiAssistantPageQueryDto,
  AiAssistantStatusUpdateDto,
  AiAssistantUpdateDto,
} from './assistant.types'
import type { ApiId, PageResult } from '@/api/types'
import { createDynamicApiClient } from '@/api/base'

const command = createDynamicApiClient('AiAssistant')
const query = createDynamicApiClient('AiAssistantQuery')

export const aiAssistantApi = {
  create(input: AiAssistantCreateDto) {
    return command.post<AiAssistantDetailDto, AiAssistantCreateDto>('Create', input)
  },
  update(input: AiAssistantUpdateDto) {
    return command.put<AiAssistantDetailDto, AiAssistantUpdateDto>('Update', input)
  },
  updateStatus(input: AiAssistantStatusUpdateDto) {
    return command.put<AiAssistantDetailDto, AiAssistantStatusUpdateDto>('Status', input)
  },
  setDefault(id: ApiId) {
    return command.post<AiAssistantDetailDto, AiAssistantActionDto>('SetDefault', { basicId: id })
  },
  delete(id: ApiId) {
    return command.delete('Delete', { id })
  },
  page(input: AiAssistantPageQueryDto) {
    return query.post<PageResult<AiAssistantListItemDto>>('Page', input)
  },
  detail(id: ApiId) {
    return query.get<AiAssistantDetailDto | null>('Detail', { id })
  },
}

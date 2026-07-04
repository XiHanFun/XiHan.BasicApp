import type { ApiId, PageResult } from '../../types'
import type {
  AiProviderActionDto,
  AiProviderCreateDto,
  AiProviderDetailDto,
  AiProviderListItemDto,
  AiProviderPageQueryDto,
  AiProviderStatusUpdateDto,
  AiProviderTestConnectionResultDto,
  AiProviderUpdateDto,
} from './provider.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const command = createDynamicApiClient('AiProvider')
const query = createDynamicApiClient('AiProviderQuery')

export const aiProviderApi = {
  create(input: AiProviderCreateDto) {
    return command.post<AiProviderDetailDto, AiProviderCreateDto>('Create', input)
  },
  update(input: AiProviderUpdateDto) {
    return command.put<AiProviderDetailDto, AiProviderUpdateDto>('Update', input)
  },
  updateStatus(input: AiProviderStatusUpdateDto) {
    return command.put<AiProviderDetailDto, AiProviderStatusUpdateDto>('Status', input)
  },
  setDefault(id: ApiId) {
    return command.post<AiProviderDetailDto, AiProviderActionDto>('SetDefault', { basicId: id })
  },
  delete(id: ApiId) {
    return command.delete(`Delete/${formatDynamicApiRouteValue(id)}`)
  },
  testConnection(id: ApiId) {
    return command.post<AiProviderTestConnectionResultDto, AiProviderActionDto>('TestConnection', { basicId: id })
  },
  page(input: AiProviderPageQueryDto) {
    return query.post<PageResult<AiProviderListItemDto>>('Page', input)
  },
  detail(id: ApiId) {
    return query.get<AiProviderDetailDto | null>(`Detail/${formatDynamicApiRouteValue(id)}`)
  },
}

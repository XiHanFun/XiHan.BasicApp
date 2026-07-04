import type { ApiId, PageResult } from '../../types'
import type {
  KnowledgeDetailDto,
  KnowledgeIngestDto,
  KnowledgeListItemDto,
  KnowledgePageQueryDto,
  KnowledgeQueryDto,
  KnowledgeQueryResultDto,
} from './knowledge.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const command = createDynamicApiClient('KnowledgeDocument')
const query = createDynamicApiClient('KnowledgeDocumentQuery')
const search = createDynamicApiClient('KnowledgeQuery')

export const knowledgeApi = {
  ingest(input: KnowledgeIngestDto) {
    return command.post<KnowledgeDetailDto, KnowledgeIngestDto>('Ingest', input)
  },
  reindex(id: ApiId) {
    return command.post<KnowledgeDetailDto, { basicId: ApiId }>('Reindex', { basicId: id })
  },
  delete(id: ApiId) {
    return command.delete(`Delete/${formatDynamicApiRouteValue(id)}`)
  },
  page(input: KnowledgePageQueryDto) {
    return query.post<PageResult<KnowledgeListItemDto>>('Page', input)
  },
  detail(id: ApiId) {
    return query.get<KnowledgeDetailDto | null>(`Detail/${formatDynamicApiRouteValue(id)}`)
  },
  query(input: KnowledgeQueryDto) {
    return search.post<KnowledgeQueryResultDto, KnowledgeQueryDto>('Query', input)
  },
}

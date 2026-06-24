import type { ApiId, PageResult } from '../../types'
import type {
  CodeGenHistoryDetailDto,
  CodeGenHistoryListItemDto,
  CodeGenHistoryPageQueryDto,
} from './codegen-history.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const query = createDynamicApiClient('CodeGenHistoryQuery')

export const codeGenHistoryApi = {
  page(input: CodeGenHistoryPageQueryDto) {
    return query.post<PageResult<CodeGenHistoryListItemDto>>('Page', input)
  },
  detail(id: ApiId) {
    return query.get<CodeGenHistoryDetailDto | null>(`Detail/${formatDynamicApiRouteValue(id)}`)
  },
  getByTable(tableId: ApiId) {
    return query.get<CodeGenHistoryListItemDto[]>(`ByTable/${formatDynamicApiRouteValue(tableId)}`)
  },
}

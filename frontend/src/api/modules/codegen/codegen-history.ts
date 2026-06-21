import type { ApiId, PageResult } from '../../types'
import type {
  CodeGenHistoryDetailDto,
  CodeGenHistoryListItemDto,
  CodeGenHistoryPageQueryDto,
} from './codegen-history.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const query = createDynamicApiClient('CodeGenHistoryQuery')

export const codeGenHistoryApi = {
  page(input: CodeGenHistoryPageQueryDto) {
    return query.get<PageResult<CodeGenHistoryListItemDto>>('Page', toPageParams(input))
  },
  detail(id: ApiId) {
    return query.get<CodeGenHistoryDetailDto | null>(`Detail/${formatDynamicApiRouteValue(id)}`)
  },
  getByTable(tableId: ApiId) {
    return query.get<CodeGenHistoryListItemDto[]>(`ByTable/${formatDynamicApiRouteValue(tableId)}`)
  },
}

function toPageParams(input: CodeGenHistoryPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'TableId', input.tableId)
  appendDynamicApiParam(params, 'TableName', input.tableName)
  appendDynamicApiParam(params, 'BatchNumber', input.batchNumber)
  appendDynamicApiParam(params, 'GenStatus', input.genStatus)
  appendDynamicApiParam(params, 'StartTime', input.startTime)
  appendDynamicApiParam(params, 'EndTime', input.endTime)
  return params
}

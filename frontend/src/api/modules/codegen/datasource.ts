import type { ApiId, PageResult } from '../../types'
import type {
  CodeGenConnectionTestResultDto,
  CodeGenDataSourceCreateDto,
  CodeGenDataSourceDetailDto,
  CodeGenDataSourceListItemDto,
  CodeGenDataSourcePageQueryDto,
  CodeGenDataSourceStatusUpdateDto,
  CodeGenDataSourceUpdateDto,
} from './datasource.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const command = createDynamicApiClient('CodeGenDataSource')
const query = createDynamicApiClient('CodeGenDataSourceQuery')

export const codeGenDataSourceApi = {
  create(input: CodeGenDataSourceCreateDto) {
    return command.post<CodeGenDataSourceDetailDto, CodeGenDataSourceCreateDto>('Create', input)
  },
  update(input: CodeGenDataSourceUpdateDto) {
    return command.put<CodeGenDataSourceDetailDto, CodeGenDataSourceUpdateDto>('Update', input)
  },
  updateStatus(input: CodeGenDataSourceStatusUpdateDto) {
    return command.put<CodeGenDataSourceDetailDto, CodeGenDataSourceStatusUpdateDto>('Status', input)
  },
  delete(id: ApiId) {
    return command.delete(`Delete/${formatDynamicApiRouteValue(id)}`)
  },
  testConnection(id: ApiId) {
    return command.post<CodeGenConnectionTestResultDto>(`TestConnection/${formatDynamicApiRouteValue(id)}`)
  },
  page(input: CodeGenDataSourcePageQueryDto) {
    return query.get<PageResult<CodeGenDataSourceListItemDto>>('Page', toPageParams(input))
  },
  detail(id: ApiId) {
    return query.get<CodeGenDataSourceDetailDto | null>(`Detail/${formatDynamicApiRouteValue(id)}`)
  },
}

function toPageParams(input: CodeGenDataSourcePageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'DatabaseType', input.databaseType)
  appendDynamicApiParam(params, 'Status', input.status)
  return params
}

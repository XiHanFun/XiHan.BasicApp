import type {
  CodeGenConnectionTestResultDto,
  CodeGenDataSourceCreateDto,
  CodeGenDataSourceDetailDto,
  CodeGenDataSourceListItemDto,
  CodeGenDataSourcePageQueryDto,
  CodeGenDataSourceStatusUpdateDto,
  CodeGenDataSourceUpdateDto,
} from './datasource.types'
import type { ApiId, PageResult } from '@/api/types'
import { createDynamicApiClient } from '@/api/base'

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
    return command.delete('Delete', { id })
  },
  testConnection(id: ApiId) {
    return command.post<CodeGenConnectionTestResultDto>('TestConnection', { id })
  },
  /** 数据源全量列表，供下拉一次取全 */
  options() {
    return query.get<CodeGenDataSourceListItemDto[]>('Options')
  },
  page(input: CodeGenDataSourcePageQueryDto) {
    return query.post<PageResult<CodeGenDataSourceListItemDto>>('Page', input)
  },
  detail(id: ApiId) {
    return query.get<CodeGenDataSourceDetailDto | null>('Detail', { id })
  },
}

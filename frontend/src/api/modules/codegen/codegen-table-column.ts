import type { ApiId } from '../../types'
import type {
  CodeGenTableColumnBatchSaveDto,
  CodeGenTableColumnListItemDto,
  CodeGenTableColumnUpdateDto,
} from './codegen-table-column.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const command = createDynamicApiClient('CodeGenTableColumn')
const query = createDynamicApiClient('CodeGenTableColumnQuery')

export const codeGenTableColumnApi = {
  batchSave(input: CodeGenTableColumnBatchSaveDto) {
    return command.post<void, CodeGenTableColumnBatchSaveDto>('BatchSave', input)
  },
  update(input: CodeGenTableColumnUpdateDto) {
    return command.put<CodeGenTableColumnListItemDto, CodeGenTableColumnUpdateDto>('Update', input)
  },
  getByTable(tableId: ApiId) {
    return query.get<CodeGenTableColumnListItemDto[]>(`ByTable/${formatDynamicApiRouteValue(tableId)}`)
  },
}

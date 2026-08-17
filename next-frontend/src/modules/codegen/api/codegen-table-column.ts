import type {
  CodeGenTableColumnBatchSaveDto,
  CodeGenTableColumnListItemDto,
  CodeGenTableColumnUpdateDto,
} from './codegen-table-column.types'
import type { ApiId } from '@/api/types'
import { createDynamicApiClient } from '@/api/base'

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
    return query.get<CodeGenTableColumnListItemDto[]>('ByTable', { tableId })
  },
}

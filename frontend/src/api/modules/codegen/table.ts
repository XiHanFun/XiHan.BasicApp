import type { ApiId, PageResult } from '../../types'
import type {
  CodeGenTableDetailDto,
  CodeGenTableListItemDto,
  CodeGenTablePageQueryDto,
  CodeGenTableStatusUpdateDto,
  CodeGenTableUpdateDto,
} from './table.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const command = createDynamicApiClient('CodeGenTable')
const query = createDynamicApiClient('CodeGenTableQuery')

/**
 * 代码生成表配置 API。
 * 表由「导入表」编排接口创建，故无 create；动作名为 Update/Status/Delete/Page/Detail，
 * 裸 createDynamicApiClient 直调（不用 createReadApi）。
 */
export const codeGenTableApi = {
  update(input: CodeGenTableUpdateDto) {
    return command.put<CodeGenTableDetailDto, CodeGenTableUpdateDto>('Update', input)
  },
  updateStatus(input: CodeGenTableStatusUpdateDto) {
    return command.put<CodeGenTableDetailDto, CodeGenTableStatusUpdateDto>('Status', input)
  },
  delete(id: ApiId) {
    return command.delete(`Delete/${formatDynamicApiRouteValue(id)}`)
  },
  page(input: CodeGenTablePageQueryDto) {
    return query.post<PageResult<CodeGenTableListItemDto>>('Page', input)
  },
  detail(id: ApiId) {
    return query.get<CodeGenTableDetailDto | null>(`Detail/${formatDynamicApiRouteValue(id)}`)
  },
}

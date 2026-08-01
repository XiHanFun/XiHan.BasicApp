import type { ApiId, PageResult } from '../../types'
import type { OperationLogDetailDto, OperationLogListItemDto, OperationLogPageQueryDto } from './operation-log.types'
import { createDynamicApiClient } from '../../base'

const operationLogQueryApi = createDynamicApiClient('OperationLogQuery')

export const operationLogApi = {
  detail(id: ApiId) {
    return operationLogQueryApi.get<OperationLogDetailDto | null>(
      'OperationLogDetail',
      { id },
    )
  },
  page(input: OperationLogPageQueryDto) {
    return operationLogQueryApi.post<PageResult<OperationLogListItemDto>>('OperationLogPage', input)
  },
}

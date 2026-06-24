import type { ApiId, PageResult } from '../../types'
import type { OperationLogDetailDto, OperationLogListItemDto, OperationLogPageQueryDto } from './operation-log.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const operationLogQueryApi = createDynamicApiClient('OperationLogQuery')

export const operationLogApi = {
  detail(id: ApiId) {
    return operationLogQueryApi.get<OperationLogDetailDto | null>(
      `OperationLogDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: OperationLogPageQueryDto) {
    return operationLogQueryApi.post<PageResult<OperationLogListItemDto>>('OperationLogPage', input)
  },
}

import type { ApiId, PageResult } from '../../types'
import type { ExceptionLogDetailDto, ExceptionLogListItemDto, ExceptionLogPageQueryDto } from './exception-log.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const exceptionLogQueryApi = createDynamicApiClient('ExceptionLogQuery')

export const exceptionLogApi = {
  detail(id: ApiId) {
    return exceptionLogQueryApi.get<ExceptionLogDetailDto | null>(
      `ExceptionLogDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: ExceptionLogPageQueryDto) {
    return exceptionLogQueryApi.post<PageResult<ExceptionLogListItemDto>>('ExceptionLogPage', input)
  },
}

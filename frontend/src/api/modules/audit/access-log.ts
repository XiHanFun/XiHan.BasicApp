import type { ApiId, PageResult } from '../../types'
import type { AccessLogDetailDto, AccessLogListItemDto, AccessLogPageQueryDto } from './access-log.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const accessLogQueryApi = createDynamicApiClient('AccessLogQuery')

export const accessLogApi = {
  detail(id: ApiId) {
    return accessLogQueryApi.get<AccessLogDetailDto | null>(
      `AccessLogDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: AccessLogPageQueryDto) {
    return accessLogQueryApi.post<PageResult<AccessLogListItemDto>>('AccessLogPage', input)
  },
}

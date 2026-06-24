import type { ApiId, PageResult } from '../../types'
import type { DiffLogDetailDto, DiffLogListItemDto, DiffLogPageQueryDto } from './diff-log.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const diffLogQueryApi = createDynamicApiClient('DiffLogQuery')

export const diffLogApi = {
  detail(id: ApiId) {
    return diffLogQueryApi.get<DiffLogDetailDto | null>(
      `DiffLogDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: DiffLogPageQueryDto) {
    return diffLogQueryApi.post<PageResult<DiffLogListItemDto>>('DiffLogPage', input)
  },
}

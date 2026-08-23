import type { ApiId, PageResult } from '../../types'
import type { ApiLogDetailDto, ApiLogListItemDto, ApiLogPageQueryDto } from './api-log.types'
import { createDynamicApiClient } from '../../base'

const apiLogQueryApi = createDynamicApiClient('ApiLogQuery')

export const apiLogApi = {
  detail(id: ApiId) {
    return apiLogQueryApi.get<ApiLogDetailDto | null>(
      'ApiLogDetail',
      { id },
    )
  },
  page(input: ApiLogPageQueryDto) {
    return apiLogQueryApi.post<PageResult<ApiLogListItemDto>>('ApiLogPage', input)
  },
}

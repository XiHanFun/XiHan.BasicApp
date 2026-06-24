import type { ApiId, PageResult } from '../../types'
import type {
  UserStatisticsDetailDto,
  UserStatisticsListItemDto,
  UserStatisticsPageQueryDto,
} from './user-statistics.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const userStatisticsQueryApi = createDynamicApiClient('UserStatisticsQuery')

export const userStatisticsApi = {
  detail(id: ApiId) {
    return userStatisticsQueryApi.get<UserStatisticsDetailDto | null>(
      `UserStatisticsDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: UserStatisticsPageQueryDto) {
    return userStatisticsQueryApi.post<PageResult<UserStatisticsListItemDto>>('UserStatisticsPage', input)
  },
}

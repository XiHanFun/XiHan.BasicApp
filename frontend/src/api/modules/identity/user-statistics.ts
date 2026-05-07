import type { ApiId, PageResult } from '../../types'
import type {
  UserStatisticsDetailDto,
  UserStatisticsListItemDto,
  UserStatisticsPageQueryDto,
} from './user-statistics.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const userStatisticsQueryApi = createDynamicApiClient('UserStatisticsQuery')

export const userStatisticsApi = {
  detail(id: ApiId) {
    return userStatisticsQueryApi.get<UserStatisticsDetailDto | null>(
      `UserStatisticsDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: UserStatisticsPageQueryDto) {
    return userStatisticsQueryApi.get<PageResult<UserStatisticsListItemDto>>(
      'UserStatisticsPage',
      toUserStatisticsPageParams(input),
    )
  },
}

function toUserStatisticsPageParams(input: UserStatisticsPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'UserId', input.userId)
  appendDynamicApiParam(params, 'Period', input.period)
  appendDynamicApiParam(params, 'StatisticsDateStart', input.statisticsDateStart)
  appendDynamicApiParam(params, 'StatisticsDateEnd', input.statisticsDateEnd)
  return params
}

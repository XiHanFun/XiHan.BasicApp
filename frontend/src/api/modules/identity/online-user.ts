import type { DynamicApiParams } from '../../base'
import type { PageResult } from '../../types'
import type {
  OnlineUserListItemDto,
  OnlineUserPageQueryDto,
  OnlineUserSummaryDto,
} from './online-user.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
} from '../../base'

const onlineUserQueryApi = createDynamicApiClient('OnlineUserQuery')

export const onlineUserApi = {
  page(input: OnlineUserPageQueryDto) {
    return onlineUserQueryApi.get<PageResult<OnlineUserListItemDto>>(
      'OnlineUserPage',
      toOnlineUserPageParams(input),
    )
  },
  summary() {
    return onlineUserQueryApi.get<OnlineUserSummaryDto>('OnlineUserSummary')
  },
}

function toOnlineUserPageParams(input: OnlineUserPageQueryDto) {
  const params: DynamicApiParams = createPageRequestParams(input)

  appendDynamicApiParam(params, 'DeviceType', input.deviceType)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'UserId', input.userId)

  return params
}

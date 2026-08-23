import type { PageResult } from '../../types'
import type {
  OnlineUserListItemDto,
  OnlineUserPageQueryDto,
  OnlineUserSummaryDto,
} from './online-user.types'
import { createDynamicApiClient } from '../../base'

const onlineUserQueryApi = createDynamicApiClient('OnlineUserQuery')

export const onlineUserApi = {
  page(input: OnlineUserPageQueryDto) {
    return onlineUserQueryApi.post<PageResult<OnlineUserListItemDto>>(
      'OnlineUserPage',
      input,
    )
  },
  summary() {
    return onlineUserQueryApi.get<OnlineUserSummaryDto>('OnlineUserSummary')
  },
}

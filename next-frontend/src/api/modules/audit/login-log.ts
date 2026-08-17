import type { ApiId, PageResult } from '../../types'
import type { LoginLogDetailDto, LoginLogListItemDto, LoginLogPageQueryDto } from './login-log.types'
import { createDynamicApiClient } from '../../base'

const loginLogQueryApi = createDynamicApiClient('LoginLogQuery')

export const loginLogApi = {
  detail(id: ApiId) {
    return loginLogQueryApi.get<LoginLogDetailDto | null>(
      'LoginLogDetail',
      { id },
    )
  },
  page(input: LoginLogPageQueryDto) {
    return loginLogQueryApi.post<PageResult<LoginLogListItemDto>>('LoginLogPage', input)
  },
}
